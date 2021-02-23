using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Serialization;

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
        // ReSharper restore MemberCanBePrivate.Global

        AnnotationDefinition m_AnnotationDefinition;
        EntityQuery m_EntityQuery;
        Texture2D m_MissingTexture;

        Dictionary<int, AsyncAnnotation> m_AsyncAnnotations;
        Dictionary<int, Dictionary<uint, KeypointEntry>> m_Keypoints;
        List<KeypointEntry> m_ToReport;

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

            m_EntityQuery = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(Labeling), typeof(GroundTruthInfo));

            // Texture to use in case the template does not contain a texture for the joints or the skeletal connections
            m_MissingTexture = new Texture2D(1, 1);

            m_KnownStatus = new Dictionary<uint, CachedData>();

            m_AsyncAnnotations = new Dictionary<int, AsyncAnnotation>();
            m_Keypoints = new Dictionary<int, Dictionary<uint, KeypointEntry>>();
            m_ToReport = new List<KeypointEntry>();
            m_CurrentFrame = 0;

            perceptionCamera.InstanceSegmentationImageReadback += OnInstanceSegmentationImageReadback;
        }

        bool AreEqual(Color32 lhs, Color32 rhs)
        {
            return lhs.r == rhs.r && lhs.g == rhs.g && lhs.b == rhs.b && lhs.a == rhs.a;
        }

        void OnInstanceSegmentationImageReadback(int frameCount, NativeArray<Color32> data, RenderTexture renderTexture)
        {
            if (!m_AsyncAnnotations.TryGetValue(frameCount, out var asyncAnnotation))
                return;

            m_AsyncAnnotations.Remove(frameCount);

            if (!m_Keypoints.TryGetValue(frameCount, out var keypoints))
                return;

            var width = renderTexture.width;

            m_Keypoints.Remove(frameCount);

            m_ToReport.Clear();

            foreach (var keypointSet in keypoints)
            {
                if (InstanceIdToColorMapping.TryGetColorFromInstanceId(keypointSet.Key, out var idColor))
                {
                    var shouldReport = false;

                    foreach (var keypoint in keypointSet.Value.keypoints)
                    {
                        // If the keypoint isn't mapped to a body part keep it at 0
                        if (keypoint.state == 0) continue;

                        if (keypoint.x < 0 || keypoint.x > width || keypoint.y < 0 || keypoint.y > renderTexture.height)
                        {
                            keypoint.state = 0;
                            keypoint.x = 0;
                            keypoint.y = 0;
                        }
                        else
                        {
                            // Get the pixel color at the keypoints location
                            var height = renderTexture.height - (int)keypoint.y;
                            var pixel = data[height * width + (int)keypoint.x];

                            keypoint.state = AreEqual(pixel, idColor) ? 2 : 1;
                            shouldReport = true;
                        }
                    }

                    if (shouldReport)
                        m_ToReport.Add(keypointSet.Value);
                }
            }

            KeypointsComputed?.Invoke(frameCount, m_ToReport);
            asyncAnnotation.ReportValues(m_ToReport);
        }

        /// <inheritdoc/>
        protected override void OnBeginRendering()
        {
            m_CurrentFrame = Time.frameCount;

            m_Keypoints[m_CurrentFrame] = new Dictionary<uint, KeypointEntry>();

            m_AsyncAnnotations[m_CurrentFrame] = perceptionCamera.SensorHandle.ReportAnnotationAsync(m_AnnotationDefinition);

            var entities = m_EntityQuery.ToEntityArray(Allocator.TempJob);
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            foreach (var entity in entities)
            {
                ProcessEntity(entityManager.GetComponentObject<Labeling>(entity));
            }

            entities.Dispose();
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
        public class Keypoint
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
            return perceptionCamera.attachedCamera.targetTexture != null ?
                perceptionCamera.attachedCamera.targetTexture.height : Screen.height;
        }

        // Converts a coordinate from world space into pixel space
        Vector3 ConvertToScreenSpace(Vector3 worldLocation)
        {
            var pt = perceptionCamera.attachedCamera.WorldToScreenPoint(worldLocation);
            pt.y = GetCaptureHeight() - pt.y;
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

        void ProcessEntity(Labeling labeledEntity)
        {
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

                if (idLabelConfig.TryGetLabelEntryFromInstanceId(labeledEntity.instanceId, out var labelEntry))
                {
                    var entityGameObject = labeledEntity.gameObject;

                    cached.keypoints.instance_id = labeledEntity.instanceId;
                    cached.keypoints.label_id = labelEntry.id;
                    cached.keypoints.template_guid = activeTemplate.templateID.ToString();

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
                            var loc = ConvertToScreenSpace(bone.position);
                            keypoints[i].index = i;
                            keypoints[i].x = loc.x;
                            keypoints[i].y = loc.y;
                            keypoints[i].state = 2;
                        }
                    }
                }

                // Go through all of the additional or override points defined by joint labels and get
                // their locations
                foreach (var (joint, idx) in cachedData.overrides)
                {
                    var loc = ConvertToScreenSpace(joint.transform.position);
                    keypoints[idx].index = idx;
                    keypoints[idx].x = loc.x;
                    keypoints[idx].y = loc.y;
                    keypoints[idx].state = 2;
                }

                cachedData.keypoints.pose = "unset";

                if (cachedData.animator != null)
                {
                    cachedData.keypoints.pose = GetPose(cachedData.animator);
                }

                m_Keypoints[m_CurrentFrame][labeledEntity.instanceId] = cachedData.keypoints;
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

        /// <inheritdoc/>
        protected override void OnVisualize()
        {
            if (m_ToReport == null) return;

            var jointTexture = activeTemplate.jointTexture;
            if (jointTexture == null) jointTexture = m_MissingTexture;

            var skeletonTexture = activeTemplate.skeletonTexture;
            if (skeletonTexture == null) skeletonTexture = m_MissingTexture;

            foreach (var entry in m_ToReport)
            {
                foreach (var bone in activeTemplate.skeleton)
                {
                    var joint1 = entry.keypoints[bone.joint1];
                    var joint2 = entry.keypoints[bone.joint2];

                    if (joint1.state == 2 && joint2.state == 2)
                    {
                        VisualizationHelper.DrawLine(joint1.x, joint1.y, joint2.x, joint2.y, bone.color, 8, skeletonTexture);
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
        }

        [Serializable]
        struct SkeletonJson
        {
            public int joint1;
            public int joint2;
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
            json.template_id = input.templateID.ToString();
            json.template_name = input.templateName;
            json.key_points = new JointJson[input.keypoints.Length];
            json.skeleton = new SkeletonJson[input.skeleton.Length];

            for (var i = 0; i < input.keypoints.Length; i++)
            {
                json.key_points[i] = new JointJson
                {
                    label = input.keypoints[i].label,
                    index = i
                };
            }

            for (var i = 0; i < input.skeleton.Length; i++)
            {
                json.skeleton[i] = new SkeletonJson()
                {
                    joint1 = input.skeleton[i].joint1,
                    joint2 = input.skeleton[i].joint2
                };
            }

            return json;
        }
    }
}
