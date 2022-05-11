Shader "Reflection/PlanarReflection"
{	
	Properties
	{
		// _ReflectionTex ("Reflection Tex", 2D) = "white" {}
		_EnvMap ("CubeMap", Cube) = "Skybox" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
 
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
 
			struct appdata
			{
				float4 vertex : POSITION;
				float2 baseUV : TEXCOORD0;
			};
 
			struct v2f
			{
				float4 screenPos : TEXCOORD0;
				float2 baseUV : TEXCOORD1;
				float4 vertex : SV_POSITION;
				float3 worldPos : TEXCOORD2;
			};
 
			sampler2D _SSReflectionTexture;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.screenPos = ComputeScreenPos(o.vertex);
				o.baseUV = v.baseUV;
				o.worldPos = mul(unity_ObjectToWorld, o.vertex).xyz;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float2 uv = i.screenPos.xy / i.screenPos.w;
				float4 reflectDst = tex2D(_SSReflectionTexture, uv);
				return reflectDst;
			}
			ENDCG
		}
	}
}