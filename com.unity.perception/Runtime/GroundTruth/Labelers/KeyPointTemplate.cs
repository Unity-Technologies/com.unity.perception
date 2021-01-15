using System;

namespace UnityEngine.Perception.GroundTruth
{
    [Serializable]
    public class KeyPointDefinition
    {
        public string label;
        public bool associateToRig = true;
        public HumanBodyBones rigLabel = HumanBodyBones.Head;
        public Color color;
    }

    [Serializable]
    public class SkeletonDefinition
    {
        public int joint1;
        public int joint2;
        public Color color;
    }

    [CreateAssetMenu(fileName = "KeypointTemplate", menuName = "Perception/Keypoint Template", order = 2)]
    public class KeyPointTemplate : ScriptableObject
    {
        public Guid templateID;
        public string templateName;
        public Texture2D jointTexture;
        public Texture2D skeletonTexture;
        public KeyPointDefinition[] keyPoints;
        public SkeletonDefinition[] skeleton;
    }
}
