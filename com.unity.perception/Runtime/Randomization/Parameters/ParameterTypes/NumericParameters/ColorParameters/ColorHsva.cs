using System;
using Unity.Mathematics;

namespace UnityEngine.Perception.Randomization.Parameters
{
    /// <summary>
    /// A struct representing the hue, saturation, value, and alpha components of a particular color
    /// </summary>
    [Serializable]
    public struct ColorHsva
    {
        /// <summary>
        /// A float value representing the hue component of a color
        /// </summary>
        public float h;

        /// <summary>
        /// A float value representing the saturation component of a color
        /// </summary>
        public float s;

        /// <summary>
        /// A float value representing the value component of a color
        /// </summary>
        public float v;

        /// <summary>
        /// A float value representing the alpha component of a color
        /// </summary>
        public float a;

        /// <summary>
        /// Constructs an ColorHsva struct
        /// </summary>
        /// <param name="h">Hue</param>
        /// <param name="s">Saturation</param>
        /// <param name="v">Value</param>
        /// <param name="a">Alpha</param>
        public ColorHsva(float h, float s, float v, float a)
        {
            this.h = h;
            this.s = s;
            this.v = v;
            this.a = a;
        }

        /// <summary>
        /// Implicitly converts an HSVA Color to a float4
        /// </summary>
        /// <param name="c">The HSVA color to convert to a float4</param>
        /// <returns>A new float4</returns>
        public static implicit operator float4(ColorHsva c) => new float4(c.h, c.s, c.v, c.a);

        /// <summary>
        /// Implicitly converts an float4 to an HSVA color
        /// </summary>
        /// <param name="f">The float4 to convert to an HSVA color</param>
        /// <returns>A new HSVA color</returns>
        public static implicit operator ColorHsva(float4 f) => new ColorHsva(f.x, f.y, f.z, f.w);

        /// <summary>
        /// Implicitly converts an HSVA Color to a Vector4
        /// </summary>
        /// <param name="c">The HSVA color to convert to a Vector4</param>
        /// <returns>A new Vector4</returns>
        public static implicit operator Vector4(ColorHsva c) => new float4(c.h, c.s, c.v, c.a);

        /// <summary>
        /// Implicitly converts an Vector4 to an HSVA color
        /// </summary>
        /// <param name="v">The Vector4 color to convert to an HSVA color</param>
        /// <returns>A new HSVA color</returns>
        public static implicit operator ColorHsva(Vector4 v) => new ColorHsva(v.x, v.y, v.z, v.w);

        /// <summary>
        /// Converts an HSVA Color to an RGBA Color
        /// </summary>
        /// <param name="c">The HSVA color to convert to RGBA</param>
        /// <returns>A new RGBA color</returns>
        public static explicit operator Color(ColorHsva c)
        {
            var color = Color.HSVToRGB(c.h, c.s, c.v);
            color.a = c.a;
            return color;
        }

        /// <summary>
        /// Converts an RGBA Color to an HSVA Color
        /// </summary>
        /// <param name="c">The RGBA color to convert to HSVA</param>
        /// <returns>A new HSVA color</returns>
        public static explicit operator ColorHsva(Color c)
        {
            Color.RGBToHSV(c, out var h, out var s, out var v);
            return new ColorHsva(h, s, v, c.a);
        }

        /// <summary>
        /// Generates a string representation of a ColorHsva
        /// </summary>
        /// <returns>A string representing the components of this ColorHsva</returns>
        public override string ToString()
        {
            return $"ColorHsva({h}, {s}, {v}, {a})";
        }
    }
}
