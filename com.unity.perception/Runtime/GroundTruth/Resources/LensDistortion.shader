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

    /*
    HLSLINCLUDE
        #pragma exclude_renderers gles
        #pragma multi_compile_local_fragment _ _DISTORTION

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
        //#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        //#include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"

        half4 frag(FullscreenVaryings input) : SV_Target
        {
            //UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float2 uv = UnityStereoTransformScreenSpaceTex(input.uv);
            float2 uvDistorted = DistortUV(uv);

            half3 color = (0.0).xxx;

            return half4(color, 1.0);
        }

    ENDHLSL
    */

    SubShader
    {
        //Tags {  "RenderPipeline" = "UniversalPipeline"}
        Tags { "RenderType" = "Opaque" "LightMode" = "SRP" }
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {

            Name "LensDistortion"

            CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                #include "UnityCG.cginc"

                struct appdata
                {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                };

                struct v2f
                {
                    float2 uv : TEXCOORD0;
                    float4 vertex : SV_POSITION;
                };

                v2f vert (appdata v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = v.uv;
                    return o;
                }

                // Note: This is taken from the HDRP UberPost shader:
                // https://github.com/Unity-Technologies/Graphics/blob/master/com.unity.render-pipelines.universal/Shaders/PostProcessing/UberPost.shader
                // TODO: Include this code somehow since we're really only using the DistortUV call

                //float4 _Distortion_Params1;
                //float4 _Distortion_Params2;

                /*#define DistCenter              _Distortion_Params1.xy
                #define DistAxis                _Distortion_Params1.zw
                #define DistTheta               _Distortion_Params2.x
                #define DistSigma               _Distortion_Params2.y
                #define DistScale               _Distortion_Params2.z
                #define DistIntensity           _Distortion_Params2.w*/

                float2 DistortUV(float2 uv)
                {
                    // TODO: Grab these from the camera / volume or whatever
                    //float4 _Distortion_Params1 = float4(0.0f, 0.0f, 1.0f, .5f);
                    //float4 _Distortion_Params2 = float4(0.0f, 0.25f, 1.0f, 0.713f);;

                    float2 DistCenter = float2(0.0f, 0.0f);
                    float2 DistAxis = float2(1.0f, 1.0f);
                    //float DistTheta = _Distortion_Params2.x;
                    //float DistSigma = _Distortion_Params2.y;
                    float DistScale = 1.0f;
                    float DistIntensity = 0.713f;

                    // https://github.com/Unity-Technologies/Graphics/blob/257b08bba6c11de0f894e42e811124247a522d3c/com.unity.render-pipelines.universal/Runtime/Passes/PostProcessPass.cs
                    // This will be passed in from CPU eventually
                    float DistTheta = (115.0f * (3.14f / 180.0f));
                    float DistSigma = 2.0f * tan(DistTheta * 0.5f);

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

                    return uv;
                }

                //#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
                //#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

                sampler2D _MainTex;
                //SamplerState sampler_MainTex;

                fixed4 frag(v2f input) : SV_Target
                {
                    float2 uvDistorted = input.uv;
                    uvDistorted = DistortUV(uvDistorted);
                    float4 texValue = tex2D(_MainTex, uvDistorted);

                    return texValue;
                }

            ENDCG
        }
    }
}
