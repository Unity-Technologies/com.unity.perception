Shader "Perception/LensDistortion"
{
    //Properties
    //{
    //    [PerObjectData] _SegmentationId("Segmentation ID", int) = 0
    //}

    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    HLSLINCLUDE
        #pragma exclude_renderers gles
        #pragma multi_compile_local_fragment _ _DISTORTION

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
        //#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        //#include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"

        TEXTURE2D(_LensDirt_Texture);

        float4 _Distortion_Params1;
        float4 _Distortion_Params2;

        #define DistCenter              _Distortion_Params1.xy
        #define DistAxis                _Distortion_Params1.zw
        #define DistTheta               _Distortion_Params2.x
        #define DistSigma               _Distortion_Params2.y
        #define DistScale               _Distortion_Params2.z
        #define DistIntensity           _Distortion_Params2.w

        float2 DistortUV(float2 uv)
        {
            // Note: this variant should never be set with XR
            #if _DISTORTION
            {
                uv = (uv - 0.5) * DistScale + 0.5;
                float2 ruv = DistAxis * (uv - 0.5 - DistCenter);
                float ru = length(float2(ruv));

                UNITY_BRANCH
                if (DistIntensity > 0.0)
                {
                    float wu = ru * DistTheta;
                    ru = tan(wu) * (rcp(ru * DistSigma));
                    uv = uv + ruv * (ru - 1.0);
                }
                else
                {
                    ru = rcp(ru) * DistTheta * atan(ru * DistSigma);
                    uv = uv + ruv * (ru - 1.0);
                }
            }
            #endif

            return uv;
        }

        half4 Frag(FullscreenVaryings input) : SV_Target
        {
            //UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float2 uv = UnityStereoTransformScreenSpaceTex(input.uv);
            float2 uvDistorted = DistortUV(uv);

            half3 color = (0.0).xxx;

            return half4(color, 1.0);
        }

    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            // Note: This is taken from the HDRP UberPost shader:
            // https://github.com/Unity-Technologies/Graphics/blob/master/com.unity.render-pipelines.universal/Shaders/PostProcessing/UberPost.shader
            // TODO: Include this code somehow since we're really only using the DistortUV call
            Name "LensDistortion"

            HLSLPROGRAM
                #pragma vertex FullscreenVert
                #pragma fragment Frag
            ENDHLSL
        }
    }
}
