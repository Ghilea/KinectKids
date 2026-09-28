Shader "KinectKids/GreveWindowParallax"
{
    Properties
    {
        _FarTex ("Night landscape", 2D) = "black" {}
        _NearTex ("Moving mist", 2D) = "black" {}
        _FarOffset ("Far offset", Float) = 0
        _NearOffset ("Near offset", Float) = 0
        _WindowAspect ("Opening width / height", Float) = 0.19
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _FarTex;
            sampler2D _NearTex;
            float _FarOffset;
            float _NearOffset;
            float _WindowAspect;
            float4 _FarTex_TexelSize;
            float4 _NearTex_TexelSize;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Match the pointed opening rather than exposing the square
                // backdrop above the Gothic arch.
                float arch = saturate((i.uv.y - 0.78) / 0.22);
                float halfWidth = sqrt(saturate(1.0 - arch * arch)) * 0.5;
                clip(halfWidth - abs(i.uv.x - 0.5));
                // Crop the panorama to the aperture. Equal world distances in
                // X and Y must cover equal source pixels, preserving its aspect.
                float farSpan = 0.84 * _WindowAspect * _FarTex_TexelSize.w / _FarTex_TexelSize.z;
                float nearSpan = 0.84 * _WindowAspect * _NearTex_TexelSize.w / _NearTex_TexelSize.z;
                float2 farUv = float2(frac(farSpan * (i.uv.x - 0.5) + _FarOffset),
                    0.08 + 0.84 * i.uv.y);
                float2 nearUv = float2(frac(nearSpan * (i.uv.x - 0.5) + _NearOffset),
                    0.08 + 0.84 * i.uv.y);
                fixed4 farColor = tex2D(_FarTex, farUv);
                fixed4 nearColor = tex2D(_NearTex, nearUv);
                return fixed4(lerp(farColor.rgb, nearColor.rgb,
                    nearColor.a * 0.38), 1.0);
            }
            ENDCG
        }
    }
}
