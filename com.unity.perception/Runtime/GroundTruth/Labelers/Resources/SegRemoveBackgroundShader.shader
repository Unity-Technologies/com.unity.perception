Shader "Perception/SegRemoveBackgroundShader"
{
    Properties
    {
        _BaseMap ("Base (RGB) Trans (A)", 2D) = "white" { }
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

            sampler2D _BaseMap;
            fixed4 _RemoveColor;
            float _SegmentTransparency;
            float _BackTransparency;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                //o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_BaseMap, i.uv);
                
                if (abs(col.r - _RemoveColor.r) < .01f)
                {
                    if (abs(col.g - _RemoveColor.g) < .01f)
                    {
                        if (abs(col.b - _RemoveColor.b) < .01f)
                        {
                            col.a = _BackTransparency;
                        }
                    }
                }
                
                if (abs(col.a - _BackTransparency) > .01f) col.a = _SegmentTransparency;

                return col;
            }
            ENDCG
        }
    }
}
