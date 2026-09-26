Shader "KinectKids/Greve Living Mass"
{
    Properties
    {
        _Color ("Body", Color) = (0.002,0.002,0.006,1)
        _RimColor ("Soft edge", Color) = (0.025,0.018,0.042,1)
        _Opacity ("Opacity", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            float4 _Color;
            float4 _RimColor;
            float _Opacity;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float3 world : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.color = v.color;
                return o;
            }

            float hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }
            float valueNoise(float2 p)
            {
                float2 cell = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash21(cell), hash21(cell + float2(1,0)), f.x),
                    lerp(hash21(cell + float2(0,1)), hash21(cell + 1.0), f.x), f.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Both scales only disturb the feather. The inner body stays opaque.
                float slow = valueNoise(i.world.xy * 0.52 + float2(_Time.y * 0.13, -_Time.y * 0.08));
                float fine = valueNoise(i.world.xz * 1.65 + float2(-_Time.y * 0.21, _Time.y * 0.16));
                float fringe = i.color.a * (0.77 + slow * 0.32 + fine * 0.20);
                float alpha = i.color.a > 0.985 ? 1.0 : saturate(fringe);
                float3 tint = lerp(_RimColor.rgb, _Color.rgb, saturate(alpha * 1.8));
                return fixed4(tint, alpha * _Opacity);
            }
            ENDCG
        }
    }
}
