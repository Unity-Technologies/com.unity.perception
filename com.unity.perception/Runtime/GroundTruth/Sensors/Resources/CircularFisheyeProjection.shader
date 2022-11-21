Shader "Perception/CircularFisheyeProjection"
{
    SubShader
    {
        Pass
        {
            Tags { "LightMode" = "SRPDefaultUnlit" }

            HLSLPROGRAM
            #include "UnityCG.cginc"

            #pragma vertex vertexShader
            #pragma fragment fragmentShader

            bool _RenderCorners;
            float _FieldOfView;
            samplerCUBE _CubemapTex;

            struct inVertex
            {
                float4 vertex : POSITION;
                float2 uv: TEXCOORD0;
            };

            struct vertexToFragment
            {
                float4 vertex : SV_POSITION;
                float2 angleFromCenter: TEXCOORD0;
            };

            vertexToFragment vertexShader(inVertex vertObjectSpace)
            {
                vertexToFragment vertScreenSpace;
                vertScreenSpace.vertex = UnityObjectToClipPos(vertObjectSpace.vertex);
                vertScreenSpace.angleFromCenter = (vertObjectSpace.uv * 2 - 1) * radians(_FieldOfView) / 2;
                vertScreenSpace.angleFromCenter.y *= -1;
                return vertScreenSpace;
            }

            float3 FisheyeCubemapUV(float2 angleFromCenter, float xAxisRotation)
            {
                const float zAxisRotation = atan2(angleFromCenter.x, angleFromCenter.y);
                return float3(
                    sin(zAxisRotation) * sin(xAxisRotation),
                    cos(zAxisRotation) * sin(xAxisRotation),
                    cos(xAxisRotation));
            }

            float4 fragmentShader(vertexToFragment i) : SV_Target
            {
                const float xAxisRot = length(i.angleFromCenter);
                const float3 cubemapUV = FisheyeCubemapUV(i.angleFromCenter, xAxisRot);
                const float4 sampledValue = texCUBE(_CubemapTex, cubemapUV);
                return _RenderCorners && xAxisRot < UNITY_PI || xAxisRot < radians(_FieldOfView) / 2
                    ? sampledValue : float4(0, 0, 0, 1);
            }

            ENDHLSL
        }
    }
}
