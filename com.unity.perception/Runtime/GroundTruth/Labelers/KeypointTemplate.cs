using System;
using UnityEngine.Serialization;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// A definition of a keypoint (joint).
    /// </summary>
    [Serializable]
    public class KeypointDefinition
    {
        internal const float defaultSelfOcclusionDistance = 0.15f;
        /// <summary>
        /// The name of the keypoint
        /// </summary>
        public string label;
        /// <summary>
        /// Does this keypoint map directly to a <see cref="Animator"/> <see cref="Avatar"/> <see cref="HumanBodyBones"/>
        /// </summary>
        public bool associateToRig = true;
        /// <summary>
        /// The associated <see cref="HumanBodyBones"/> of the rig
        /// </summary>
        public HumanBodyBones rigLabel = HumanBodyBones.Head;
        /// <summary>
        /// The color of the keypoint in the visualization
        /// </summary>
        public Color color = Color.blue;
        /// <summary>
        /// The max distance the keypoint can be behind an object before it is considered occluded. Distance is computed
        /// in the local space of the joint, so object scale is computed
        /// </summary>
        public float selfOcclusionDistance = defaultSelfOcclusionDistance;
    }

    /// <summary>
    /// A skeletal connection between two joints.
    /// </summary>
    [Serializable]
    public class SkeletonDefinition
    {
        /// <summary>
        /// The first joint
        /// </summary>
        public int joint1;
        /// <summary>
        /// The second joint
        /// </summary>
        public int joint2;
        /// <summary>
        /// The color of the skeleton in the visualization
        /// </summary>
        public Color color = Color.blue;
    }

    /// <summary>
    /// Template used to define the keypoints of a humanoid asset.
    /// </summary>
    [CreateAssetMenu(fileName = "KeypointTemplate", menuName = "Perception/Keypoint Template", order = 2)]
    public class KeypointTemplate : ScriptableObject
    {
        /// <summary>
        /// The <see cref="Guid"/> of the template
        /// </summary>
        public string templateID = Guid.NewGuid().ToString();
        /// <summary>
        /// The name of the template
        /// </summary>
        public string templateName;
        /// <summary>
        /// Texture to use for the visualization of the joint.
        /// </summary>
        public Texture2D jointTexture;
        /// <summary>
        /// Texture to use for the visualization of an occluded joint.
        /// </summary>
        public Texture2D occludedJointTexture;
        /// <summary>
        /// Color to use for the visualization of an occluded joint.
        /// </summary>
        public Color occludedJointColor = Color.black;
        /// <summary>
        /// Texture to use for the visualization of the skeletal connection.
        /// </summary>
        public Texture2D skeletonTexture;
        /// <summary>
        /// Array of <see cref="KeypointDefinition"/> for the template.
        /// </summary>
        [FormerlySerializedAs("keyPoints")]
        public KeypointDefinition[] keypoints;
        /// <summary>
        /// Array of the <see cref="SkeletonDefinition"/> for the template.
        /// </summary>
        public SkeletonDefinition[] skeleton;
    }
}
