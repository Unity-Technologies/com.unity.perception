Shader "Perception/InstanceSegmentation"
{
    Properties
    {
        [PerObjectData] _SegmentationId("Segmentation ID", int) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Tags { "LightMode" = "SRP" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

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
                return float4(UnpackUIntToFloat((uint)_SegmentationId, 0, 8), UnpackUIntToFloat(_SegmentationId, 8, 8), UnpackUIntToFloat(_SegmentationId, 16, 8), UnpackUIntToFloat(_SegmentationId, 24, 8));
            }
            ENDCG
        }
    }
}
