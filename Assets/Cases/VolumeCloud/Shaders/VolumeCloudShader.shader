Shader "Custom/VolumeCloudShader"
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
                half4 pos : SV_POSITION;
                half2 uv : TEXCOORD0;
                float3 rayOrigin : TEXCOORD1;
                fixed3 viewDir : TEXCOORD2;
                float4 projPos : TEXCOORD3;
            };
            
            float4x4 _CameraInverseProjection;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.projPos = ComputeScreenPos(o.pos);
                o.uv = v.texcoord;
                // 这部分应该能优化
                o.rayOrigin = mul(unity_CameraToWorld, float4(0, 0, 0, 1)).xyz;
                float3 nearClipPlane = mul(_CameraInverseProjection, fixed4(1 - o.uv * 2, 0, 1)).xyz;
                o.viewDir = normalize(mul(unity_CameraToWorld, float4(nearClipPlane, 0)).xyz);
                return o;
            }

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;

            float3 BoundMin;
            float3 BoundMax;
            int StepNums;
            int LightStepNums;

            sampler2D HeightGradientTexture;
            sampler2D CurlNoiseTexture;
            
            Texture3D<float4> ShapeNoise;
            SamplerState samplerShapeNoise;
            float4 ShapeNoiseWeight;
            float CloudScale;
            float4 CloudOffset;
            float4 CloudMoveDirAndSpeed;
            float DensityThreshold;
            float DensityMultiplier;

            Texture3D<float4> DetailNoise;
            SamplerState samplerDetailNoise;
            float DetailWeight;
            float4 DetailNoiseWeight;
            float DetailNoiseScale;
            float4 DetailNoiseOffset;
            float4 DetailMoveDirAndSpeed;

            float LightAbsorptionTowardSun;
            float LightAbsorptionTowardCloud;
            float4 DarknessThreshold;

            // 包围盒算法
            float2 RayBoxDst(float3 boundMin, float3 boundMax, float3 rayOrigin, float3 rayDir)
            {
                float3 t0 = (boundMin - rayOrigin) / rayDir;
                float3 t1 = (boundMax - rayOrigin) / rayDir;
                float3 tMin = min(t0, t1);
                float3 tMax = max(t0, t1);

                float dstA = max(max(tMin.x, tMin.y), tMin.z);
                float dstB = min(tMax.x, min(tMax.y, tMax.z));

                float dstToBox = max(0, dstA);
                float dstInsideBox = max(0, dstB - dstToBox);
                return float2(dstToBox, dstInsideBox);
            }
            
            float HGFunc(float cos_angle, float g)
            {
                float g2 = g * g;
                return (1 - g2) / (pow(1.0 + g2 - 2.0 * g * cos_angle, 1.5) * 4 * 3.14159);
            }
            float Phase(float a)
            {
                float blend = 0.5;
                float hgBlend = HGFunc(a, DarknessThreshold.x) * (1 -blend) + HGFunc(a, -DarknessThreshold.y) * blend;
                return DarknessThreshold.z + hgBlend * DarknessThreshold.w;
            }
            float Remap(half x, half t1, half t2, half s1, half s2)
            {
                return (x - t1) / (t2 - t1) * (s2 - s1) + s1;
            }
            float SampleDensity(float3 samplerPosition)
            {
                // 取样高度
                float4 heightUV = float4(0.5, (samplerPosition.y - BoundMin.y) / (BoundMax.y - BoundMin.y), 0, 0);
                float height = tex2Dlod(HeightGradientTexture, heightUV).b;
                float3 uvw = samplerPosition * CloudScale * 0.001 + CloudOffset.xyz * CloudOffset.w + _Time.y * CloudMoveDirAndSpeed.xyz * CloudMoveDirAndSpeed.w;
                float4 shape = ShapeNoise.SampleLevel(samplerShapeNoise, uvw, 0);
                float4 normalizeShapeWeight = ShapeNoiseWeight / dot(ShapeNoiseWeight, 1);
                // Remap(shape.r, (shape.g * 0.625 + shape.b * 0.25 + shape.a * 0.125) - 1, 1, 0, 1); //
                float shapeFBM = dot(shape, normalizeShapeWeight);
                float shapeDensity = (shapeFBM + DensityThreshold * 0.1) * height;
                if (shapeDensity < 0) return 0;
                float3 detailUVW = uvw * DetailNoiseScale + DetailNoiseOffset.xyz * DetailNoiseOffset.w + _Time.y * DetailMoveDirAndSpeed.xyz * DetailMoveDirAndSpeed.w;
                float4 detail = DetailNoise.SampleLevel(samplerDetailNoise, detailUVW, 0);
                float4 normalizeDetailWeight = DetailNoiseWeight / dot(DetailNoiseWeight, 1);
                float detailFBM = dot(detail, normalizeDetailWeight);
                // 形成一个3D的mask, 在形状的边缘处进行细节处理
                float detailErodeWeight = (1 - shapeFBM) * (1 - shapeFBM) * (1 - shapeFBM);
                // 1 - detailFBM 形成花椰菜的形状
                float detailErodeDensity = (1 - detailFBM) * detailErodeWeight * DetailWeight;
                return max(0, (shapeDensity - detailErodeDensity) * DensityMultiplier * 0.1);
            }

            float LightMarch(float3 samplerPosition)
            {
                fixed3 lightDir = normalize(UnityWorldSpaceLightDir(samplerPosition));
                float dstInsideBox = RayBoxDst(BoundMin, BoundMax, samplerPosition, lightDir).y;
                float lightStepSize = dstInsideBox / LightStepNums;
                float totalDensity = 0;
                for (int step = 0; step < LightStepNums; step++)
                {
                    samplerPosition += lightDir * lightStepSize;
                    totalDensity += max(0, SampleDensity(samplerPosition)) * lightStepSize;
                }
                // 采样点到光线距离上的采样越大，透光率越低
                float transmittance = exp(-totalDensity * LightAbsorptionTowardSun);
                return transmittance;
            }

            half4 frag (v2f i) : SV_Target
            {
                // 获得线性深度
                float linearDepth = LinearEyeDepth(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
                // 初始化值
                float3 rayOrigin = i.rayOrigin;
                fixed3 rayDir = -normalize(i.viewDir);
                fixed3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float2 rayBoxDst = RayBoxDst(BoundMin, BoundMax, rayOrigin, rayDir);
                float dstToBox = rayBoxDst.x;
                float dstInsideBox = rayBoxDst.y;
                // 采样密度
                float marchDst = 0;     // 步进总长
                float stepSize = dstInsideBox / StepNums; // 每一步的距离
                float limitDst = min(linearDepth - dstToBox, dstInsideBox);  // 区分物体与体积云的前后关系
                float3 samplerPosition = 0; // 采样位置
                float3 entryPoint = rayOrigin + rayDir * dstToBox;  // 进入盒子的起点

                float transmittance = 1;
                float lightEnergy = 0;

                float VdotL = dot(-rayDir, lightDir);

                while (marchDst < limitDst)
                {
                    samplerPosition = entryPoint + rayDir * marchDst;
                    // 采样次数固定，所以采样间距不一样，将结果与采样间距相乘用于保存不同角度下采样结果的相对平衡
                    float density = SampleDensity(samplerPosition) * stepSize;
                    if (density > 0)
                    {
                        float lightTransmittance = LightMarch(samplerPosition);
                        lightEnergy += density * transmittance * lightTransmittance * Phase(VdotL);
                        transmittance *= exp(-density * LightAbsorptionTowardCloud);

                        if (transmittance < 0.01) break;
                    }
                    marchDst += stepSize;
                }


                half4 mainColor = tex2D(_MainTex, i.uv);
                half3 cloudColor = min(1, _LightColor0.rgb * lightEnergy);
                half3 finalColor = mainColor * transmittance + cloudColor;
                return half4(finalColor, 1);
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
