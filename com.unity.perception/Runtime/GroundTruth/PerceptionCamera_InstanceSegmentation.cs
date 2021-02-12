using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

#if HDRP_PRESENT
    using UnityEngine.Rendering.HighDefinition;
#elif URP_PRESENT
    using UnityEngine.Rendering.Universal;
#endif

namespace UnityEngine.Perception.GroundTruth
{
    partial class PerceptionCamera
    {
        /// <summary>
        /// Invoked when instance segmentation images are read back from the graphics system. The first parameter is the
        /// Time.frameCount at which the objects were rendered. May be invoked many frames after the objects were rendered.
        /// </summary>
        public event Action<int, NativeArray<Color32>, RenderTexture> InstanceSegmentationImageReadback;

        /// <summary>
        /// Invoked when RenderedObjectInfos are calculated. The first parameter is the Time.frameCount at which the
        /// objects were rendered. This may be called many frames after the objects were rendered.
        /// </summary>
        public event Action<int, NativeArray<RenderedObjectInfo>> RenderedObjectInfosCalculated;

        RenderedObjectInfoGenerator m_RenderedObjectInfoGenerator;
        RenderTexture m_InstanceSegmentationTexture;
        RenderTextureReader<Color32> m_InstanceSegmentationReader;

        internal bool m_fLensDistortionEnabled = false;

#if HDRP_PRESENT || URP_PRESENT
        private float? m_LensDistortionIntensityOverride;
    #if HDRP_PRESENT
        InstanceSegmentationPass m_InstanceSegmentationPass;
        LensDistortionPass m_LensDistortionPass;
    #elif URP_PRESENT
        InstanceSegmentationUrpPass m_InstanceSegmentationPass;
        LensDistortionUrpPass m_LensDistortionPass;
    #endif

        internal void OverrideLensDistortionIntensity(float? intensity)
        {
            m_LensDistortionIntensityOverride = intensity;
            if (m_LensDistortionPass != null)
                m_LensDistortionPass.m_LensDistortionCrossPipelinePass.lensDistortionOverride = intensity;
        }
#endif

        void SetupInstanceSegmentation()
        {
            var myCamera = GetComponent<Camera>();
            var width = myCamera.pixelWidth;
            var height = myCamera.pixelHeight;
            m_InstanceSegmentationTexture = new RenderTexture(new RenderTextureDescriptor(width, height, GraphicsFormat.R8G8B8A8_UNorm, 8));
            m_InstanceSegmentationTexture.filterMode = FilterMode.Point;
            m_InstanceSegmentationTexture.name = "InstanceSegmentation";

            m_RenderedObjectInfoGenerator = new RenderedObjectInfoGenerator();

#if HDRP_PRESENT || URP_PRESENT
#if HDRP_PRESENT
            var customPassVolume = this.GetComponent<CustomPassVolume>() ?? gameObject.AddComponent<CustomPassVolume>();
            customPassVolume.injectionPoint = CustomPassInjectionPoint.BeforeRendering;
            customPassVolume.isGlobal = true;
            m_InstanceSegmentationPass = new InstanceSegmentationPass()
            {
                name = "Instance segmentation pass",
                targetCamera = GetComponent<Camera>(),
                targetTexture = m_InstanceSegmentationTexture
            };
            m_InstanceSegmentationPass.EnsureInit();
            customPassVolume.customPasses.Add(m_InstanceSegmentationPass);


            m_LensDistortionPass = new LensDistortionPass(GetComponent<Camera>(), m_InstanceSegmentationTexture)
            {
                name = "Instance Segmentation Lens Distortion Pass"
            };
            m_LensDistortionPass.EnsureInit();
            customPassVolume.customPasses.Add(m_LensDistortionPass);

            m_fLensDistortionEnabled = true;
#elif URP_PRESENT
            m_InstanceSegmentationPass = new InstanceSegmentationUrpPass(myCamera, m_InstanceSegmentationTexture);
            AddScriptableRenderPass(m_InstanceSegmentationPass);

            // Lens Distortion
            m_LensDistortionPass = new LensDistortionUrpPass(myCamera, m_InstanceSegmentationTexture);
            AddScriptableRenderPass(m_LensDistortionPass);

            m_fLensDistortionEnabled = true;
#endif
            m_LensDistortionPass.m_LensDistortionCrossPipelinePass.lensDistortionOverride =
                m_LensDistortionIntensityOverride;
#endif

            m_InstanceSegmentationReader = new RenderTextureReader<Color32>(m_InstanceSegmentationTexture, myCamera, (frameCount, data, tex) =>
            {
                InstanceSegmentationImageReadback?.Invoke(frameCount, data, tex);
                if (RenderedObjectInfosCalculated != null)
                {
                    m_RenderedObjectInfoGenerator.Compute(data, tex.width, BoundingBoxOrigin.TopLeft, out var renderedObjectInfos, Allocator.Temp);
                    RenderedObjectInfosCalculated?.Invoke(frameCount, renderedObjectInfos);
                    renderedObjectInfos.Dispose();
                }
            });
        }

        void CleanUpInstanceSegmentation()
        {
            if (m_InstanceSegmentationTexture != null)
                m_InstanceSegmentationTexture.Release();

            m_InstanceSegmentationTexture = null;

            m_InstanceSegmentationReader?.WaitForAllImages();
            m_InstanceSegmentationReader?.Dispose();
            m_InstanceSegmentationReader = null;
        }
    }
}
