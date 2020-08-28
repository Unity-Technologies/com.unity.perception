﻿Shader "Perception/LensDistortion"
{

    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
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
                // https://github.com/Unity-Technologies/Graphics/blob/257b08bba6c11de0f894e42e811124247a522d3c/com.unity.render-pipelines.universal/Runtime/Passes/PostProcessPass.cs

                // TODO: Include this code somehow since we're really only using the DistortUV call

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

                sampler2D _MainTex;

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
