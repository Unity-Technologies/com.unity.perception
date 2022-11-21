Shader "Perception/Depth"
{
    SubShader
    {
        Pass
        {
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Off
            ZWrite On

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

                // Invert the z component of the vertex in camera space.
                float4 cameraSpacePosition = float4(UnityObjectToViewPos(v.vertex), 1);
                cameraSpacePosition.z = -cameraSpacePosition.z;
                o.cameraSpacePosition = cameraSpacePosition;

                return o;
            }

            float4 frag(v2f i) : SV_TARGET
            {
                return float4(i.cameraSpacePosition.z, 0, 0, 1);
            }
            ENDHLSL
        }
    }
}
