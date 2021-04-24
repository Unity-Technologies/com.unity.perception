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
            #pragma enable_d3d11_debug_symbols
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
                PositionInputs positionInputs = GetPositionInput(checkPosition, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
                depth = positionInputs.linearDepth;

                //encode and decode checkDepth to account for loss of precision with depth values close to far plane
                float4 viewPos = mul(UNITY_MATRIX_I_VP, float4(positionInputs.positionWS.x, positionInputs.positionWS.y, checkDepth.x, 1.0));
                float4 positionCheckWS = mul(UNITY_MATRIX_VP, viewPos);

                //depth = LinearEyeDepth(depth, _ZBufferParams);
                // float depth = UNITY_SAMPLE_TEX2DARRAY(_DepthTexture, float3(checkPosition.xy, 0)).r; //SAMPLE_DEPTH_TEXTURE(_DepthTexture, checkPosition.xy);
                //float depth_decoded = LinearEyeDepth(depth);
                // float depth_decoded = Linear01Depth(depth);
                uint result = depth >= positionCheckWS.z ? 1 : 0;
                return float4(result, result, result, 1);
            }
#else

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"

            bool IsPerspectiveProjection()
            {
                return unity_OrthoParams.w == 0;
            }
            float ViewSpaceDepth(float depth)
            {
                if (IsPerspectiveProjection())
                    return LinearEyeDepth(depth, _ZBufferParams);
                else
                    return _ProjectionParams.y + (_ProjectionParams.z - _ProjectionParams.y) * (1 - depth);
            }

            float4 FullScreenPass(Varyings varyings) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);

                float4 checkPosition = _Positions.Load(float3(varyings.positionCS.xy, 0));
                float checkDepth = _KeypointCheckDepth.Load(float3(varyings.positionCS.xy, 0)).r;

                float depth = LoadSceneDepth(float2(checkPosition.x, _ScreenParams.y - checkPosition.y));

                depth = ViewSpaceDepth(depth);

                //encode and decode checkDepth to account for loss of precision with depth values close to far plane
                PositionInputs positionInputs = GetPositionInput(checkPosition, _ScreenParams.zw - float2(1, 1), depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
                float4 viewPos = mul(UNITY_MATRIX_V, float4(positionInputs.positionWS.x, positionInputs.positionWS.y, checkDepth, 1.0));
                float4 positionCheckWS = mul(UNITY_MATRIX_I_V, viewPos);
                float depthCompare = positionCheckWS.z;

                //float depthCompare = checkDepth;

                //depth = LinearEyeDepth(depth, _ZBufferParams);
                // float depth = UNITY_SAMPLE_TEX2DARRAY(_DepthTexture, float3(checkPosition.xy, 0)).r; //SAMPLE_DEPTH_TEXTURE(_DepthTexture, checkPosition.xy);
                //float depth_decoded = LinearEyeDepth(depth);
                // float depth_decoded = Linear01Depth(depth);
                uint result = depth >= depthCompare ? 1 : 0;
                return float4(result, result, result, 1);
            }
#endif
            ENDHLSL
        }
    }
    Fallback Off
}
