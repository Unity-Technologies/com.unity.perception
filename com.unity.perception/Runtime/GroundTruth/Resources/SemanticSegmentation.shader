Shader "Perception/SemanticSegmentation"
{
    Properties
    {
        [PerObjectData] LabelingId("Labeling Id", int) = 0
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

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

            uint LabelingId;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            uint _SegmentationId;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return float4(UnpackUIntToFloat((uint)LabelingId, 0, 8), UnpackUIntToFloat((uint)LabelingId, 8, 8), 0, 1.0);
            }

            ENDCG
        }
    }
}
