Shader "Perception/SegRemoveBackgroundShader"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "defaulttexture" { }
        _RemoveColor ("Remove Color", Color) = (0, 0, 0, 1)
        _SegmentTransparency("Segment Transparency", Range(0.0,1.0)) = 0.5
        _BackTransparency("Background Transparency", Range(0.0,1.0)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        ZWrite off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag Lambert alpha

            #include "UnityCG.cginc"

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

            sampler2D _MainTex;
            fixed4 _RemoveColor;
            float _SegmentTransparency;
            float _BackTransparency;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);

                if (abs(col.r - _RemoveColor.r) < .001f && abs(col.g - _RemoveColor.g) < .001f && abs(col.b - _RemoveColor.b) < .001f)
                    col.a = _BackTransparency;
                else
                    col.a = _SegmentTransparency;

                return col;
            }
            ENDCG
        }
    }
}
