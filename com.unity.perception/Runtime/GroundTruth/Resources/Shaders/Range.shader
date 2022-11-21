Shader "Perception/Range"
{
    SubShader
    {
        Cull Off
        ZWrite On
        Tags { "LightMode" = "SRPDefaultUnlit" }

        Pass
        {
            HLSLPROGRAM
            #include "UnityCG.cginc"

            #pragma vertex vert
            #pragma fragment frag

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float4 cameraSpacePosition: TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.position = UnityObjectToClipPos(v.vertex);
                o.cameraSpacePosition = float4(UnityObjectToViewPos(v.vertex), 0);
                return o;
            }

            float4 frag(v2f i) : SV_TARGET
            {
                float depth = length(i.cameraSpacePosition);
                return float4(depth, 0, 0, 1);
            }
            ENDHLSL
        }
    }
}
