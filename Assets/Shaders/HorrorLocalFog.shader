Shader "Horror/Local Fog"
{
    Properties
    {
        _Color ("Fog Color", Color) = (0.04, 0.05, 0.055, 0.72)
        _MainTex ("Noise Texture", 2D) = "white" {}
        _Density ("Density", Range(0, 3)) = 1.15
        _Softness ("Depth Softness", Range(0.01, 20)) = 4
        _HeightFade ("Height Fade", Range(0, 8)) = 1.25
        _EdgeFade ("Edge Fade", Range(0, 1)) = 0.18
        _NoiseScale ("Noise Scale", Range(0.01, 8)) = 1.7
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.55
        _DriftSpeed ("Drift Speed", Vector) = (0.025, 0.008, 0, 0)
        _Flicker ("Uneven Flicker", Range(0, 0.35)) = 0.08
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _CameraDepthTexture;
            fixed4 _Color;
            float _Density, _Softness, _HeightFade, _EdgeFade;
            float _NoiseScale, _NoiseStrength, _Flicker;
            float4 _DriftSpeed;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float3 localPos : TEXCOORD2;
            };

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.screenPos = ComputeScreenPos(o.pos);
                COMPUTE_EYEDEPTH(o.screenPos.z);
                o.localPos = v.vertex.xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 drift = _Time.y * _DriftSpeed.xy;
                float2 noiseUv = i.uv * _NoiseScale + drift;
                float noise = lerp(1.0, (tex2D(_MainTex, noiseUv).r + hash21(noiseUv * 5.0)) * 0.5, _NoiseStrength);
                float rawDepth = SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos));
                float sceneDepth = LinearEyeDepth(rawDepth);
                float depthFade = rawDepth > 0.0001 && rawDepth < 0.9999
                    ? saturate((sceneDepth - i.screenPos.z) / max(_Softness, 0.001)) : 1.0;
                float heightMask = saturate(1.0 - abs(i.localPos.y) * _HeightFade);
                float edgeX = smoothstep(0.0, _EdgeFade, i.uv.x) * smoothstep(0.0, _EdgeFade, 1.0 - i.uv.x);
                float edgeY = smoothstep(0.0, _EdgeFade, i.uv.y) * smoothstep(0.0, _EdgeFade, 1.0 - i.uv.y);
                float flicker = 1.0 + sin(_Time.y * 1.73 + noise * 6.2831) * _Flicker;
                fixed4 col = _Color;
                col.rgb *= lerp(0.65, 1.15, noise);
                col.a = saturate(_Color.a * _Density * noise * heightMask * edgeX * edgeY * depthFade * flicker);
                return col;
            }
            ENDCG
        }
    }
    Fallback Off
}
