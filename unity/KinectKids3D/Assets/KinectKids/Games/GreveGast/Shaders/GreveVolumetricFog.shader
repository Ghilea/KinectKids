Shader "KinectKids/Greve Volumetric Fog"
{
    Properties
    {
        _FogColor ("Fog Color", Color) = (0,0,0,1)
        _Density ("Density", Range(0,8)) = 2
        _NoiseScale ("Noise Scale", Range(0.05,4)) = 0.55
        _NoiseSpeed ("Noise Speed", Vector) = (0.05,0.02,0.03,0)
        _EdgeSoftness ("Edge Softness", Range(0.02,0.8)) = 0.22
        _FloorMode ("Floor Mode", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent-10" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Front

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _CameraDepthTexture;
            float4 _FogColor;
            float _Density;
            float _NoiseScale;
            float4 _NoiseSpeed;
            float _EdgeSoftness;
            float _FloorMode;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 screenPos : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.pos);
                return o;
            }

            float hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float valueNoise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float n000 = hash31(i + float3(0,0,0));
                float n100 = hash31(i + float3(1,0,0));
                float n010 = hash31(i + float3(0,1,0));
                float n110 = hash31(i + float3(1,1,0));
                float n001 = hash31(i + float3(0,0,1));
                float n101 = hash31(i + float3(1,0,1));
                float n011 = hash31(i + float3(0,1,1));
                float n111 = hash31(i + float3(1,1,1));
                float4 nx = lerp(float4(n000,n010,n001,n011),
                                 float4(n100,n110,n101,n111), f.x);
                float2 ny = lerp(nx.xz, nx.yw, f.y);
                return lerp(ny.x, ny.y, f.z);
            }

            float fbm(float3 p)
            {
                float n = valueNoise(p) * 0.58;
                n += valueNoise(p * 2.03 + 7.1) * 0.29;
                n += valueNoise(p * 4.01 + 19.7) * 0.13;
                return n;
            }

            float2 rayBox(float3 ro, float3 rd)
            {
                float3 inv = 1.0 / (rd + sign(rd) * 1e-5);
                float3 t0 = (-0.5 - ro) * inv;
                float3 t1 = ( 0.5 - ro) * inv;
                float3 mn = min(t0, t1);
                float3 mx = max(t0, t1);
                return float2(max(max(mn.x, mn.y), mn.z),
                              min(min(mx.x, mx.y), mx.z));
            }

            float roundedVolumeMask(float3 p)
            {
                float2 q = abs(p.xy) * 2.0;
                float rounded = pow(pow(q.x, 6.0) + pow(q.y, 6.0), 1.0 / 6.0);
                float side = saturate((1.0 - rounded) / _EdgeSoftness);
                float depth = saturate((0.5 - abs(p.z)) / max(0.04, _EdgeSoftness * 0.45));
                return smoothstep(0.0, 1.0, side) * smoothstep(0.0, 1.0, depth);
            }

            float floorVolumeMask(float3 p)
            {
                float xEdge = saturate((0.5 - abs(p.x)) / max(0.03, _EdgeSoftness));
                float zEdge = saturate((0.5 - abs(p.z)) / max(0.03, _EdgeSoftness));
                float height = saturate(0.5 - p.y);
                height = height * height * (3.0 - 2.0 * height);
                return smoothstep(0.0, 1.0, xEdge) * smoothstep(0.0, 1.0, zEdge) * height;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 ro = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1)).xyz;
                float2 uv = i.screenPos.xy / i.screenPos.w;
                float4 clip = float4(uv * 2.0 - 1.0, 1.0, 1.0);
                float4 view = mul(unity_CameraInvProjection, clip);
                float3 worldDirection = mul((float3x3)unity_CameraToWorld,
                    normalize(view.xyz / max(0.0001, view.w)));
                float3 rd = normalize(mul((float3x3)unity_WorldToObject, worldDirection));
                float2 hit = rayBox(ro, rd);
                float tStart = max(hit.x, 0.0);
                float tEnd = hit.y;
                if (tEnd <= tStart) discard;

                float3 entryObject = ro + rd * tStart;
                float3 entryWorld = mul(unity_ObjectToWorld, float4(entryObject, 1)).xyz;
                float entryDepth = -mul(UNITY_MATRIX_V, float4(entryWorld, 1)).z;
                float sceneDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(
                    _CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos)));
                if (sceneDepth + 0.08 < entryDepth) discard;

                const int STEPS = 40;
                float stepLength = (tEnd - tStart) / STEPS;
                float alpha = 0.0;
                float timeOffset = _Time.y;
                [loop]
                for (int s = 0; s < STEPS; s++)
                {
                    float jitter = hash31(float3(uv * _ScreenParams.xy, s)) - 0.5;
                    float t = tStart + (s + 0.5 + jitter * 0.35) * stepLength;
                    float3 p = ro + rd * t;
                    float3 wp = mul(unity_ObjectToWorld, float4(p, 1)).xyz;
                    float noise = fbm(wp * _NoiseScale + _NoiseSpeed.xyz * timeOffset);
                    float mask = lerp(roundedVolumeMask(p), floorVolumeMask(p), _FloorMode);
                    // Break up only the outer part of the black silhouette.
                    // The interior retains a guaranteed density floor and can
                    // therefore never develop transparent pockets.
                    float warpedBlackMask = saturate(mask + (noise - 0.52) * 0.82);
                    warpedBlackMask = max(warpedBlackMask, mask * 0.72);
                    mask = lerp(warpedBlackMask, mask, _FloorMode);
                    // A non-zero base prevents holes while animated FBM gives
                    // the boundary and interior visible volumetric movement.
                    float density = _Density * mask * lerp(0.68, 1.32, noise);
                    float sampleAlpha = 1.0 - exp(-density * stepLength);
                    alpha += (1.0 - alpha) * sampleAlpha;
                    if (alpha > 0.995) break;
                }

                return fixed4(_FogColor.rgb, alpha * _FogColor.a);
            }
            ENDCG
        }
    }
}
