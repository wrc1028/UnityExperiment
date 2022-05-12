Shader "Reflection/PlanarReflection"
{	
	Properties
	{
		// _ReflectionTex ("Reflection Tex", 2D) = "white" {}
		_SkyBoxCubeMap ("CubeMap", Cube) = "Skybox" {}
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
				float3 normal : NORMAL;
			};
 
			struct v2f
			{
				float4 screenPos : TEXCOORD0;
				float2 baseUV : TEXCOORD1;
				float4 vertex : SV_POSITION;
				float3 worldPos : TEXCOORD2;
				float3 worldNormal : TEXCOORD3;
			};
 
			sampler2D _SSReflectionTexture;
			samplerCUBE _SkyBoxCubeMap;
			float4 _SkyBoxCubeMap_HDR;

			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.screenPos = ComputeScreenPos(o.vertex);
				o.baseUV = v.baseUV;
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.worldNormal = UnityObjectToWorldDir(v.normal);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float3 worldPos = i.worldPos;
				float3 worldNormal = normalize(i.worldNormal);
				float3 viewDir = UnityWorldSpaceViewDir(worldPos);
				float3 viewReflectDir = reflect(-viewDir, worldNormal);

				half4 rgbm = texCUBElod(_SkyBoxCubeMap, half4(viewReflectDir, 0.0));
				half3 reflEnvCol = DecodeHDR(rgbm, _SkyBoxCubeMap_HDR);

				float2 uv = i.screenPos.xy / i.screenPos.w;
				float4 reflectDst = tex2D(_SSReflectionTexture, uv);

				return float4(lerp(reflEnvCol, reflectDst.rgb, reflectDst.a), 1);
			}
			ENDCG
		}
	}
}