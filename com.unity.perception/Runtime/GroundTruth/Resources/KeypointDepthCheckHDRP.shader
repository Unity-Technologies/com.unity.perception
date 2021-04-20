Shader "Perception/KeypointDepthCheck"
{
    Properties
    {
        _Positions("Positions", 2D) = "defaultTexture" {}
        _KeypointCheckDepth("KeypointCheckDepth", 2D) = "defaultTexture" {}
        _DepthTexture("Depth", 2DArray) = "defaultTexture" {}
    }

    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

    //enable GPU instancing support
    #pragma multi_compile_instancing

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Tags { "LightMode" = "SRP" }

            Name "KeypointDepthCheck"
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM

            #pragma multi_compile HDRP_DISABLED HDRP_ENABLED
            #pragma only_renderers d3d11 vulkan metal
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment FullScreenPass


            //UNITY_DECLARE_TEX2DARRAY(_DepthTexture);
            //sampler2D _CameraDepthTexture;

            Texture2D _Positions;
            SamplerState my_point_clamp_sampler;
            Texture2D _KeypointCheckDepth;

#if HDRP_ENABLED

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"

            float4 FullScreenPass(Varyings varyings) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);

                float4 checkPosition = _Positions.Load(float3(varyings.positionCS.xy, 0));
                float4 checkDepth = _KeypointCheckDepth.Load(float3(varyings.positionCS.xy, 0));

                float depth = LoadCameraDepth(float2(checkPosition.x, _ScreenSize.y - checkPosition.y));
                depth = LinearEyeDepth(depth, _ZBufferParams);
                // float depth = UNITY_SAMPLE_TEX2DARRAY(_DepthTexture, float3(checkPosition.xy, 0)).r; //SAMPLE_DEPTH_TEXTURE(_DepthTexture, checkPosition.xy);
                //float depth_decoded = LinearEyeDepth(depth);
                // float depth_decoded = Linear01Depth(depth);
                uint result = depth > checkDepth.x ? 1 : 0;
                return float4(result, result, result, 1);
            }
#else
            /// Dummy Implementation for non HDRP_ENABLED variants

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv     : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f Vert(appdata v)
            {
                v2f o;
                o.uv     = float2(0, 0);
                o.vertex = float4(0, 0, 0, 0);
                return o;
            }

            float4 FullScreenPass(v2f i) : SV_Target
            {
                return float4(0, 0, 0, 1);
            }
#endif
            ENDHLSL
        }
    }
    Fallback Off
}
