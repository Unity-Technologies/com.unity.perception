Shader "Perception/KeypointDepthCheck"
{
    Properties
    {
        _Positions("Positions", 2D) = "defaultTexture" {}
        _KeypointDepth("KeypointDepth", 2D) = "defaultTexture" {}
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
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Tags { "LightMode" = "SRP" }

            Blend Off
            ZWrite On
            ZTest LEqual

            Cull Back

            CGPROGRAM

            #pragma vertex depthCheckVertexStage
            #pragma fragment depthCheckFragmentStage

            #include "UnityCG.cginc"

            UNITY_DECLARE_TEX2DARRAY(_DepthTexture);
            //sampler2D _CameraDepthTexture;

            Texture2D _Positions;
            SamplerState my_point_clamp_sampler;
            Texture2D _KeypointDepth;

            struct in_vert
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct vertexToFragment
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            vertexToFragment depthCheckVertexStage (in_vert vertWorldSpace)
            {
                vertexToFragment vertScreenSpace;
                vertScreenSpace.vertex = UnityObjectToClipPos(vertWorldSpace.vertex);
                vertScreenSpace.uv = vertWorldSpace.uv;
                return vertScreenSpace;
            }

            fixed4 depthCheckFragmentStage (vertexToFragment vertScreenSpace) : SV_Target
            {
                float4 checkPosition = _Positions.Sample(my_point_clamp_sampler, vertScreenSpace.uv);
                float4 checkDepth = _KeypointDepth.Sample(my_point_clamp_sampler, vertScreenSpace.uv);
                float depth = UNITY_SAMPLE_TEX2DARRAY(_DepthTexture, float3(checkPosition.xy, 0)).r; //SAMPLE_DEPTH_TEXTURE(_DepthTexture, checkPosition.xy);
                //float depth_decoded = LinearEyeDepth(depth);
                float depth_decoded = Linear01Depth(depth);
                uint result = depth_decoded < checkDepth.x - .001 ? 0 : 1;
                return fixed4(result, result, result, result);
            }

            ENDCG
        }
    }
}
