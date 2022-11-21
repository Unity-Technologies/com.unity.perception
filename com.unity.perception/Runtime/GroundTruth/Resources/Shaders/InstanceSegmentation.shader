Shader "Perception/InstanceSegmentation"
{
    Properties
    {
        [MainColor] _MainColor("Main Color", Color) = (1,1,1,1)
        [MainTexture] _MainTex("Main Texture", 2D) = "white" {}
        _InstanceSegmentationColor("Segmentation ID", Color) = (0,0,0,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            ZTest LEqual
            Cull Off

            HLSLPROGRAM

            #pragma vertex vertexShader
            #pragma fragment fragmentShader

            #include "UnityCG.cginc"

            float _AlphaThreshold;
            float4 _InstanceSegmentationColor;
            float4 _MainColor;
            float4 _MainTex_ST;
            sampler2D _MainTex;

            struct inVertex
            {
                float4 vertex : POSITION;
                float2 uv: TEXCOORD0;
            };

            struct vertexToFragment
            {
                float4 vertex : SV_POSITION;
                float2 uv: TEXCOORD0;
            };

            vertexToFragment vertexShader(inVertex vertObjectSpace)
            {
                vertexToFragment vertScreenSpace;
                vertScreenSpace.vertex = UnityObjectToClipPos(vertObjectSpace.vertex);
                vertScreenSpace.uv = TRANSFORM_TEX(vertObjectSpace.uv, _MainTex);
                return vertScreenSpace;
            }

            float4 fragmentShader(vertexToFragment vertScreenSpace) : SV_Target
            {
                const float alpha = tex2D(_MainTex, vertScreenSpace.uv).a * _MainColor.a;
                clip(alpha - _AlphaThreshold);
                return _InstanceSegmentationColor;
            }

            ENDHLSL
        }
    }
}
