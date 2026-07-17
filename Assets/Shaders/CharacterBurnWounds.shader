Shader "Horror/Character Burn Wounds"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Range(0,2)) = 1
        _Metallic ("Metallic", Range(0,1)) = 0
        _Glossiness ("Smoothness", Range(0,1)) = 0.35

        [Header(Burn Wounds)]
        _BurnAmount ("Burn Amount", Range(0,1)) = 0
        _BurnScale ("Burn Pattern Scale", Range(1,20)) = 7
        _BurnDarkColor ("Charred Color", Color) = (0.025,0.004,0.002,1)
        _BurnRawColor ("Fresh Burn Color", Color) = (0.38,0.018,0.006,1)
        [HDR] _BurnEmissionColor ("Fresh Edge Emission", Color) = (0.22,0.008,0.002,1)
        _BurnEmissionStrength ("Fresh Edge Strength", Range(0,2)) = 0.2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _BumpMap;
        fixed4 _Color;
        half _BumpScale;
        half _Metallic;
        half _Glossiness;
        half _BurnAmount;
        half _BurnScale;
        fixed4 _BurnDarkColor;
        fixed4 _BurnRawColor;
        fixed4 _BurnEmissionColor;
        half _BurnEmissionStrength;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
        };

        float Hash21(float2 value)
        {
            value = frac(value * float2(123.34, 456.21));
            value += dot(value, value + 45.32);
            return frac(value.x * value.y);
        }

        float ValueNoise(float2 uv)
        {
            float2 cell = floor(uv);
            float2 local = frac(uv);
            local = local * local * (3.0 - 2.0 * local);

            float bottom = lerp(Hash21(cell), Hash21(cell + float2(1, 0)), local.x);
            float top = lerp(Hash21(cell + float2(0, 1)), Hash21(cell + float2(1, 1)), local.x);
            return lerp(bottom, top, local.y);
        }

        float BurnNoise(float2 uv)
        {
            float noise = ValueNoise(uv);
            noise += ValueNoise(uv * 2.07 + 9.13) * 0.5;
            noise += ValueNoise(uv * 4.03 + 21.7) * 0.25;
            return noise / 1.75;
        }

        void surf(Input IN, inout SurfaceOutputStandard output)
        {
            fixed4 baseColor = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            float burn = saturate(_BurnAmount);
            float noise = BurnNoise(IN.uv_MainTex * _BurnScale);
            float threshold = 1.0 - burn;
            float woundMask = smoothstep(threshold - 0.12, threshold + 0.08, noise);
            woundMask *= step(0.0001, burn);
            woundMask = lerp(woundMask, 1.0, smoothstep(0.94, 1.0, burn));

            float charAmount = saturate(burn * 0.8 + noise * 0.35);
            fixed3 woundColor = lerp(_BurnRawColor.rgb, _BurnDarkColor.rgb, charAmount);
            float freshEdge = 1.0 - smoothstep(0.0, 0.075, abs(noise - threshold));
            freshEdge *= woundMask * (1.0 - smoothstep(0.8, 1.0, burn));

            output.Albedo = lerp(baseColor.rgb, woundColor, woundMask);
            output.Normal = UnpackScaleNormal(tex2D(_BumpMap, IN.uv_BumpMap), _BumpScale);
            output.Metallic = lerp(_Metallic, 0.0, woundMask);
            output.Smoothness = lerp(_Glossiness, 0.08, woundMask);
            output.Emission = _BurnEmissionColor.rgb * freshEdge * _BurnEmissionStrength;
            output.Alpha = baseColor.a;
        }
        ENDCG
    }

    FallBack "Standard"
}
