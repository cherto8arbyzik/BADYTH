Shader "Hollowwest/Starting Island 3"
{
    Properties
    {
        _BaseColorMap ("Base Color", 2D) = "white" {}
        _NormalMap ("Normal", 2D) = "bump" {}
        _MetallicMap ("Metallic", 2D) = "black" {}
        _RoughnessMap ("Roughness", 2D) = "white" {}
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        _NormalStrength ("Normal Strength", Range(0, 2)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 350

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.5

        sampler2D _BaseColorMap;
        sampler2D _NormalMap;
        sampler2D _MetallicMap;
        sampler2D _RoughnessMap;
        fixed4 _Tint;
        half _NormalStrength;

        struct Input
        {
            float2 uv_BaseColorMap;
        };

        void surf(Input input, inout SurfaceOutputStandard output)
        {
            fixed4 baseColor = tex2D(_BaseColorMap, input.uv_BaseColorMap);
            output.Albedo = baseColor.rgb * _Tint.rgb;
            output.Normal = UnpackScaleNormal(
                tex2D(_NormalMap, input.uv_BaseColorMap),
                _NormalStrength);
            output.Metallic = saturate(
                tex2D(_MetallicMap, input.uv_BaseColorMap).r);
            output.Smoothness = 1.0h - saturate(
                tex2D(_RoughnessMap, input.uv_BaseColorMap).r);
            output.Occlusion = 1.0h;
            output.Alpha = 1.0h;
        }
        ENDCG
    }

    FallBack "Standard"
}
