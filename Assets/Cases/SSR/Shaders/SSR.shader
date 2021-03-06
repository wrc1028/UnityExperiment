Shader "Unlit/SSR"
{
    Properties
    {
        _Volume ("Volume SDF", 3D) = "White" {}
        _WaterSDF ("Water SDF", 2D) = "White" {}
        _SkyboxCube ("Skybox", Cube) = "Skybox" {}
        _WaterInfo ("Water Info", vector) = (0, 0, 0, 0)
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
				float3 worldPos : TEXCOORD3;
				float3 worldNormal : TEXCOORD4;
			};

            sampler2D _CameraDepthTexture;
            sampler2D _CustomColorTexture;

            sampler3D _Volume;
            sampler2D _WaterSDF;
            samplerCUBE _SkyboxCube;
			float4 _SkyboxCube_HDR;
            
            float4 _WaterInfo;
            float3 _WaterPos;
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
            float SampleVolumeSDF(float3 positionWS)
            {
                float3 relativePos = positionWS - _WaterPos;
                float3 uvw = (relativePos + _WaterInfo.xyz / 2) / _WaterInfo.xyz;
                float3 reUVW = float3(uvw.z, 1 - uvw.x, 1 - uvw.y);
                return tex3D(_Volume, reUVW).r;
            }
            float GetMarchDst(float3 positionWS)
            {
                float marchDst = SampleVolumeSDF(positionWS) * _WaterInfo.x;
                return marchDst;
            }

            float GetMarchDst(float3 positionWS, half cosAlpha)
            {
                float2 relativeXZPos = positionWS.xz - _WaterPos.xz;
                float2 uv = (relativeXZPos + _WaterInfo.xy / 2) / _WaterInfo.xy;
#ifdef UNITY_UV_STARTS_AT_TOP
                uv.y = 1 - uv.y;
#endif
                float marchDst = tex2D(_WaterSDF, uv).r * _WaterInfo.x / cosAlpha * _WaterInfo.w;
                return marchDst > _StepSize * 10 ? marchDst : _StepSize; // tex2D(_WaterSDF, uv).r * _WaterInfo.z; 
            }

            bool GetMarchPointUVAndDepth(float3 rayDestinationWS, out float3 UVAndDepth)
            {
                float4 rayDestinationCS = UnityWorldToClipPos(rayDestinationWS);
                bool isOutOfUV01 = abs(rayDestinationCS.x) > rayDestinationCS.w || abs(rayDestinationCS.y) > rayDestinationCS.w || rayDestinationCS.z > rayDestinationCS.w;
                rayDestinationCS /= rayDestinationCS.w;
#ifdef UNITY_UV_STARTS_AT_TOP
                rayDestinationCS.y *= -1;
#endif
                UVAndDepth = float3(rayDestinationCS.xy * 0.5 + 0.5, LinearEyeDepth(rayDestinationCS.z));
                return isOutOfUV01;
            }
            // ?????????????????????????????????
            half3 GetSSRColor(float3 positionWS, half3 viewReflectDirWS, half3 normalWS, half3 specColor)
            {
                // ???Y??????????????????
                float3 rayDestinationWS = positionWS;
                float3 currentUVAndDepth = 0;
                float3 prevUVAndDepth = 0;
                float sampleLinearDepth = 0;
                half NdotV = dot(normalWS, viewReflectDirWS);
                UNITY_LOOP
                for (int index = 0; index < 128; index ++)
                {
                    rayDestinationWS = positionWS + viewReflectDirWS * index * (_StepSize / NdotV);
                    // ???????????????UV
                    if (GetMarchPointUVAndDepth(rayDestinationWS, currentUVAndDepth)) return specColor;
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
            half3 TestVolumeSDF(float3 positionWS, half3 viewReflectDirWS, half3 specColor)
            {
                float3 rayDestinationWS = positionWS;
                float rayDst = 0;
                UNITY_LOOP
                for (int index = 0; index < 16; index ++)
                {
                    rayDst += GetMarchDst(rayDestinationWS);
                    rayDestinationWS = positionWS + viewReflectDirWS * rayDst;
                    if (SampleVolumeSDF(rayDestinationWS) < 0.01) return 0;
                }
                return specColor;
            }

            half4 frag (v2f i) : SV_Target
            {
                float3 worldPos = i.worldPos;
				float3 worldNormal = normalize(i.worldNormal);
				float3 viewDir = UnityWorldSpaceViewDir(worldPos);
				float3 viewReflectDir = reflect(-viewDir, worldNormal);

                half4 skyboxBase = texCUBElod(_SkyboxCube, half4(viewReflectDir, 0.0));
				half3 IBLSpecColor = DecodeHDR(skyboxBase, _SkyboxCube_HDR);

                half3 ssrColor = GetSSRColor(worldPos, viewReflectDir, worldNormal, IBLSpecColor);
                return half4(ssrColor, 1);
                // return half4(GetMarchDst(worldPos).xxx, 1);
            }
            ENDCG
        }
    }
}
