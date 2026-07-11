Shader "Horror/Built-In Conifer Bark"
{
    Properties
    {
        _BaseMap ("Albedo", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1,1,1,1)
        _BumpOcclusionMap ("Normal Occlusion", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Range(0,4)) = 1
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Back

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0

        sampler2D _BaseMap;
        sampler2D _BumpOcclusionMap;
        fixed4 _BaseColor;
        half _BumpScale;
        half _Smoothness;

        struct Input
        {
            float2 uv_BaseMap;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 albedo = tex2D(_BaseMap, IN.uv_BaseMap) * _BaseColor;
            fixed4 packedNormal = tex2D(_BumpOcclusionMap, IN.uv_BaseMap);
            o.Albedo = albedo.rgb;
            o.Normal = UnpackScaleNormal(packedNormal, _BumpScale);
            o.Metallic = 0;
            o.Smoothness = saturate(_Smoothness * packedNormal.a);
            o.Occlusion = packedNormal.g;
            o.Alpha = 1;
        }
        ENDCG
    }

    Fallback "Diffuse"
}
