using System;

namespace UnityEngine.Perception
{
    public struct RenderedObjectInfo : IEquatable<RenderedObjectInfo>
    {
        public int instanceId;
        public int labelId;
        public Rect boundingBox;
        public int pixelCount;

        public override string ToString()
        {
            return $"{nameof(instanceId)}: {instanceId}, {nameof(labelId)}: {labelId}, {nameof(boundingBox)}: {boundingBox}, {nameof(pixelCount)}: {pixelCount}";
        }

        public bool Equals(RenderedObjectInfo other)
        {
            return instanceId == other.instanceId && labelId == other.labelId && boundingBox.Equals(other.boundingBox) && pixelCount == other.pixelCount;
        }

        public override bool Equals(object obj)
        {
            return obj is RenderedObjectInfo other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = instanceId;
                hashCode = (hashCode * 397) ^ labelId;
                hashCode = (hashCode * 397) ^ boundingBox.GetHashCode();
                hashCode = (hashCode * 397) ^ pixelCount;
                return hashCode;
            }
        }
    }
}
