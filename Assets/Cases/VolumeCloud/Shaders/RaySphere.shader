Shader "Unlit/RaySphere"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            struct appdata
            {
                half2 texcoord : TEXCOORD0;
                half4 vertex : POSITION;
            };

            struct v2f
            {
                half2 uv : TEXCOORD0;
                half4 pos : SV_POSITION;
                float3 rayOrigin : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
                float4 projPos : TEXCOORD3;
            };

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;
            sampler2D _HeightGradientTexture;

            float3 _RaySphereCenter;
            float _SphereRadius;
            float _SphereHeight;
            int _RadiusStepNums;

            Texture3D<float4> _ShapeTexture;
            SamplerState sampler_ShapeTexture;
            float _CloudScale;
            float _DensityThreshold;
            float _DensityMultiplier;


            float4x4 _CameraInverseProjection;
            // TODO: 优化
            float4 RaySphere(float3 center, float radius, float height, float3 rayOrigin, float3 rayDir)
            {
                float dstToCenter = distance(rayOrigin, center);
                fixed3 rayOriginToCenterDir = normalize(center - rayOrigin);
                half cosAngle = dot(rayDir, rayOriginToCenterDir);
                float dstToRay = dstToCenter * sqrt(1 - cosAngle * cosAngle);
                // 半径内部
                if (dstToCenter <= radius)
                {
                    float bottom_r = sqrt(radius * radius - dstToRay * dstToRay);
                    float bottom_R = sqrt((radius + height) * (radius + height) - dstToRay * dstToRay);
                    return float4(dstToCenter * cosAngle + bottom_r, bottom_R - bottom_r, 0, 0);
                }
                // 云层外
                else if (dstToCenter > (radius + height))
                {
                    if (cosAngle < 0 || dstToRay > (radius + height)) return 0;
                    else
                    {
                        float bottom_r = sqrt(max(0, radius * radius - dstToRay * dstToRay));
                        float bottom_R = sqrt(max(0, (radius + height) * (radius + height) - dstToRay * dstToRay));
                        return bottom_r == 0 ? float4(dstToCenter * cosAngle - bottom_R, bottom_R * 2, 0, 0) : 
                            float4(dstToCenter * cosAngle - bottom_R, bottom_R - bottom_r, dstToCenter * cosAngle + bottom_r, bottom_R - bottom_r);
                    }
                }
                // 云层里
                else
                {
                    float bottom_r = sqrt(max(0, radius * radius - dstToRay * dstToRay));
                    float bottom_R = sqrt((radius + height) * (radius + height) - dstToRay * dstToRay);
                    return (bottom_r == 0 || cosAngle < 0) ? float4(0, dstToCenter * cosAngle + bottom_R, 0, 0) : 
                        float4(0, dstToCenter * cosAngle - bottom_r, dstToCenter * cosAngle + bottom_r, bottom_R - bottom_r);
                }
            }

            float SamplerDensity(float3 samplerPosition)
            {
                float4 heightUV = float4(0.5, (distance(samplerPosition, _RaySphereCenter) - _SphereRadius) / _SphereHeight, 0, 0);
                float height = tex2Dlod(_HeightGradientTexture, heightUV).b;
                float3 uvw = samplerPosition * _CloudScale * 0.001;
                float4 shape = _ShapeTexture.SampleLevel(sampler_ShapeTexture, uvw, 0) * height;
                return max(0, (shape.r + _DensityThreshold * 0.1) * _DensityMultiplier * 0.1);
            }

            float DensityMarch(float3 rayOrigin, float3 rayDir, float dstToSphere, float dstInsideSphere, float linearDepth)
            {
                float marchDst = 0;
                float totalDensity = 0;
                float limitDst = min(linearDepth - dstToSphere, dstInsideSphere);
                float stepLength = dstInsideSphere / _RadiusStepNums;
                float3 entryPosition = rayOrigin + rayDir * dstToSphere;
                float3 samplerPosition = 0;
                while(marchDst < limitDst)
                {
                    samplerPosition = entryPosition + rayDir * marchDst;
                    marchDst += stepLength;
                    totalDensity += SamplerDensity(samplerPosition) * stepLength;
                }
                return totalDensity;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.projPos = ComputeScreenPos(o.pos);
                o.uv = v.texcoord;
                o.rayOrigin = mul(unity_CameraToWorld, float4(0, 0, 0, 1)).xyz;
                float3 nearClipPlane = mul(_CameraInverseProjection, fixed4(1 - o.uv * 2, 0, 1)).xyz;
                o.viewDir = normalize(mul(unity_CameraToWorld, float4(nearClipPlane, 0)).xyz);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 col = tex2D(_MainTex, i.uv);
                // 深度值需要重新计算
                float linearDepth = LinearEyeDepth(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
                float3 rayOrigin = i.rayOrigin;
                fixed3 rayDir = -normalize(i.viewDir);
                float4 raySphereResult = RaySphere(_RaySphereCenter, _SphereRadius, _SphereHeight, rayOrigin, rayDir);
                
                float density = 0;
                if (raySphereResult.y > 0) density += DensityMarch(rayOrigin, rayDir, raySphereResult.x, raySphereResult.y, linearDepth);
                if (raySphereResult.w > 0) density += DensityMarch(rayOrigin, rayDir, raySphereResult.z, raySphereResult.w, linearDepth);
                
                return col * exp(-density);
            }
            ENDCG
        }
    }
}
