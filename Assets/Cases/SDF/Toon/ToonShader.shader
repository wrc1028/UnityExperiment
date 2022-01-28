Shader "Unlit/ToonShader"
{
    Properties
    {
        _MainTexa ("Texture 1", 2D) = "black" {}
        _MainTexb ("Texture 2", 2D) = "black" {}
        _MainTexc ("Texture 3", 2D) = "black" {}
        _MainTexd ("Texture 4", 2D) = "black" {}
        _MainTexe ("Texture 5", 2D) = "black" {}
        _MainTexf ("Texture 6", 2D) = "black" {}
        _MainTexg ("Texture 7", 2D) = "black" {}
        _MainTexh ("Texture 8", 2D) = "black" {}
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

            sampler2D _MainTexa;
            sampler2D _MainTexb;
            sampler2D _MainTexc;
            sampler2D _MainTexd;
            sampler2D _MainTexe;
            sampler2D _MainTexf;
            sampler2D _MainTexg;
            sampler2D _MainTexh;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half a = tex2D(_MainTexa, i.uv).r;
                half b = tex2D(_MainTexb, i.uv).r;
                half c = tex2D(_MainTexc, i.uv).r;
                half d = tex2D(_MainTexd, i.uv).r;
                half f = tex2D(_MainTexe, i.uv).r;
                half g = tex2D(_MainTexf, i.uv).r;
                half h = tex2D(_MainTexg, i.uv).r;
                half j = tex2D(_MainTexh, i.uv).r;
                half result = (a + b + c + d + f + g + h + j) / 8;
                return half4(result.xxx, 1);
            }
            ENDCG
        }
    }
}
