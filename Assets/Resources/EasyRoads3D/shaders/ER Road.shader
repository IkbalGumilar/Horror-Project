Shader "EasyRoads3D/ER Road"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Metallic ("Metallic AO Smoothness", 2D) = "gray" {}
        _MainMetallicPower ("Metallic Power", Range(0,2)) = 0
        _MainMetallicPower1 ("Metallic Power Legacy", Range(0,2)) = 0
        _MainSmoothnessPower ("Smoothness", Range(0,2)) = 1
        _MainSmoothnessPower1 ("Smoothness Legacy", Range(0,2)) = 1
        _OcclusionStrength ("Occlusion", Range(0,2)) = 1
        _OcclusionStrength1 ("Occlusion Legacy", Range(0,2)) = 1
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Range(0,4)) = 1
        _Show ("Show Surfaces", Range(0,1)) = 1
        _OffsetFactor ("Offset Factor", Range(-10,0)) = -1
        _OffsetUnit ("Offset Unit", Range(-10,0)) = -1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Offset [_OffsetFactor], [_OffsetUnit]
        Cull Back
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        CGPROGRAM
        #pragma surface surf Standard alpha:fade fullforwardshadows
        #pragma target 3.0
        sampler2D _MainTex, _Metallic, _BumpMap;
        fixed4 _Color;
        half _MainMetallicPower, _MainMetallicPower1;
        half _MainSmoothnessPower, _MainSmoothnessPower1;
        half _OcclusionStrength, _OcclusionStrength1, _BumpScale, _Show;
        struct Input { float2 uv_MainTex; fixed4 color : COLOR; };
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            fixed4 packed = tex2D(_Metallic, IN.uv_MainTex);
            half metallicPower = max(_MainMetallicPower, _MainMetallicPower1);
            half smoothnessPower = max(_MainSmoothnessPower, _MainSmoothnessPower1);
            half occlusionPower = max(_OcclusionStrength, _OcclusionStrength1);
            o.Albedo = c.rgb;
            o.Metallic = saturate(packed.r * metallicPower);
            o.Smoothness = saturate(packed.a * smoothnessPower);
            o.Occlusion = lerp(1, packed.g, saturate(occlusionPower));
            o.Normal = UnpackScaleNormal(tex2D(_BumpMap, IN.uv_MainTex), _BumpScale);
            o.Alpha = c.a * IN.color.a * _Show;
        }
        ENDCG
    }
    Fallback "Transparent/Diffuse"
}
