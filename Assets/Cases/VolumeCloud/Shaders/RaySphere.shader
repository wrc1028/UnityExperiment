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

            float2 RaySphereDst(float3 center, float radius, float3 rayOrigin, float3 rayDir)
            {
                float3 rayOriginToCenter = center - rayOrigin;
                float rayBottom = dot(rayOriginToCenter, rayDir);
                float centerToRay2 = dot(rayOriginToCenter, rayOriginToCenter) - rayBottom * rayBottom;
                float rBottom2 = radius * radius - centerToRay2;
                if (rBottom2 >= 0)
                {
                    float rBottom = sqrt(rBottom2);
                    float dstToSphere = rayBottom - rBottom;
                    float dstInsideSphere = dstToSphere < 0 ? rayBottom + rBottom : rBottom * 2;
                    return float2(dstToSphere, dstInsideSphere);
                }
                return float2(1.#INF, 0);
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
                // 包围盒
                float density = 0;
                float2 hitOuterSphere = RaySphereDst(_RaySphereCenter, _SphereRadius + _SphereHeight, rayOrigin, rayDir);
                float2 firstThroughInfo = 0;
                float2 secondThroughInfo = 0;
                if (hitOuterSphere.y > 0)
                {
                    float2 hitInnerSphere = RaySphereDst(_RaySphereCenter, _SphereRadius, rayOrigin, rayDir);
                    if (hitInnerSphere.y > 0)
                    {
                        firstThroughInfo = float2(max(0, hitOuterSphere.x), (hitOuterSphere.y - hitInnerSphere.y + min(0, hitOuterSphere.x) + min(0, hitInnerSphere.x)) / 2);
                        secondThroughInfo = float2(max(0, hitInnerSphere.x) + hitInnerSphere.y, (hitOuterSphere.y - hitInnerSphere.y - min(0, hitOuterSphere.x) + min(0, hitInnerSphere.x)) / 2);
                    }
                    else firstThroughInfo = float2(max(0, hitOuterSphere.x), hitOuterSphere.y);
                }
                if (firstThroughInfo.y > 0) density += DensityMarch(rayOrigin, rayDir, firstThroughInfo.x, firstThroughInfo.y, linearDepth);
                if (secondThroughInfo.y > 0) density += DensityMarch(rayOrigin, rayDir, secondThroughInfo.x, secondThroughInfo.y, linearDepth);
                return col * exp(-density);
            }
            ENDCG
        }
    }
}
