using System;
using UnityEngine;

namespace GroundTruthTests
{
    static class TestHelper
    {
        public static GameObject CreateLabeledPlane(float scale = 10, string label = "label")
        {
            GameObject planeObject;
            planeObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            planeObject.transform.SetPositionAndRotation(new Vector3(0, 0, 10), Quaternion.Euler(90, 0, 0));
            planeObject.transform.localScale = new Vector3(scale, -1, scale);
            var labeling = planeObject.AddComponent<Labeling>();
            labeling.classes.Add(label);
            return planeObject;
        }
#if UNITY_EDITOR
        public static void LoadAndStartRenderDocCapture(out UnityEditor.EditorWindow gameView)
        {
            UnityEditorInternal.RenderDoc.Load();
            System.Reflection.Assembly assembly = typeof(UnityEditor.EditorWindow).Assembly;
            Type type = assembly.GetType("UnityEditor.GameView");
            gameView = UnityEditor.EditorWindow.GetWindow(type);
            UnityEditorInternal.RenderDoc.BeginCaptureRenderDoc(gameView);
        }
#endif
    }
}
