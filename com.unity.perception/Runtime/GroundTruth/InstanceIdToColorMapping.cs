using System;
using System.Collections.Generic;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// Static class to procedurally generate a unique color for an instance ID. This algorithm
    /// is deterministic, and will always return the same color for a ID, and the same ID for a color. ID 0 is reserved to
    /// be an invalid ID and is mapped to color black (0,0,0,255). Invalid IDs always map to black, and black always maps to ID 0.
    /// In order to try to create visually contrasting colors for IDs, there are a subset of IDs reserved (1-65)
    /// to be generated by applying the golden ration to find the next color in the HSL spectrum. All of these
    /// colors, and only theses colors, will be in the alpha channel 255. After the first 65 IDs, the color will be
    /// determined by iterating through all available RGB values in the alpha channels from 264 - 1. Alpha channel 0 is marked as invalid.
    /// This service will support over 4 billion unique IDs => colors [(256^4) - (256*2) + 64]
    /// </summary>
   public static class InstanceIdToColorMapping
    {
        // ReSharper disable once MemberCanBePrivate.Global
        /// <summary>
        /// The max ID supported by this class.
        /// </summary>
        public const uint MaxId = uint.MaxValue - ((256 * 256 * 256) * 2) + k_HslCount;

        static Dictionary<uint, uint> s_IdToColorCache;
        static Dictionary<uint, uint> s_ColorToIdCache;
        const uint k_HslCount = 64;
        const uint k_ColorsPerAlpha = 256 * 256 * 256;
        const uint k_InvalidPackedColor = 255; // packed uint for color (0, 0, 0, 255);
        static readonly Color32 k_InvalidColor = new Color(0, 0, 0, 255);
        static readonly float k_GoldenRatio = (1 + Mathf.Sqrt(5)) / 2;
        const int k_HuesInEachValue = 30;

        static void InitializeMaps()
        {

            s_IdToColorCache = new Dictionary<uint, uint>();
            s_ColorToIdCache = new Dictionary<uint, uint>();

            s_IdToColorCache[0] = k_InvalidPackedColor;
            s_IdToColorCache[k_InvalidPackedColor] = 0;

            for (uint i = 1; i <= k_HslCount; i++)
            {
                var color = GenerateHSLValueForId(i);
                s_IdToColorCache[i] = color;
                s_ColorToIdCache[color] = i;
            }
        }

        static uint GenerateHSLValueForId(uint count)
        {
            count -= 1;

            var ratio = count * k_GoldenRatio;
            var hue = ratio - Mathf.Floor(ratio);

            count /= k_HuesInEachValue;
            ratio = count * k_GoldenRatio;
            var value = 1 - (ratio - Mathf.Floor(ratio));

            var color = (Color32)Color.HSVToRGB(hue, 1f, value);
            color.a = 255;
            return GetPackedColorFromColor(color);
        }

        static uint GetColorForId(uint id)
        {
            if (id > MaxId || id == 0 || id == k_InvalidPackedColor) return k_InvalidPackedColor;

            if (id <= k_HslCount)
            {
                if (s_IdToColorCache == null) InitializeMaps();
                return s_IdToColorCache[id];
            }

            var altered_id = id - k_HslCount;
            var rgb = altered_id % k_ColorsPerAlpha;
            var alpha= 254 - (altered_id / k_ColorsPerAlpha);

            return rgb << 8 | alpha;
        }

        static uint GetIdForColor(uint color)
        {
            if (color == 0 || color == k_InvalidPackedColor) return 0;

            var alpha = color & 0xff;

            if (alpha == 255)
            {
                if (s_ColorToIdCache == null) InitializeMaps();
                if (s_ColorToIdCache.TryGetValue(color, out var id))
                {
                    return id;
                }
                else
                {
                    throw new InvalidOperationException($"Passed in color: {color} was not one of the reserved colors for alpha channel 255");
                }
            }
            else
            {
                var rgb = color >> 8;
                var id = k_HslCount + rgb + (256 * 256 * 256) * (254 - alpha);
                return id;
            }
        }

        /// <summary>
        /// Packs a color32 (RGBA - 1 byte per channel) into a 32bit unsigned integer.
        /// </summary>
        /// <param name="color">The RGBA color.</param>
        /// <returns>The packed unsigned int 32 of the color.</returns>
        public static uint GetPackedColorFromColor(Color32 color)
        {
            var tmp = (uint) ((color.r << 24) | (color.g << 16) | (color.b << 8) | (color.a << 0));
            return tmp;
        }

        /// <summary>
        /// Converts a packed color (or unsigned 32bit representation of a color) into an RGBA color.
        /// </summary>
        /// <param name="color">The packed color</param>
        /// <returns>The RGBA color</returns>
        public static Color32 GetColorFromPackedColor(uint color)
        {
            return new Color32((byte)(color >> 24), (byte)(color >> 16), (byte)(color >> 8), (byte)color);
        }

        /// <summary>
        /// Retrieve the color that is mapped to the passed in ID. If the ID is 0 or 255 false will be returned, and
        /// color will be set to black.
        /// </summary>
        /// <param name="id">The ID of interest.</param>
        /// <param name="color">Will be set to the color associated with the passed in ID.</param>
        /// <returns>Returns true if the ID was mapped to a non-black color, otherwise returns false</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if the passed in ID is greater than the largest supported ID <see cref="MaxId"/></exception>
        public static bool TryGetColorFromInstanceId(uint id, out Color32 color)
        {
            if (id > MaxId) throw new IndexOutOfRangeException($"Passed in index: {id} is greater than max ID: {MaxId}");

            color = k_InvalidColor;
            var packed = GetColorForId(id);

            if (packed == k_InvalidPackedColor) return false;
            color = GetColorFromPackedColor(packed);
            return true;
        }

        /// <summary>
        /// Retrieve the color that is mapped to the passed in ID. If the ID is 0 or 255 the returned color will be black.
        /// </summary>
        /// <param name="id">The ID of interest.</param>
        /// <returns>The color associated with the passed in ID, or black if no associated color exists.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if the passed in ID is greater than the largest supported ID <see cref="MaxId"/></exception>
        public static Color32 GetColorFromInstanceId(uint id)
        {
            if (id > MaxId) throw new IndexOutOfRangeException($"Passed in index: {id} is greater than max ID: {MaxId}");

            var color = k_InvalidColor;
            var packed = GetColorForId(id);

            if (packed != k_InvalidPackedColor)
                color = GetColorFromPackedColor(packed);

            return color;
        }

        /// <summary>
        /// Retrieve the ID associated with the passed in color. If the passed in color is black this service will return
        /// false, and the out id will be set to 0.
        /// </summary>
        /// <param name="color">The color to map to an ID.</param>
        /// <param name="id">This value will be updated with the ID for the passed in color.</param>
        /// <returns>This service will return true if an ID is properly mapped to a color, otherwise it will return false.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if the passed in color is mapped to an ID that is greater than the largest supported ID</exception>
        /// <<exception cref="InvalidOperationException">Thrown if the passed in color cannot be mapped to an ID in the alpha 255 range<see cref="MaxId"/></exception>
        public static bool TryGetInstanceIdFromColor(Color32 color, out uint id)
        {
            id = GetIdForColor(GetPackedColorFromColor(color));
            if (id > MaxId) throw new IndexOutOfRangeException($"Passed in color: {color} maps to an ID: {id} which is greater than max ID: {MaxId}");
            return id != 0;
        }

        /// <summary>
        /// Retrieve the ID associated with the passed in color. If the passed in color is black this service will return 0.
        /// </summary>
        /// <param name="color">The color to map to an ID.</param>
        /// <returns>This value will be updated with the ID for the passed in color.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if the passed in color is mapped to an ID that is greater than the largest supported ID</exception>
        /// <<exception cref="InvalidOperationException">Thrown if the passed in color cannot be mapped to an ID in the alpha 255 range<see cref="MaxId"/></exception>
        public static uint GetInstanceIdFromColor(Color32 color)
        {
            var id = GetIdForColor(GetPackedColorFromColor(color));
            if (id > MaxId) throw new IndexOutOfRangeException($"Passed in color: {color} maps to an ID: {id} which is greater than max ID: {MaxId}");
            return id;
        }
    }
}
