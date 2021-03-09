#if URP_PRESENT
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Perception.GroundTruth
{
    public class GroundTruthRendererFeature : ScriptableRendererFeature
    {
        public override void Create() {}

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var cameraObject = renderingData.cameraData.camera.gameObject;
            var perceptionCamera = cameraObject.GetComponent<PerceptionCamera>();

            if (perceptionCamera == null)
                return;

#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
                return;
#endif
            perceptionCamera.MarkGroundTruthRendererFeatureAsPresent();
            foreach (var pass in perceptionCamera.passes)
                renderer.EnqueuePass(pass);
        }
    }
}
#endif
