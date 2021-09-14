using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Unity.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Perception.GroundTruth;

namespace GroundTruthTests
{
    static class TestHelper
    {
        #if UNITY_EDITOR
        private static EditorWindow s_GameView;
        #endif

        public static GameObject CreateLabeledPlane(float scale = 10, string label = "label")
        {
            GameObject planeObject;
            planeObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            planeObject.transform.SetPositionAndRotation(new Vector3(0, 0, 10), Quaternion.Euler(90, 0, 0));
            planeObject.transform.localScale = new Vector3(scale, -1, scale);
            var labeling = planeObject.AddComponent<Labeling>();
            labeling.labels.Add(label);
            return planeObject;
        }

        public static GameObject CreateLabeledCube(float scale = 10, string label = "label", float x = 0, float y = 0, float z = 0, float roll = 0, float pitch = 0, float yaw = 0)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

            return SetupLabeledCube(cube, scale, label, x, y, z, roll, pitch, yaw);
        }

        public static GameObject SetupLabeledCube(GameObject cube, float scale = 10, string label = "label", float x = 0, float y = 0, float z = 0, float roll = 0, float pitch = 0, float yaw = 0)
        {
            cube.transform.SetPositionAndRotation(new Vector3(x, y, z), Quaternion.Euler(pitch, yaw, roll));
            cube.transform.localScale = new Vector3(scale, scale, scale);
            var labeling = cube.AddComponent<Labeling>();
            labeling.labels.Add(label);
            return cube;
        }

        public static void ReadRenderTextureRawData<T>(RenderTexture renderTexture, Action<NativeArray<T>> callback) where T : struct
        {
            RenderTexture.active = renderTexture;

            var cpuTexture = new Texture2D(renderTexture.width, renderTexture.height, renderTexture.graphicsFormat, TextureCreationFlags.None);

            cpuTexture.ReadPixels(new Rect(
                    Vector2.zero,
                    new Vector2(renderTexture.width, renderTexture.height)),
                0, 0);
            RenderTexture.active = null;
            var data = cpuTexture.GetRawTextureData<T>();
            callback(data);
        }

#if UNITY_EDITOR
        public static void LoadAndStartRenderDocCapture()
        {
            UnityEditorInternal.RenderDoc.Load();
            System.Reflection.Assembly assembly = typeof(UnityEditor.EditorWindow).Assembly;
            Type type = assembly.GetType("UnityEditor.GameView");
            s_GameView = UnityEditor.EditorWindow.GetWindow(type);
            UnityEditorInternal.RenderDoc.BeginCaptureRenderDoc(s_GameView);
        }
        [Conditional("UNITY_EDITOR")]
        public static void EndCaptureRenderDoc()
        {
            UnityEditorInternal.RenderDoc.EndCaptureRenderDoc(s_GameView);
        }
#endif

        public static string NormalizeJson(string json, bool normalizeFormatting = false)
        {
            if (normalizeFormatting)
                json = Regex.Replace(json, "^\\s*", "", RegexOptions.Multiline);

            return json.Replace("\r\n", "\n");
        }
    }
}
