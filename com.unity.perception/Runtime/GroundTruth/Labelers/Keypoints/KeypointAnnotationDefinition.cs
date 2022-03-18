using System;
using UnityEngine.Perception.GroundTruth.DataModel;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// The definition of the keypoint
    /// </summary>
    public class KeypointAnnotationDefinition : AnnotationDefinition
    {
        internal const string labelerDescription = "Produces keypoint annotations for all visible labeled objects that have a humanoid animation avatar component.";

        /// <inheritdoc/>
        public override string modelType => "type.unity.com/unity.solo.KeypointAnnotation";

        /// <inheritdoc/>
        public override string description => labelerDescription;

        internal Template template;

        internal KeypointAnnotationDefinition(string id) : base(id) { }

        internal KeypointAnnotationDefinition(string id, Template template)
            : base(id)
        {
            this.template = template;
        }

        [Serializable]
        public struct JointDefinition : IMessageProducer
        {
            public string label;
            public int index;
            public Color color;
            public void ToMessage(IMessageBuilder builder)
            {
                builder.AddString("label", label);
                builder.AddInt("index", index);
                builder.AddIntArray("color", MessageBuilderUtils.ToIntVector(color));
            }
        }

        [Serializable]
        public struct SkeletonDefinition : IMessageProducer
        {
            public int joint1;
            public int joint2;
            public Color color;
            public void ToMessage(IMessageBuilder builder)
            {
                builder.AddInt("joint1", joint1);
                builder.AddInt("joint2", joint2);
                builder.AddIntArray("color", MessageBuilderUtils.ToIntVector(color));
            }
        }

        [Serializable]
        public struct Template : IMessageProducer
        {
            public string templateId;
            public string templateName;
            public JointDefinition[] keyPoints;
            public SkeletonDefinition[] skeleton;

            public void ToMessage(IMessageBuilder builder)
            {
                builder.AddString("templateId", templateId);
                builder.AddString("templateName", templateName);

                foreach (var kp in keyPoints)
                {
                    var nested = builder.AddNestedMessageToVector("keypoints");
                    kp.ToMessage(nested);
                }

                foreach (var bone in skeleton)
                {
                    var nested = builder.AddNestedMessageToVector("skeleton");
                    bone.ToMessage(nested);
                }
            }
        }

        public override void ToMessage(IMessageBuilder builder)
        {
            base.ToMessage(builder);
            var nested = builder.AddNestedMessage("template");
            template.ToMessage(nested);
        }
    }
}
