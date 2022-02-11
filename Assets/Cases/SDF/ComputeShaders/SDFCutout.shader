Shader "Unlit/SDFCutout"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _CilpValue ("Clip Value", Range(0.01, 1)) = 0.01
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                half2 texcoord : TEXCOORD0;
                half4 vertex : POSITION;
            };

            struct v2f
            {
                half2 uv : TEXCOORD0;
                half4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float _CilpValue;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 col = tex2D(_MainTex, i.uv).rrrr;
                clip(col.r - _CilpValue);
                col.r = saturate(col.r - _CilpValue) <= 0.01 ? 1 : col.r;
                return half4(col.rrr, 1);
            }
            ENDCG
        }
    }
}
