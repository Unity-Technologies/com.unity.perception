//This shader takes in keypoint positions and minimum depth values as pixels
//in a pair of textures and compares them with the depth image to see if any
//objects are occluding the keypoint
Shader "Perception/KeypointDepthCheck"
{
    Properties
    {
        //2d position in screen space of each keypoint
        _Positions("Positions", 2D) = "defaultTexture" {}
        //minimum allowable depth of geometry in the direction ot each keypoint
        _KeypointCheckDepth("KeypointCheckDepth", 2D) = "defaultTexture" {}
        //The rendered depth texture
        _DepthTexture("Depth", 2DArray) = "defaultTexture" {}
    }

    HLSLINCLUDE

    #pragma target 4.5
    //#pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

    //enable GPU instancing support
    #pragma multi_compile_instancing

    ENDHLSL

    SubShader
    {
        Pass
        {
            PackageRequirements {
                "com.unity.render-pipelines.high-definition"
            }
            Tags { "LightMode" = "SRP" }

            Name "KeypointDepthCheck"
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
//            #pragma only_renderers d3d11 vulkan metal
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag


            static const float2 checkOffsets[9] = {
                float2( 0,  0),
                float2(-1, -1),
                float2( 0, -1),
                float2( 1, -1),
                float2(-1,  0),
                float2( 1,  0),
                float2(-1,  1),
                float2( 0,  1),
                float2( 1,  1)};

            Texture2D _Positions;
            Texture2D _KeypointCheckDepth;

            #pragma enable_d3d11_debug_symbols

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"

            float4 Frag(Varyings varyings) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);

                float checkDepth = _KeypointCheckDepth.Load(float3(varyings.positionCS.xy, 0)).r;

                float2 checkPosition = _Positions.Load(float3(varyings.positionCS.xy, 0)).xy;
                checkPosition = float2(checkPosition.x, _ScreenSize.y - checkPosition.y);

                float2 checkPositionResolved;
                float depth;
                for (int i = 0; i < 9; i++)
                {
                    checkPositionResolved = checkPosition + checkOffsets[i];
                    depth = LoadCameraDepth(checkPositionResolved);
                    if (depth > 0)
                        break;
                }

                PositionInputs positionInputs = GetPositionInput(checkPosition, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
                depth = positionInputs.linearDepth;

                uint result = depth >= checkDepth ? 1 : 0;
                return float4(result, result, result, 1);
            }
            ENDHLSL
        }
        Pass
        {
            PackageRequirements {
                "com.unity.render-pipelines.universal"
            }
            Tags { "LightMode" = "SRP" }

            Name "KeypointDepthCheck"
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
            //#pragma only_renderers d3d11 vulkan metal
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag


            static const float2 checkOffsets[9] = {
                float2( 0,  0),
                float2(-1, -1),
                float2( 0, -1),
                float2( 1, -1),
                float2(-1,  0),
                float2( 1,  0),
                float2(-1,  1),
                float2( 0,  1),
                float2( 1,  1)};

            Texture2D _Positions;
            Texture2D _KeypointCheckDepth;
            #include "UnityCG.cginc"

            //copied from UnityInput.hlsl
            float4x4 _InvViewProjMatrix;
            float4x4 _InvProjMatrix;

            //#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

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

            v2f Vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            bool IsPerspectiveProjection()
            {
                return unity_OrthoParams.w == 0;
            }
            float ViewSpaceDepth(float depth)
            {
                if (IsPerspectiveProjection())
                    return LinearEyeDepth(depth);
                else
                    return _ProjectionParams.y + (_ProjectionParams.z - _ProjectionParams.y) * (1 - depth);
            }

            float EncodeAndDecodeDepth(float vsDepth)
            {
                if (IsPerspectiveProjection())
                {
                    //derived from vsDepth = 1.0 / (_ZBufferParams.z * dtDepth + _ZBufferParams.w);
                    float dtDepth = (1.0 / vsDepth - _ZBufferParams.w) / _ZBufferParams.z;
                    dtDepth = dtDepth;
                    return LinearEyeDepth(dtDepth);
                }
                else //in orthographic projections depth is linear so there is no loss of precision.
                    return vsDepth;
            }

            Texture2D _CameraDepthTexture;

            float LoadSceneDepth(uint2 uv)
            {
                return _CameraDepthTexture.Load(float3(uv, 0)).r;
            }

            float4 ComputeClipSpacePosition(float2 positionNDC, float deviceDepth)
            {
                float4 positionCS = float4(positionNDC * 2.0 - 1.0, deviceDepth, 1.0);

            #if UNITY_UV_STARTS_AT_TOP
                // Our world space, view space, screen space and NDC space are Y-up.
                // Our clip space is flipped upside-down due to poor legacy Unity design.
                // The flip is baked into the projection matrix, so we only have to flip
                // manually when going from CS to NDC and back.
                positionCS.y = -positionCS.y;
            #endif

                return positionCS;
            }

            float3 ComputeWorldSpacePosition(float2 positionNDC, float deviceDepth, float4x4 invViewProjMatrix)
            {
                float4 positionCS  = ComputeClipSpacePosition(positionNDC, deviceDepth);
                float4 hpositionWS = mul(invViewProjMatrix, positionCS);
                return hpositionWS.xyz / hpositionWS.w;
            }
            fixed4 Frag (v2f i) : SV_Target
            {
                float depthVSToCheck = _KeypointCheckDepth.Load(float3(i.vertex.xy, 0)).r;

                float2 checkPosition = _Positions.Load(float3(i.vertex.xy, 0)).xy;
                checkPosition = float2(checkPosition.x, _ScreenParams.y - checkPosition.y);

                float2 checkPositionResolved;
                float depth;
                for (int i = 0; i < 9; i++)
                {
                    checkPositionResolved = checkPosition + checkOffsets[i];
                    depth = LoadSceneDepth(checkPositionResolved);
                    if (depth > 0)
                        break;
                }

                float depthVSActual = ViewSpaceDepth(depth);

                uint result = depthVSActual >= depthVSToCheck  ? 1 : 0;
                return float4(result, result, result, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
