using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.Utilities
{
    /// <summary>
    /// Helper class that contains common visualization methods useful to ground truth labelers.
    /// </summary>
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    static class VisualizationHelper
    {
        static Texture2D s_OnePixel = new Texture2D(1, 1);
        static Vector2 s_StandardScreenDimensions = new Vector2(1024, 768);
        static float s_DrawScalar = float.NaN;

        /// <summary>
        /// Converts a 3D world space coordinate to image pixel space.
        /// </summary>
        /// <param name="camera">The rendering camera</param>
        /// <param name="worldLocation">The 3D world location to convert</param>
        /// <returns>The coordinate in pixel space</returns>
        public static Vector3 ConvertToScreenSpace(Camera camera, Vector3 worldLocation)
        {
            var pt = camera.WorldToScreenPoint(worldLocation);
            pt.y = Screen.height - pt.y;
            return pt;
        }

        static Rect ToBoxRect(float x, float y, float halfSize = 3.0f)
        {
            return new Rect(x - halfSize, y - halfSize, halfSize * 2, halfSize * 2);
        }

        /// <summary>
        /// Draw a point (in pixel space) on the screen
        /// </summary>
        /// <param name="pt">The point location, in pixel space</param>
        /// <param name="color">The color of the point</param>
        /// <param name="width">The width of the point</param>
        /// <param name="texture">The texture to use for the point, defaults to a solid pixel</param>
        public static void DrawPoint(Vector3 pt, Color color, float width = 4.0f, Texture texture = null)
        {
            DrawPoint(pt.x, pt.y, color, width, texture);
        }

        /// <summary>
        /// Draw a point (in pixel space) on the screen
        /// </summary>
        /// <param name="x">The point's x value, in pixel space</param>
        /// <param name="y">The point's y value, in pixel space</param>
        /// <param name="color">The color of the point</param>
        /// <param name="width">The width of the point</param>
        /// <param name="texture">The texture to use for the point, defaults to a solid pixel</param>
        public static void DrawPoint(float x, float y, Color color, float width = 4, Texture texture = null)
        {
            if (texture == null) texture = s_OnePixel;
            if (float.IsNaN(s_DrawScalar))
            {
                var widthRatio = Screen.width / s_StandardScreenDimensions.x;
                var heightRatio = Screen.height / s_StandardScreenDimensions.y;
                s_DrawScalar = Mathf.Max(widthRatio, heightRatio);
            }

            width *= s_DrawScalar;
            var oldColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(ToBoxRect(x, y, width * 0.5f), texture);
            GUI.color = oldColor;
        }

        static float Magnitude(float p1X, float p1Y, float p2X, float p2Y)
        {
            var x = p2X - p1X;
            var y = p2Y - p1Y;
            return Mathf.Sqrt(x * x + y * y);
        }

        /// <summary>
        /// Draw's a texture between two locations of a passed in width.
        /// </summary>
        /// <param name="p1">The start point in pixel space</param>
        /// <param name="p2">The end point in pixel space</param>
        /// <param name="color">The color of the line</param>
        /// <param name="width">The width of the line</param>
        /// <param name="texture">The texture to use, if null, will draw a solid line of passed in color</param>
        public static void DrawLine(Vector2 p1, Vector2 p2, Color color, float width = 3.0f, Texture texture = null)
        {
            DrawLine(p1.x, p1.y, p2.x, p2.y, color, width, texture);
        }

        /// <summary>
        /// Draw's a texture between two locations of a passed in width.
        /// </summary>
        /// <param name="p1X">The start point's x coordinate in pixel space</param>
        /// <param name="p1Y">The start point's y coordinate in pixel space</param>
        /// <param name="p2X">The end point's x coordinate in pixel space</param>
        /// <param name="p2Y">The end point's y coordinate in pixel space</param>
        /// <param name="color">The color of the line</param>
        /// <param name="width">The width of the line</param>
        /// <param name="texture">The texture to use, if null, will draw a solid line of passed in color</param>
        public static void DrawLine(float p1X, float p1Y, float p2X, float p2Y, Color color, float width = 3.0f, Texture texture = null)
        {
            if (texture == null) texture = s_OnePixel;

            if (float.IsNaN(s_DrawScalar))
            {
                var widthRatio = Screen.width / s_StandardScreenDimensions.x;
                var heightRatio = Screen.height / s_StandardScreenDimensions.y;
                s_DrawScalar = Mathf.Max(widthRatio, heightRatio);
            }

            width *= s_DrawScalar;
            var oldColor = GUI.color;

            GUI.color = color;

            var matrixBackup = GUI.matrix;
            var angle = Mathf.Atan2(p2Y - p1Y, p2X - p1X) * 180f / Mathf.PI;

            var length = Magnitude(p1X, p1Y, p2X, p2Y);

            GUIUtility.RotateAroundPivot(angle, new Vector2(p1X, p1Y));
            var halfWidth = width * 0.5f;
            GUI.DrawTexture(new Rect(p1X - halfWidth, p1Y - halfWidth, length, width), texture);

            GUI.matrix = matrixBackup;
            GUI.color = oldColor;
        }
    }
}
