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
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        LOD 100
        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            float4 _MainTex_ST;
            float _Density;
            float _Softness;
            float _HeightFade;
            float _EdgeFade;
            float _NoiseScale;
            float _NoiseStrength;
            float4 _DriftSpeed;
            float _Flicker;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
                float eyeDepth : TEXCOORD3;
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 uv)
            {
                float2 cell = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);

                float a = Hash21(cell);
                float b = Hash21(cell + float2(1.0, 0.0));
                float c = Hash21(cell + float2(0.0, 1.0));
                float d = Hash21(cell + float2(1.0, 1.0));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS);
                output.positionCS = positionInputs.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.positionOS = input.positionOS;
                output.eyeDepth = -positionInputs.positionVS.z;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 drift = _Time.y * _DriftSpeed.xy;
                float2 noiseUvA = input.uv * _NoiseScale + drift;
                float2 noiseUvB = input.uv * (_NoiseScale * 0.47) - drift.yx * 1.7;

                float texNoise = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, noiseUvA).r;
                float noiseA = ValueNoise(noiseUvA * 3.0);
                float noiseB = ValueNoise(noiseUvB * 6.0);
                float noise = lerp(1.0, (noiseA * 0.55 + noiseB * 0.35 + texNoise * 0.25), _NoiseStrength);

                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float rawDepth = SampleSceneDepth(screenUV);
                float sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float depthFade = 1.0;

                #if UNITY_REVERSED_Z
                bool hasDepth = rawDepth > 0.0001;
                #else
                bool hasDepth = rawDepth < 0.9999;
                #endif

                if (hasDepth)
                {
                    depthFade = saturate((sceneDepth - input.eyeDepth) / max(_Softness, 0.001));
                }

                float heightMask = saturate(1.0 - abs(input.positionOS.y) * _HeightFade);
                float edgeX = smoothstep(0.0, _EdgeFade, input.uv.x) * smoothstep(0.0, _EdgeFade, 1.0 - input.uv.x);
                float edgeY = smoothstep(0.0, _EdgeFade, input.uv.y) * smoothstep(0.0, _EdgeFade, 1.0 - input.uv.y);
                float edgeMask = edgeX * edgeY;

                float flicker = 1.0 + sin(_Time.y * 1.73 + noiseA * 6.2831) * _Flicker;
                float alpha = _Color.a * _Density * noise * heightMask * edgeMask * depthFade * flicker;

                half4 col = _Color;
                col.rgb *= lerp(0.65, 1.15, noise);
                col.a = saturate(alpha);
                return col;
            }
            ENDHLSL
        }
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        LOD 100
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
            float _Density;
            float _Softness;
            float _HeightFade;
            float _EdgeFade;
            float _NoiseScale;
            float _NoiseStrength;
            float4 _DriftSpeed;
            float _Flicker;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float3 localPos : TEXCOORD2;
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 uv)
            {
                float2 cell = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);

                float a = Hash21(cell);
                float b = Hash21(cell + float2(1.0, 0.0));
                float c = Hash21(cell + float2(0.0, 1.0));
                float d = Hash21(cell + float2(1.0, 1.0));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
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
                float2 noiseUvA = i.uv * _NoiseScale + drift;
                float2 noiseUvB = i.uv * (_NoiseScale * 0.47) - drift.yx * 1.7;

                float texNoise = tex2D(_MainTex, noiseUvA).r;
                float noiseA = ValueNoise(noiseUvA * 3.0);
                float noiseB = ValueNoise(noiseUvB * 6.0);
                float noise = lerp(1.0, (noiseA * 0.55 + noiseB * 0.35 + texNoise * 0.25), _NoiseStrength);

                float rawDepth = SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos));
                float sceneDepth = LinearEyeDepth(rawDepth);
                float depthFade = 1.0;

                if (rawDepth > 0.0001 && rawDepth < 0.9999)
                {
                    depthFade = saturate((sceneDepth - i.screenPos.z) / max(_Softness, 0.001));
                }

                float heightMask = saturate(1.0 - abs(i.localPos.y) * _HeightFade);
                float edgeX = smoothstep(0.0, _EdgeFade, i.uv.x) * smoothstep(0.0, _EdgeFade, 1.0 - i.uv.x);
                float edgeY = smoothstep(0.0, _EdgeFade, i.uv.y) * smoothstep(0.0, _EdgeFade, 1.0 - i.uv.y);
                float edgeMask = edgeX * edgeY;

                float flicker = 1.0 + sin(_Time.y * 1.73 + noiseA * 6.2831) * _Flicker;
                float alpha = _Color.a * _Density * noise * heightMask * edgeMask * depthFade * flicker;

                fixed4 col = _Color;
                col.rgb *= lerp(0.65, 1.15, noise);
                col.a = saturate(alpha);
                return col;
            }
            ENDCG
        }
    }

    Fallback Off
}
