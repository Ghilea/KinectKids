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

    float height = saturate((0.18 - p.y) / 0.68);
    height = smoothstep(0.0, 1.0, height);
    height = pow(height, 2.8);

    return smoothstep(0.0, 1.0, xEdge) *
           smoothstep(0.0, 1.0, zEdge) *
           height;
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

                const int STEPS = 56;

float stepLength = (tEnd - tStart) / STEPS;

float alpha = 0.0;
float3 accumulatedColor = float3(0.0, 0.0, 0.0);

// Används bara av Grevens mörker.
// Håller reda på om kamerans ray passerar den solida kärnan.
float maxBlackCore = 0.0;

float timeOffset = _Time.y;

[loop]
for (int s = 0; s < STEPS; s++)
{
    float jitter =
        hash31(float3(uv * _ScreenParams.xy, s)) - 0.5;

    float t =
        tStart +
        (s + 0.5 + jitter * 0.35) * stepLength;

    float3 p = ro + rd * t;

    float3 wp =
        mul(unity_ObjectToWorld, float4(p, 1)).xyz;

    // ------------------------------------------------
    // Stoppa dimman när vi träffar riktig geometri.
    // ------------------------------------------------

    float sampleViewDepth =
        -mul(UNITY_MATRIX_V, float4(wp, 1)).z;

    if (sampleViewDepth >= sceneDepth - 0.04)
        break;

    // ------------------------------------------------
    // Två noise-skalor.
    // Stor noise = stora dimbankar.
    // Liten noise = detaljer inuti dem.
    // ------------------------------------------------

    float largeNoise =
        fbm(
            wp * _NoiseScale +
            _NoiseSpeed.xyz * timeOffset
        );

    float fineNoise =
        fbm(
            wp * (_NoiseScale * 2.15) -
            _NoiseSpeed.xyz * timeOffset * 0.65 +
            11.7
        );

    float noise =
        saturate(
            largeNoise * 0.72 +
            fineNoise * 0.28
        );

   float baseMask =
    lerp(
        roundedVolumeMask(p),
        floorVolumeMask(p),
        _FloorMode
    );

float mask = baseMask;

if (_FloorMode < 0.5)
{
    // Noise påverkar framför allt ytterkanten.
    // Kärnan får aldrig tunnas ut av noise.
    float warpedBlackMask =
        saturate(
            baseMask +
            (noise - 0.52) * 0.38
        );

    warpedBlackMask =
        max(
            warpedBlackMask,
            baseMask * 0.92
        );

    // Solid kärna inne i Grevens mörker.
    float coreMask =
        smoothstep(
            0.28,
            0.68,
            baseMask
        );

    maxBlackCore =
        max(
            maxBlackCore,
            coreMask
        );

    mask =
        max(
            warpedBlackMask,
            coreMask
        );
}

    // ------------------------------------------------
    // Mycket större skillnad mellan tunn och tjock dimma.
    // ------------------------------------------------

    float densityVariation =
        lerp(
            0.42,
            1.55,
            smoothstep(0.18, 0.85, noise)
        );

  float density;

if (_FloorMode < 0.5)
{
    // Grevens mörker:
    // ingen kamerafade och extremt tät kärna.
    float solidCore =
        smoothstep(
            0.28,
            0.68,
            baseMask
        );

    density =
        _Density *
        (
            mask * lerp(0.90, 1.35, noise) +
            solidCore * 5.0
        );
}
else
{
    // Blå golvdimma:
    // tunnare nära kameran och tätare längre bort.
    density =
        _Density *
        mask *
        densityVariation;

    float nearFade =
        smoothstep(
            4.0,
            8.0,
            sampleViewDepth
        );

    density *= nearFade;

    float depthDensity =
        lerp(
            0.90,
            1.30,
            saturate(
                (sampleViewDepth - 10.0) / 45.0
            )
        );

    density *= depthDensity;
}

    float sampleAlpha =
        1.0 -
        exp(-density * stepLength);

    float contribution =
        (1.0 - alpha) *
        sampleAlpha;

    // ------------------------------------------------
    // Dimman får intern ljusvariation.
    // Detta ger mycket mer 3D-känsla.
    // ------------------------------------------------

    float depthShade =
        lerp(
            1.10,
            0.72,
            saturate(
                (sampleViewDepth - 8.0) / 50.0
            )
        );

    float noiseLight =
        lerp(
            0.78,
            1.20,
            noise
        );

    float3 sampleColor =
        _FogColor.rgb *
        depthShade *
        noiseLight;

    accumulatedColor +=
        sampleColor *
        contribution;

    alpha += contribution;

    if (alpha > 0.995)
        break;
}

float3 finalColor =
    accumulatedColor /
    max(alpha, 0.0001);

if (_FloorMode < 0.5)
{
    // Där rayen går genom kärnan ska bakgrunden inte kunna synas.
    alpha =
        max(
            alpha,
            maxBlackCore * 0.995
        );

    // Grevens mörker ska verkligen vara svart,
    // inte grått från noise/depth shading.
    finalColor = float3(0.0, 0.0, 0.0);
}

return fixed4(
    finalColor,
    alpha * _FogColor.a
);

              
            }
            ENDCG
        }
    }
}
