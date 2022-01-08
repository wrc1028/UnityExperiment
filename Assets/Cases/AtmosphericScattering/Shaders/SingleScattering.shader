Shader "Unlit/SingleScattering"
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
            #define PI 3.14159

            #include "UnityCG.cginc"

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
            
            int _ScatteringStepNums;
            int _OpticalDepthStepNums;
            float3 _ScatteringValue;

            float3 _EarthCenter;
            float _EarthRadius;
            float _AtmosphereHeight;
            float _AtmosphericDensityCtrl;
            
            float4x4 _CameraInverseProjection;
            
            float2 RaySphereDst(float3 center, float radius, float3 rayOrigin, float3 rayDir)
            {
                float3 rayOriginToCenter = center - rayOrigin;
                float rayBottom = dot(rayOriginToCenter, rayDir);
                float centerToRay2 = dot(rayOriginToCenter, rayOriginToCenter) - rayBottom * rayBottom;
                float rBottom2 = radius * radius - centerToRay2;
                if (rBottom2 >= 0)
                {
                    float rBottom = sqrt(rBottom2);
                    float dstToSphere = max(0, rayBottom - rBottom);
                    float dstInsideSphere = dstToSphere == 0 ? rayBottom + rBottom : rBottom * 2;
                    return float2(dstToSphere, dstInsideSphere);
                }
                return float2(1.#INF, 0);
            }
            // 获得当前采样点的大气密度
            float GetAtmosphericDensity(float3 samplerPoint)
            {
                float altitude = max(0, (distance(samplerPoint, _EarthCenter) - _EarthRadius) / _AtmosphereHeight);
                float density = exp(-altitude * _AtmosphericDensityCtrl) * (1 - altitude);
                return density;
            }
            // 采样光学深度
            float OpticalDepth(float3 samplerPoint, fixed3 rayDir, float samplerLength)
            {
                float totalDensity = 0;
                float stepLength = samplerLength / (_OpticalDepthStepNums -1);
                float3 densitySamplerPoint = samplerPoint;
                for (int step = 0; step < _OpticalDepthStepNums; step++)
                {
                    totalDensity += GetAtmosphericDensity(densitySamplerPoint) * stepLength;
                    densitySamplerPoint += rayDir * stepLength;
                }
                return totalDensity;
            }
            float3 CalculateScattering(float3 originSamplerPoint, fixed3 rayDir, float dstInsideSphere, fixed3 lightDir, float3 originColor)
            {
                float3 scatteringPoint = originSamplerPoint;
                float stepLength = dstInsideSphere / (_ScatteringStepNums - 1);
                float3 scatteringResult = 0;
                float viewDirOpticalDepth = 0;
                // 终点在大气最外层, 第一次衰减自然为0;
                for (int step = 0; step < _ScatteringStepNums; step ++)
                {
                    // 从散射点-光源这段距离上的大气密度计算
                    float pointToEdgeLength = RaySphereDst(_EarthCenter, _EarthRadius + _AtmosphereHeight, scatteringPoint, lightDir).y;
                    float pointToSumOpticalDepth = OpticalDepth(scatteringPoint, lightDir, pointToEdgeLength);
                    viewDirOpticalDepth = OpticalDepth(scatteringPoint, -rayDir, step * stepLength);
                    float3 transmittance = exp(-(pointToSumOpticalDepth + viewDirOpticalDepth) * _ScatteringValue);
                    float scatteringPointDentity = GetAtmosphericDensity(scatteringPoint);
                    
                    scatteringResult += scatteringPointDentity * transmittance * _ScatteringValue * stepLength;
                    scatteringPoint += rayDir * stepLength;
                }
                float colorResult = exp(max(0, -viewDirOpticalDepth));
                return originColor * colorResult + scatteringResult;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.rayOrigin = mul(unity_CameraToWorld, float4(0, 0, 0, 1)).xyz;
                float3 nearClipPlane = mul(_CameraInverseProjection, fixed4(1 - o.uv * 2, 0, 1)).xyz;
                o.viewDir = normalize(mul(unity_CameraToWorld, float4(nearClipPlane, 0)).xyz);
                o.projPos = ComputeScreenPos(o.pos);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 backgroundCol = tex2D(_MainTex, i.uv);

                float3 rayOrigin = i.rayOrigin;
                fixed3 rayDir = -normalize(i.viewDir);
                fixed3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                // 视角太偏深度不起作用的情况
                float linearDepth = LinearEyeDepth(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));

                float2 rayHitSphere = RaySphereDst(_EarthCenter, _EarthRadius + _AtmosphereHeight, rayOrigin, rayDir);
                float dstToSphere = rayHitSphere.x;
                float dstInsideSphere = min(linearDepth - dstToSphere, rayHitSphere.y);

                if (dstInsideSphere > 0)
                {
                    float3 rayEntrySphere = rayOrigin + rayDir * dstToSphere;
                    float3 light = CalculateScattering(rayEntrySphere, rayDir, dstInsideSphere, lightDir, backgroundCol);
                    return float4(light, 0);
                }
                
                
                return backgroundCol;
            }
            ENDCG
        }
    }
}
