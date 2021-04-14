using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Rendering;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Produces keypoint annotations for a humanoid model. This labeler supports generic
    /// <see cref="KeypointTemplate"/>. Template values are mapped to rigged
    /// <see cref="Animator"/> <seealso cref="Avatar"/>. Custom joints can be
    /// created by applying <see cref="JointLabel"/> to empty game objects at a body
    /// part's location.
    /// </summary>
    [Serializable]
    public sealed class KeypointLabeler : CameraLabeler
    {
        /// <summary>
        /// The active keypoint template. Required to annotate keypoint data.
        /// </summary>
        public KeypointTemplate activeTemplate;

        /// <inheritdoc/>
        public override string description
        {
            get => "Produces keypoint annotations for all visible labeled objects that have a humanoid animation avatar component.";
            protected set { }
        }

        ///<inheritdoc/>
        protected override bool supportsVisualization => true;

        // ReSharper disable MemberCanBePrivate.Global
        /// <summary>
        /// The GUID id to associate with the annotations produced by this labeler.
        /// </summary>
        public string annotationId = "8b3ef246-daa7-4dd5-a0e8-a943f6e7f8c2";
        /// <summary>
        /// The <see cref="IdLabelConfig"/> which associates objects with labels.
        /// </summary>
        public IdLabelConfig idLabelConfig;

        /// <summary>
        /// Controls which objects will have keypoints recorded in the dataset.
        /// <see cref="KeypointObjectFilter"/>
        /// </summary>
        public KeypointObjectFilter objectFilter;
        // ReSharper restore MemberCanBePrivate.Global

        AnnotationDefinition m_AnnotationDefinition;
        Texture2D m_MissingTexture;

        Dictionary<int, (AsyncAnnotation annotation, Dictionary<uint, KeypointEntry> keypoints)> m_AsyncAnnotations;
        List<KeypointEntry> m_KeypointEntriesToReport;

        int m_CurrentFrame;

        /// <summary>
        /// Action that gets triggered when a new frame of key points are computed.
        /// </summary>
        public event Action<int, List<KeypointEntry>> KeypointsComputed;

        /// <summary>
        /// Creates a new key point labeler. This constructor creates a labeler that
        /// is not valid until a <see cref="IdLabelConfig"/> and <see cref="KeypointTemplate"/>
        /// are assigned.
        /// </summary>
        public KeypointLabeler() { }

        /// <summary>
        /// Creates a new key point labeler.
        /// </summary>
        /// <param name="config">The Id label config for the labeler</param>
        /// <param name="template">The active keypoint template</param>
        public KeypointLabeler(IdLabelConfig config, KeypointTemplate template)
        {
            this.idLabelConfig = config;
            this.activeTemplate = template;
        }

        /// <summary>
        /// Array of animation pose labels which map animation clip times to ground truth pose labels.
        /// </summary>
        public List<AnimationPoseConfig> animationPoseConfigs;



        /// <inheritdoc/>
        protected override void Setup()
        {
            if (idLabelConfig == null)
                throw new InvalidOperationException($"{nameof(KeypointLabeler)}'s idLabelConfig field must be assigned");

            m_AnnotationDefinition = DatasetCapture.RegisterAnnotationDefinition("keypoints", new []{TemplateToJson(activeTemplate)},
                "pixel coordinates of keypoints in a model, along with skeletal connectivity data", id: new Guid(annotationId));

            // Texture to use in case the template does not contain a texture for the joints or the skeletal connections
            m_MissingTexture = new Texture2D(1, 1);

            m_KnownStatus = new Dictionary<uint, CachedData>();

            m_AsyncAnnotations = new Dictionary<int, (AsyncAnnotation, Dictionary<uint, KeypointEntry>)>();
            m_KeypointEntriesToReport = new List<KeypointEntry>();
            m_CurrentFrame = 0;

            perceptionCamera.InstanceSegmentationImageReadback += OnInstanceSegmentationImageReadback;
            perceptionCamera.RenderedObjectInfosCalculated += OnRenderedObjectInfoReadback;
        }

        bool AreEqual(Color32 lhs, Color32 rhs)
        {
            return lhs.r == rhs.r && lhs.g == rhs.g && lhs.b == rhs.b && lhs.a == rhs.a;
        }

        bool PixelOnScreen(int x, int y, (int x, int y) dimensions)
        {
            return x >= 0 && x < dimensions.x && y >= 0 && y < dimensions.y;
        }

        bool PixelsMatch(int x, int y, Color32 idColor, (int x, int y) dimensions, NativeArray<Color32> data)
        {
            var h = dimensions.y - 1 - y;
            var pixelColor = data[h * dimensions.x + x];
            return AreEqual(pixelColor, idColor);
        }

        static int s_PixelTolerance = 1;

        // Determine the state of a keypoint. A keypoint is considered visible (state = 2) if it is on screen and not occluded
        // by another object. The way that we determine if a point is occluded is by checking the pixel location of the keypoint
        // against the instance segmentation mask for the frame. The instance segmentation mask provides the instance id of the
        // visible object at a pixel location. Which means, if the keypoint does not match the visible pixel, then another
        // object is in front of the keypoint occluding it from view. An important note here is that the keypoint is an infintely small
        // point in space, which can lead to false negatives due to rounding issues if the keypoint is on the edge of an object or very
        // close to the edge of the screen. Because of this we will test not only the keypoint pixel, but also the immediate surrounding
        // pixels  to determine if the pixel is really visible. This method returns 1 if the pixel is not visible but on screen, and 0
        // if the pixel is off of the screen (taken the tolerance into account).
        int DetermineKeypointState(Keypoint keypoint, Color32 instanceIdColor, (int x, int y) dimensions, NativeArray<Color32> data)
        {
            if (keypoint.state == 0) return 0;

            var centerX = Mathf.FloorToInt(keypoint.x);
            var centerY = Mathf.FloorToInt(keypoint.y);

            if (!PixelOnScreen(centerX, centerY, dimensions))
                return 0;

            var pixelMatched = false;

            for (var y = centerY - s_PixelTolerance; y <= centerY + s_PixelTolerance; y++)
            {
                for (var x = centerX - s_PixelTolerance; x <= centerX + s_PixelTolerance; x++)
                {
                    if (!PixelOnScreen(x, y, dimensions)) continue;

                    pixelMatched = true;
                    if (PixelsMatch(x, y, instanceIdColor, dimensions, data))
                    {
                        return 2;
                    }
                }
            }

            return pixelMatched ? 1 : 0;
        }

        void OnInstanceSegmentationImageReadback(int frameCount, NativeArray<Color32> data, RenderTexture renderTexture)
        {
            if (!m_AsyncAnnotations.TryGetValue(frameCount, out var asyncAnnotation))
                return;

            var dimensions = (renderTexture.width, renderTexture.height);

            foreach (var keypointSet in asyncAnnotation.keypoints)
            {
                if (InstanceIdToColorMapping.TryGetColorFromInstanceId(keypointSet.Key, out var idColor))
                {
                    for (var i = 0; i < keypointSet.Value.keypoints.Length; i++)
                    {
                        var keypoint = keypointSet.Value.keypoints[i];
                        keypoint.state = DetermineKeypointState(keypoint, idColor, dimensions, data);

                        if (keypoint.state == 0)
                        {
                            keypoint.x = 0;
                            keypoint.y = 0;
                        }
                        else
                        {
                            keypoint.x = math.clamp(keypoint.x, 0, dimensions.width - .001f);
                            keypoint.y = math.clamp(keypoint.y, 0, dimensions.height - .001f);
                        }

                        keypointSet.Value.keypoints[i] = keypoint;
                    }
                }
            }
        }

        private void OnRenderedObjectInfoReadback(int frameCount, NativeArray<RenderedObjectInfo> objectInfos)
        {
            if (!m_AsyncAnnotations.TryGetValue(frameCount, out var asyncAnnotation))
                return;

            m_AsyncAnnotations.Remove(frameCount);

            m_KeypointEntriesToReport.Clear();

            //filter out objects that are not visible
            foreach (var keypointSet in asyncAnnotation.keypoints)
            {
                var entry = keypointSet.Value;

                var include = false;
                if (objectFilter == KeypointObjectFilter.All)
                    include = true;
                else
                {
                    foreach (var objectInfo in objectInfos)
                    {
                        if (entry.instance_id == objectInfo.instanceId)
                        {
                            include = true;
                            break;
                        }
                    }

                    if (!include && objectFilter == KeypointObjectFilter.VisibleAndOccluded)
                        include = keypointSet.Value.keypoints.Any(k => k.state == 1);
                }
                if (include)
                    m_KeypointEntriesToReport.Add(entry);
            }

            //This code assumes that OnRenderedObjectInfoReadback will be called immediately after OnInstanceSegmentationImageReadback
            KeypointsComputed?.Invoke(frameCount, m_KeypointEntriesToReport);
            asyncAnnotation.annotation.ReportValues(m_KeypointEntriesToReport);
        }

        /// <param name="scriptableRenderContext"></param>
        /// <inheritdoc/>
        protected override void OnEndRendering(ScriptableRenderContext scriptableRenderContext)
        {
            m_CurrentFrame = Time.frameCount;

            var annotation = perceptionCamera.SensorHandle.ReportAnnotationAsync(m_AnnotationDefinition);
            var keypoints = new Dictionary<uint, KeypointEntry>();

            m_AsyncAnnotations[m_CurrentFrame] = (annotation, keypoints);

            foreach (var label in LabelManager.singleton.registeredLabels)
                ProcessLabel(label);
        }

        // ReSharper disable InconsistentNaming
        // ReSharper disable NotAccessedField.Global
        // ReSharper disable NotAccessedField.Local
        /// <summary>
        /// Record storing all of the keypoint data of a labeled gameobject.
        /// </summary>
        [Serializable]
        public class KeypointEntry
        {
            /// <summary>
            /// The label id of the entity
            /// </summary>
            public int label_id;
            /// <summary>
            /// The instance id of the entity
            /// </summary>
            public uint instance_id;
            /// <summary>
            /// The template that the points are based on
            /// </summary>
            public string template_guid;
            /// <summary>
            /// Pose ground truth for the current set of keypoints
            /// </summary>
            public string pose = "unset";
            /// <summary>
            /// Array of all of the keypoints
            /// </summary>
            public Keypoint[] keypoints;
        }

        /// <summary>
        /// The values of a specific keypoint
        /// </summary>
        [Serializable]
        public struct Keypoint
        {
            /// <summary>
            /// The index of the keypoint in the template file
            /// </summary>
            public int index;
            /// <summary>
            /// The keypoint's x-coordinate pixel location
            /// </summary>
            public float x;
            /// <summary>
            /// The keypoint's y-coordinate pixel location
            /// </summary>
            public float y;
            /// <summary>
            /// The state of the point,
            /// 0 = not present,
            /// 1 = keypoint is present but not visible,
            /// 2 = keypoint is present and visible
            /// </summary>
            public int state;
        }
        // ReSharper restore InconsistentNaming
        // ReSharper restore NotAccessedField.Global
        // ReSharper restore NotAccessedField.Local

        float GetCaptureHeight()
        {
            var targetTexture = perceptionCamera.attachedCamera.targetTexture;
            return targetTexture != null ?
                targetTexture.height : Screen.height;
        }
        Vector3 ConvertToScreenSpace(Vector3 worldLocation)
        {
            var pt = perceptionCamera.attachedCamera.WorldToScreenPoint(worldLocation);
            pt.y = GetCaptureHeight() - pt.y;
            if (Mathf.Approximately(pt.y, perceptionCamera.attachedCamera.pixelHeight))
                pt.y -= .0001f;
            if (Mathf.Approximately(pt.x, perceptionCamera.attachedCamera.pixelWidth))
                pt.x -= .0001f;

            return pt;
        }

        struct CachedData
        {
            public bool status;
            public Animator animator;
            public KeypointEntry keypoints;
            public List<(JointLabel, int)> overrides;
        }

        Dictionary<uint, CachedData> m_KnownStatus;

        bool TryToGetTemplateIndexForJoint(KeypointTemplate template, JointLabel joint, out int index)
        {
            index = -1;

            foreach (var jointTemplate in joint.templateInformation.Where(jointTemplate => jointTemplate.template == template))
            {
                for (var i = 0; i < template.keypoints.Length; i++)
                {
                    if (template.keypoints[i].label == jointTemplate.label)
                    {
                        index = i;
                        return true;
                    }
                }
            }

            return false;
        }

        bool DoesTemplateContainJoint(JointLabel jointLabel)
        {
            foreach (var template in jointLabel.templateInformation)
            {
                if (template.template == activeTemplate)
                {
                    if (activeTemplate.keypoints.Any(i => i.label == template.label))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        void ProcessLabel(Labeling labeledEntity)
        {
            if (!idLabelConfig.TryGetLabelEntryFromInstanceId(labeledEntity.instanceId, out var labelEntry))
                return;

            // Cache out the data of a labeled game object the first time we see it, this will
            // save performance each frame. Also checks to see if a labeled game object can be annotated.
            if (!m_KnownStatus.ContainsKey(labeledEntity.instanceId))
            {
                var cached = new CachedData()
                {
                    status = false,
                    animator = null,
                    keypoints = new KeypointEntry(),
                    overrides = new List<(JointLabel, int)>()
                };

                var entityGameObject = labeledEntity.gameObject;

                cached.keypoints.instance_id = labeledEntity.instanceId;
                cached.keypoints.label_id = labelEntry.id;
                cached.keypoints.template_guid = activeTemplate.templateID;

                cached.keypoints.keypoints = new Keypoint[activeTemplate.keypoints.Length];
                for (var i = 0; i < cached.keypoints.keypoints.Length; i++)
                {
                    cached.keypoints.keypoints[i] = new Keypoint { index = i, state = 0 };
                }

                var animator = entityGameObject.transform.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    cached.animator = animator;
                    cached.status = true;
                }

                foreach (var joint in entityGameObject.transform.GetComponentsInChildren<JointLabel>())
                {
                    if (TryToGetTemplateIndexForJoint(activeTemplate, joint, out var idx))
                    {
                        cached.overrides.Add((joint, idx));
                        cached.status = true;
                    }
                }

                m_KnownStatus[labeledEntity.instanceId] = cached;
            }

            var cachedData = m_KnownStatus[labeledEntity.instanceId];

            if (cachedData.status)
            {
                var animator = cachedData.animator;
                var keypoints = cachedData.keypoints.keypoints;

                // Go through all of the rig keypoints and get their location
                for (var i = 0; i < activeTemplate.keypoints.Length; i++)
                {
                    var pt = activeTemplate.keypoints[i];
                    if (pt.associateToRig)
                    {
                        var bone = animator.GetBoneTransform(pt.rigLabel);
                        if (bone != null)
                        {
                                InitKeypoint(bone.position, keypoints, i);
                        }
                    }
                }

                // Go through all of the additional or override points defined by joint labels and get
                // their locations
                foreach (var (joint, idx) in cachedData.overrides)
                {
                        InitKeypoint(joint.transform.position, keypoints, idx);
                }

                cachedData.keypoints.pose = "unset";

                if (cachedData.animator != null)
                {
                    cachedData.keypoints.pose = GetPose(cachedData.animator);
                }


                var cachedKeypointEntry = cachedData.keypoints;
                var keypointEntry = new KeypointEntry()
                {
                    instance_id = cachedKeypointEntry.instance_id,
                    keypoints = cachedKeypointEntry.keypoints.ToArray(),
                    label_id = cachedKeypointEntry.label_id,
                    pose = cachedKeypointEntry.pose,
                    template_guid = cachedKeypointEntry.template_guid
                };
                m_AsyncAnnotations[m_CurrentFrame].keypoints[labeledEntity.instanceId] = keypointEntry;
            }
        }

        private void InitKeypoint(Vector3 position, Keypoint[] keypoints, int idx)
        {
            var loc = ConvertToScreenSpace(position);
            keypoints[idx].index = idx;
            if (loc.z < 0)
            {
                keypoints[idx].x = 0;
                keypoints[idx].y = 0;
                keypoints[idx].state = 0;
            }
            else
            {
                keypoints[idx].x = loc.x;
                keypoints[idx].y = loc.y;
                keypoints[idx].state = 2;
            }
        }

        string GetPose(Animator animator)
        {
            var info = animator.GetCurrentAnimatorClipInfo(0);

            if (info != null && info.Length > 0)
            {
                var clip = info[0].clip;
                var timeOffset = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;

                if (animationPoseConfigs != null)
                {
                    foreach (var p in animationPoseConfigs)
                    {
                        if (p != null && p.animationClip == clip)
                        {
                            var time = timeOffset;
                            var label = p.GetPoseAtTime(time);
                            return label;
                        }
                    }
                }
            }

            return "unset";
        }

        Keypoint? GetKeypointForJoint(KeypointEntry entry, int joint)
        {
            if (joint < 0 || joint >= entry.keypoints.Length) return null;
            return entry.keypoints[joint];
        }

        /// <inheritdoc/>
        protected override void OnVisualize()
        {
            if (m_KeypointEntriesToReport == null) return;

            var jointTexture = activeTemplate.jointTexture;
            if (jointTexture == null) jointTexture = m_MissingTexture;

            var skeletonTexture = activeTemplate.skeletonTexture;
            if (skeletonTexture == null) skeletonTexture = m_MissingTexture;

            foreach (var entry in m_KeypointEntriesToReport)
            {
                foreach (var bone in activeTemplate.skeleton)
                {
                    var joint1 = GetKeypointForJoint(entry, bone.joint1);
                    var joint2 = GetKeypointForJoint(entry, bone.joint2);

                    if (joint1 != null && joint1.Value.state == 2 && joint2 != null && joint2.Value.state == 2)
                    {
                        VisualizationHelper.DrawLine(joint1.Value.x, joint1.Value.y, joint2.Value.x, joint2.Value.y, bone.color, 8, skeletonTexture);
                    }
                }

                foreach (var keypoint in entry.keypoints)
                {
                    if (keypoint.state == 2)
                        VisualizationHelper.DrawPoint(keypoint.x, keypoint.y, activeTemplate.keypoints[keypoint.index].color, 8, jointTexture);
                }
            }
        }

        // ReSharper disable InconsistentNaming
        // ReSharper disable NotAccessedField.Local
        [Serializable]
        struct JointJson
        {
            public string label;
            public int index;
            public Color color;
        }

        [Serializable]
        struct SkeletonJson
        {
            public int joint1;
            public int joint2;
            public Color color;
        }

        [Serializable]
        struct KeypointJson
        {
            public string template_id;
            public string template_name;
            public JointJson[] key_points;
            public SkeletonJson[] skeleton;
        }
        // ReSharper restore InconsistentNaming
        // ReSharper restore NotAccessedField.Local

        KeypointJson TemplateToJson(KeypointTemplate input)
        {
            var json = new KeypointJson();
            json.template_id = input.templateID;
            json.template_name = input.templateName;
            json.key_points = new JointJson[input.keypoints.Length];
            json.skeleton = new SkeletonJson[input.skeleton.Length];

            for (var i = 0; i < input.keypoints.Length; i++)
            {
                json.key_points[i] = new JointJson
                {
                    label = input.keypoints[i].label,
                    index = i,
                    color = input.keypoints[i].color
                };
            }

            for (var i = 0; i < input.skeleton.Length; i++)
            {
                json.skeleton[i] = new SkeletonJson()
                {
                    joint1 = input.skeleton[i].joint1,
                    joint2 = input.skeleton[i].joint2,
                    color = input.skeleton[i].color
                };
            }

            return json;
        }
    }
}
