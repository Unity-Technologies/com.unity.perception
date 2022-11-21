using System;
using UnityEngine.Perception.GroundTruth.DataModel;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Labelers
{
    /// <summary>
    /// The definition of the keypoint
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    public class KeypointAnnotationDefinition : AnnotationDefinition
    {
        internal const string labelerDescription = "Produces keypoint annotations for all visible labeled objects that have a humanoid animation avatar component.";

        /// <inheritdoc/>
        public override string modelType => "type.unity.com/unity.solo.KeypointAnnotation";

        /// <inheritdoc/>
        public override string description => labelerDescription;

        internal Template template;

        internal KeypointAnnotationDefinition(string id) : base(id) {}

        internal KeypointAnnotationDefinition(string id, Template template)
            : base(id)
        {
            this.template = template;
        }

        /// <inheritdoc/>
        public override void ToMessage(IMessageBuilder builder)
        {
            base.ToMessage(builder);
            var nested = builder.AddNestedMessage("template");
            template.ToMessage(nested);
        }

        /// <summary>
        /// The definition of a keypoint skeleton joint.
        /// </summary>
        [Serializable]
        public struct JointDefinition : IMessageProducer
        {
            /// <summary>
            /// The label associated with this joint.
            /// </summary>
            public string label;

            /// <summary>
            /// The index of this joint.
            /// </summary>
            public int index;

            /// <summary>
            /// The color associated with this joint.
            /// </summary>
            public Color color;

            /// <inheritdoc/>
            public void ToMessage(IMessageBuilder builder)
            {
                builder.AddString("label", label);
                builder.AddInt("index", index);
                builder.AddIntArray("color", MessageBuilderUtils.ToIntVector(color));
            }
        }

        /// <summary>
        /// A struct defining a bone connection in a keypoint skeleton.
        /// </summary>
        [Serializable]
        public struct SkeletonDefinition : IMessageProducer
        {
            /// <summary>
            /// The id of the first joint connection of this bone.
            /// </summary>
            public int joint1;

            /// <summary>
            /// The id of the second joint connection of this bone.
            /// </summary>
            public int joint2;

            /// <summary>
            /// The color of this bone.
            /// </summary>
            public Color color;

            /// <inheritdoc/>
            public void ToMessage(IMessageBuilder builder)
            {
                builder.AddInt("joint1", joint1);
                builder.AddInt("joint2", joint2);
                builder.AddIntArray("color", MessageBuilderUtils.ToIntVector(color));
            }
        }

        /// <summary>
        /// A struct defining a skeleton of keypoints and their connections.
        /// </summary>
        [Serializable]
        public struct Template : IMessageProducer
        {
            /// <summary>
            /// The id of this template.
            /// </summary>
            public string templateId;

            /// <summary>
            /// The name of this template.
            /// </summary>
            public string templateName;

            /// <summary>
            /// The list of keypoint joints in this template.
            /// </summary>
            public JointDefinition[] keyPoints;

            /// <summary>
            /// The list of joint connections in this template.
            /// </summary>
            public SkeletonDefinition[] skeleton;

            /// <inheritdoc/>
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
    }
}
