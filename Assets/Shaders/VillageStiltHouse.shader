Shader "Horror/Village Stilt House"
{
    Properties
    {
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.08
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Back

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow vertex:vert
        #pragma target 3.0

        fixed4 _Tint;
        half _Smoothness;

        struct Input
        {
            fixed4 vertexColor;
        };

        void vert(inout appdata_full vertex, out Input output)
        {
            UNITY_INITIALIZE_OUTPUT(Input, output);
            output.vertexColor = vertex.color;
        }

        void surf(Input input, inout SurfaceOutputStandard output)
        {
            fixed4 color = input.vertexColor * _Tint;
            output.Albedo = color.rgb;
            output.Metallic = 0;
            output.Smoothness = _Smoothness;
            output.Occlusion = 1;
            output.Alpha = 1;
        }
        ENDCG
    }

    Fallback "Diffuse"
}
