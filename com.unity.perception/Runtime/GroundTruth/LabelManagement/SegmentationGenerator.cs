using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Perception.GroundTruth.LabelManagement
{
    [MovedFrom("UnityEngine.Perception.GroundTruth")]
    class SegmentationGenerator : IGroundTruthGenerator
    {
        static readonly int k_MainColor = Shader.PropertyToID("_MainColor");
        static readonly int k_MainTex = Shader.PropertyToID("_MainTex");
        static readonly int k_MainTexSt = Shader.PropertyToID("_MainTex_ST");

        static readonly int[] k_TextureIds =
        {
            Shader.PropertyToID("_BaseMap"),
            Shader.PropertyToID("_BaseColorMap"),
            Shader.PropertyToID("_UnlitColorMap"),
            Shader.PropertyToID("_MainTex")
        };

        static readonly int[] k_ColorIds =
        {
            Shader.PropertyToID("_BaseColor"),
            Shader.PropertyToID("_UnlitColor"),
            Shader.PropertyToID("_MainColor"),
            Shader.PropertyToID("_Color")
        };

        static readonly int[] k_TextureStIds =
        {
            Shader.PropertyToID("_BaseMap_ST"),
            Shader.PropertyToID("_BaseColorMap_ST"),
            Shader.PropertyToID("_UnlitColorMap_ST"),
            Shader.PropertyToID("_MainTex_ST")
        };

        public void SetupMaterialProperties(
            MaterialPropertyBlock mpb, Renderer renderer, Labeling labeling, Material material, uint instanceId)
        {
            SetMainTexture(mpb, material);
            SetMainColor(mpb, material);
        }

        public void ClearMaterialProperties(
            MaterialPropertyBlock mpb, Renderer renderer, Labeling labeling, uint instanceId) {}

        /// <summary>
        /// Notify the segmentation shader of the texture on the object's URP/HDRP Lit material to avoid rendering
        /// portions of the object that are sufficiently transparent.
        /// </summary>
        /// <param name="mpb"></param>
        /// <param name="material"></param>
        static void SetMainTexture(MaterialPropertyBlock mpb, Material material)
        {
            for (var i = 0; i < k_TextureIds.Length; i++)
            {
                var textureId = k_TextureIds[i];
                if (material.HasProperty(textureId))
                {
                    var sourceTexture = material.GetTexture(textureId);
                    if (sourceTexture != null)
                    {
                        mpb.SetTexture(k_MainTex, sourceTexture);
                        mpb.SetVector(k_MainTexSt, material.GetVector(k_TextureStIds[i]));
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Notify the segmentation shader of the main color on the object's URP/HDRP Lit material to avoid rendering
        /// portions of the object that are sufficiently transparent.
        /// </summary>
        /// <param name="mpb"></param>
        /// <param name="material"></param>
        static void SetMainColor(MaterialPropertyBlock mpb, Material material)
        {
            foreach (var colorId in k_ColorIds)
            {
                if (material.HasProperty(colorId))
                {
                    var color = material.GetColor(colorId);
                    mpb.SetColor(k_MainColor, color);
                    break;
                }
            }
        }
    }
}
