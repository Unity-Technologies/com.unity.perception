Shader "Perception/PixelPosition"
{
    SubShader
    {
        Pass
        {
            Tags { "LightMode" = "SRPDefaultUnlit" }
            ZWrite On
            Cull Off

            CGPROGRAM
            #pragma vertex vertex_shader
            #pragma fragment fragment_shader

            #include "UnityCG.cginc"

            struct in_vertex
            {
                float4 vertex : POSITION;
            };

            struct vertex_to_fragment
            {
                float4 vertex : SV_POSITION;
                float3 camera_space_position: TEXCOORD2;
            };

            vertex_to_fragment vertex_shader(const in_vertex obj_space)
            {
                vertex_to_fragment v2f;
                v2f.camera_space_position = UnityObjectToViewPos(obj_space.vertex);
                // TODO: Probably can make below faster by starting off with the camera space position
                v2f.vertex = UnityObjectToClipPos(obj_space.vertex);
                return v2f;
            }

            float4 fragment_shader(const vertex_to_fragment v2f) : SV_Target
            {
                return float4(v2f.camera_space_position.xy, -v2f.camera_space_position.z, 1.0f);
            }
            ENDCG
        }
    }
}
