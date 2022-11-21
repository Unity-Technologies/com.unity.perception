//This shader takes in keypoint positions and minimum depth values as pixels
//in a pair of textures and compares them with the depth image to see if any
//objects are occluding the keypoint
Shader "Perception/KeypointDepthCheck"
{
    SubShader
    {
        Pass
        {
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Name "KeypointDepthCheck"

            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM

            #pragma target 4.5
            #pragma multi_compile_instancing
            #pragma enable_d3d11_debug_symbols

            #pragma vertex Vert
            #pragma fragment Frag

            #include "UnityCG.cginc"

            // The camera's height in pixels.
            int _CameraPixelHeight;

            // The camera's far plane distance.
            float _CameraFarPlane;

            // The 2d position in screen space of each keypoint.
            Texture2D _KeypointPositions;

            // The minimum allowable depth of geometry in the direction ot each keypoint.
            Texture2D _KeypointDepthToCheck;

            // The sensor's depth channel texture.
            Texture2D _LinearDepthTexture;

            // The 3x3 square of depth pixels to check.
            static const float2 screenCoordOffsets[9] =
            {
                float2(-1, -1),
                float2(-1,  0),
                float2(-1,  1),
                float2( 0, -1),
                float2( 0,  0),
                float2( 0,  1),
                float2( 1, -1),
                float2( 1,  0),
                float2( 1,  1)
            };

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

            v2f Vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 LoadSceneDepth(uint2 uv)
            {
                return _LinearDepthTexture.Load(float3(uv, 0));
            }

            float4 Frag(v2f input) : SV_Target
            {
                const float keypointDepthToCheck = _KeypointDepthToCheck.Load(float3(input.vertex.xy, 0)).r;

                float2 keypointScreenCoord = _KeypointPositions.Load(float3(input.vertex.xy, 0)).xy;
                keypointScreenCoord = float2(keypointScreenCoord.x, _CameraPixelHeight - keypointScreenCoord.y);

                float depth = 0.0f;
                for (int i = 0; i < 9; i++)
                {
                    const float2 offsetScreenCoord = keypointScreenCoord + screenCoordOffsets[i];
                    const float4 pixelDepth = LoadSceneDepth(offsetScreenCoord);
                    if (pixelDepth.a > 0.0f)
                    {
                        depth = pixelDepth.r;
                        break;
                    }
                }

                uint result = depth >= keypointDepthToCheck ? 1 : 0;
                return float4(result, 0, 0, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
