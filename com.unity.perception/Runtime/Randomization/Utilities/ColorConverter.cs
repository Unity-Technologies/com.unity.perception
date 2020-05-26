using Unity.Mathematics;
using UnityEngine;

namespace UnityEngine.Perception.Randomization.Utilities
{
    public static class ColorConverter
    {
        public static float4 ToFloat4(this Color c) { return new float4(c.r, c.g, c.b, c.a); }
        public static Color ToColor(this float4 f) { return new Color(f.x, f.y, f.z, f.w); }
    }
}
