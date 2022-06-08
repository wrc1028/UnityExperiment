Shader "Unlit/SSR"
{
    Properties
    {
        _SkyboxCube ("Skybox", Cube) = "Skybox" {}
        _StepSize ("March Step Size", float) = 5
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent"}

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

            sampler2D _CameraDepthTexture;
            sampler2D _CustomColorTexture;

            samplerCUBE _SkyboxCube;
			float4 _SkyboxCube_HDR;

            float _StepSize;

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

            bool GetMarchPointUVAndDepth(float3 rayOrigin, half3 rayDir, float rayDst, out float3 UVAndDepth)
            {
                float3 rayDestinationWS = rayOrigin + rayDir * rayDst;
                float4 rayDestinationCS = UnityWorldToClipPos(rayDestinationWS);
                bool isOutOfUV01 = abs(rayDestinationCS.x) > rayDestinationCS.w || abs(rayDestinationCS.y) > rayDestinationCS.w || rayDestinationCS.z > rayDestinationCS.w;
                rayDestinationCS /= rayDestinationCS.w;
#ifdef UNITY_UV_STARTS_AT_TOP
                rayDestinationCS.y *= -1;
#endif
                UVAndDepth = float3(rayDestinationCS.xy * 0.5 + 0.5, LinearEyeDepth(rayDestinationCS.z));
                return isOutOfUV01;
            }

            half3 GetSSRColor(float3 positionWS, half3 viewReflectDirWS, half3 specColor)
            {
                // 每一步的步长
                half smallStepSize = _StepSize / 10;
                float3 currentUVAndDepth = 0;
                float3 prevUVAndDepth = 0;
                float sampleLinearDepth = 0;
                UNITY_LOOP
                for (int index = 0; index < 32; index ++)
                {
                    if (GetMarchPointUVAndDepth(positionWS, viewReflectDirWS, (index + 1) * _StepSize, currentUVAndDepth)) break;
                    sampleLinearDepth = LinearEyeDepth(tex2D(_CameraDepthTexture, currentUVAndDepth.xy).r);
                    if (sampleLinearDepth < currentUVAndDepth.z) break;
                    prevUVAndDepth = currentUVAndDepth;
                }
                if (prevUVAndDepth.z == currentUVAndDepth.z) return specColor;
                else
                {
                    half lerpValue = (sampleLinearDepth - prevUVAndDepth.z) / (currentUVAndDepth.z - prevUVAndDepth.z);
                    half2 uv = lerp(prevUVAndDepth.xy, currentUVAndDepth.xy, lerpValue);
                    return tex2D(_CustomColorTexture, uv).rgb;
                }
            }

            half4 frag (v2f i) : SV_Target
            {
                float3 worldPos = i.worldPos;
				float3 worldNormal = normalize(i.worldNormal);
				float3 viewDir = UnityWorldSpaceViewDir(worldPos);
				float3 viewReflectDir = reflect(-viewDir, worldNormal);

                half4 skyboxBase = texCUBElod(_SkyboxCube, half4(viewReflectDir, 0.0));
				half3 IBLSpecColor = DecodeHDR(skyboxBase, _SkyboxCube_HDR);

                half3 ssrColor = GetSSRColor(worldPos, viewReflectDir, IBLSpecColor);
                return half4(ssrColor, 1);
            }
            ENDCG
        }
    }
}
