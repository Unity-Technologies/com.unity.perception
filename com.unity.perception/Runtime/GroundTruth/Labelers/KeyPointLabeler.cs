using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Produces keypoint annotations for a humanoid model. This labeler supports generic
    /// <see cref="KeyPointTemplate"/>. Template values are mapped to rigged
    /// <see cref="Animator"/> <seealso cref="Avatar"/>. Custom joints can be
    /// created by applying <see cref="JointLabel"/> to empty game objects at a body
    /// part's location.
    /// </summary>
    [Serializable]
    public sealed class KeyPointLabeler : CameraLabeler
    {
        /// <summary>
        /// The active keypoint template. Required to annotate keypoint data.
        /// </summary>
        public KeyPointTemplate activeTemplate;

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

        /// <inheritdoc/>
        protected override void Setup()
        {
            if (idLabelConfig == null)
                throw new InvalidOperationException("KeyPointLabeler's idLabelConfig field must be assigned");

            m_AnnotationDefinition = DatasetCapture.RegisterAnnotationDefinition("keypoints", new []{TemplateToJson(activeTemplate)},
                "pixel coordinates of keypoints in a model, along with skeletal connectivity data", id: new Guid(annotationId));

            m_EntityQuery = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(Labeling), typeof(GroundTruthInfo));

            m_KeyPointEntries = new List<KeyPointEntry>();

            // Texture to use in case the template does not contain a texture for the joints or the skeletal connections
            m_MissingTexture = new Texture2D(1, 1);

            m_KnownStatus = new Dictionary<uint, CachedData>();
        }

        /// <inheritdoc/>
        protected override void OnBeginRendering()
        {
            var reporter = perceptionCamera.SensorHandle.ReportAnnotationAsync(m_AnnotationDefinition);

            var entities = m_EntityQuery.ToEntityArray(Allocator.TempJob);
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            m_KeyPointEntries.Clear();

            foreach (var entity in entities)
            {
                ProcessEntity(entityManager.GetComponentObject<Labeling>(entity));
            }

            entities.Dispose();

            reporter.ReportValues(m_KeyPointEntries);
        }

        // ReSharper disable InconsistentNaming
        // ReSharper disable NotAccessedField.Global
        // ReSharper disable NotAccessedField.Local
        [Serializable]
        struct KeyPointEntry
        {
            /// <summary>
            /// The id of the labeled entity
            /// </summary>
            public int label_id;
            public uint instance_id;
            public string template_guid;
            public KeyPoint[] keypoints;
        }

        [Serializable]
        struct KeyPoint
        {
            public int index;
            public float x;
            public float y;
            public int state;
        }
        // ReSharper restore InconsistentNaming
        // ReSharper restore NotAccessedField.Global
        // ReSharper restore NotAccessedField.Local

        // Converts a coordinate from world space into pixel space
        Vector3 ConvertToScreenSpace(Vector3 worldLocation)
        {
            var pt = perceptionCamera.attachedCamera.WorldToScreenPoint(worldLocation);
            pt.y = Screen.height - pt.y;
            return pt;
        }

        List<KeyPointEntry> m_KeyPointEntries;

        struct CachedData
        {
            public bool status;
            public Animator animator;
            public KeyPointEntry keyPoints;
            public List<(JointLabel, int)> overrides;
        }

        Dictionary<uint, CachedData> m_KnownStatus;

        bool TryToGetTemplateIndexForJoint(KeyPointTemplate template, JointLabel joint, out int index)
        {
            index = -1;

            foreach (var jointTemplate in joint.templateInformation.Where(jointTemplate => jointTemplate.template == template))
            {
                for (var i = 0; i < template.keyPoints.Length; i++)
                {
                    if (template.keyPoints[i].label == jointTemplate.label)
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
                    if (activeTemplate.keyPoints.Any(i => i.label == template.label))
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
                    keyPoints = new KeyPointEntry()
                };

                if (idLabelConfig.TryGetLabelEntryFromInstanceId(labeledEntity.instanceId, out var labelEntry))
                {
                    var entityGameObject = labeledEntity.gameObject;
                    var animator = entityGameObject.transform.GetComponentInChildren<Animator>();
                    if (animator != null)
                    {
                        var avatar = animator.avatar;
                        if (avatar.isValid && avatar.isHuman)
                        {
                            cached.animator = animator;
                            cached.keyPoints.instance_id = labeledEntity.instanceId;
                            cached.keyPoints.label_id = labelEntry.id;
                            cached.keyPoints.template_guid = activeTemplate.templateID.ToString();
                            cached.keyPoints.keypoints = new KeyPoint[activeTemplate.keyPoints.Length];

                            for (var i = 0; i < cached.keyPoints.keypoints.Length; i++)
                            {
                                cached.keyPoints.keypoints[i].index = i;
                                cached.keyPoints.keypoints[i].state = 0;
                            }

                            cached.overrides = new List<(JointLabel, int)>();
                            cached.status = true;

                            foreach (var joint in entityGameObject.transform.GetComponentsInChildren<JointLabel>())
                            {
                                if (TryToGetTemplateIndexForJoint(activeTemplate, joint, out var idx))
                                {
                                    cached.overrides.Add((joint, idx));
                                }
                            }
                        }
                    }
                }

                m_KnownStatus[labeledEntity.instanceId] = cached;
            }

            var cachedData = m_KnownStatus[labeledEntity.instanceId];

            if (cachedData.status)
            {
                var animator = cachedData.animator;
                var keyPoints = cachedData.keyPoints.keypoints;

                // Go through all of the rig keypoints and get their location
                for (var i = 0; i < activeTemplate.keyPoints.Length; i++)
                {
                    var pt = activeTemplate.keyPoints[i];
                    if (pt.associateToRig)
                    {
                        var loc = ConvertToScreenSpace(animator.GetBoneTransform(pt.rigLabel).position);
                        keyPoints[i].index = i;
                        keyPoints[i].x = loc.x;
                        keyPoints[i].y = loc.y;
                        keyPoints[i].state = 1;
                    }
                }

                // Go through all of the additional or override points defined by joint labels and get
                // their locations
                foreach (var (joint, idx) in cachedData.overrides)
                {
                    var loc = ConvertToScreenSpace(joint.transform.position);
                    keyPoints[idx].index = idx;
                    keyPoints[idx].x = loc.x;
                    keyPoints[idx].y = loc.y;
                    keyPoints[idx].state = 1;
                }

                m_KeyPointEntries.Add(cachedData.keyPoints);
            }
        }

        Rect ToBoxRect(float x, float y, float halfSize = 3.0f)
        {
            return new Rect(x - halfSize, y - halfSize, halfSize * 2, halfSize * 2);
        }

        void DrawPoint(float x, float y, Color color, Texture2D texture)
        {
            var oldColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(ToBoxRect(x, y, 4), texture);
            GUI.color = oldColor;
        }

        float Magnitude(float p1X, float p1Y, float p2X, float p2Y)
        {
            var x = p2X - p1X;
            var y = p2Y - p1Y;
            return Mathf.Sqrt(x * x + y * y);
        }

        void DrawLine (float p1X, float p1Y, float p2X, float p2Y, Color color, Texture texture)
        {
            var oldColor = GUI.color;

            GUI.color = color;

            var matrixBackup = GUI.matrix;
            const float width = 8.0f;
            var angle = Mathf.Atan2 (p2Y - p1Y, p2X - p1X) * 180f / Mathf.PI;

            var length = Magnitude(p1X, p1Y, p2X, p2Y);

            GUIUtility.RotateAroundPivot (angle, new Vector2(p1X, p1Y));
            const float halfWidth = width * 0.5f;
            GUI.DrawTexture (new Rect (p1X - halfWidth, p1Y - halfWidth, length, width), texture);

            GUI.matrix = matrixBackup;
            GUI.color = oldColor;
        }

        /// <inheritdoc/>
        protected override void OnVisualize()
        {
            var jointTexture = activeTemplate.jointTexture;
            if (jointTexture == null) jointTexture = m_MissingTexture;

            var skeletonTexture = activeTemplate.skeletonTexture;
            if (skeletonTexture == null) skeletonTexture = m_MissingTexture;

            foreach (var entry in m_KeyPointEntries)
            {
                foreach (var bone in activeTemplate.skeleton)
                {
                    var joint1 = entry.keypoints[bone.joint1];
                    var joint2 = entry.keypoints[bone.joint2];

                    if (joint1.state != 0 && joint2.state != 0)
                    {
                        DrawLine(joint1.x, joint1.y, joint2.x, joint2.y, bone.color, skeletonTexture);
                    }
                }

                foreach (var keypoint in entry.keypoints)
                {
                    if (keypoint.state != 0)
                        DrawPoint(keypoint.x, keypoint.y, activeTemplate.keyPoints[keypoint.index].color, jointTexture);
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
        struct KeyPointJson
        {
            public string template_id;
            public string template_name;
            public JointJson[] key_points;
            public SkeletonJson[] skeleton;
        }
        // ReSharper restore InconsistentNaming
        // ReSharper restore NotAccessedField.Local

        KeyPointJson TemplateToJson(KeyPointTemplate input)
        {
            var json = new KeyPointJson();
            json.template_id = input.templateID.ToString();
            json.template_name = input.templateName;
            json.key_points = new JointJson[input.keyPoints.Length];
            json.skeleton = new SkeletonJson[input.skeleton.Length];

            for (var i = 0; i < input.keyPoints.Length; i++)
            {
                json.key_points[i] = new JointJson
                {
                    label = input.keyPoints[i].label,
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
