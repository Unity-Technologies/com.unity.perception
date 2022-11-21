Shader "Perception/VertexNormals"
{
    SubShader
    {
        Tags { "LightMode" = "SRPDefaultUnlit" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "UnityCG.cginc"

            struct VertexIn
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct VertexToFragment
            {
                float4 vertex: SV_POSITION;
                float3 normal : TEXCOORD1;
            };

            VertexToFragment Vertex(VertexIn vertexIn)
            {
                VertexToFragment vertexOut;
                vertexOut.vertex = UnityObjectToClipPos(vertexIn.vertex);
                vertexOut.normal = mul((float3x3)UNITY_MATRIX_M, vertexIn.normal) / 2 + 0.5;
                return vertexOut;
            }

            float4 Fragment(VertexToFragment vertexOut) : SV_Target
            {
                return float4(vertexOut.normal, 1);
            }
            ENDCG
        }
    }
}
