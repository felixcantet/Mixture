Shader "Custom/SimpleShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Color1 ("Color", Color) = (1,1,1,1)
        _Color2 ("Color", Color) = (1,1,1,1)
        _Color3 ("Color", Color) = (1,1,1,1)
        _Color4 ("Color", Color) = (1,1,1,1)
        _Color5 ("Color", Color) = (1,1,1,1)
        _Color6 ("Color", Color) = (1,1,1,1)
        _Color7 ("Color", Color) = (1,1,1,1)
        _Color8 ("Color", Color) = (1,1,1,1)
        _Color9 ("Color", Color) = (1,1,1,1)
        _Color10 ("Color", Color) = (1,1,1,1)
        _Color11 ("Color", Color) = (1,1,1,1)
        _Color12 ("Color", Color) = (1,1,1,1)
        _Color13 ("Color", Color) = (1,1,1,1)
        _Color14 ("Color", Color) = (1,1,1,1)
        _Color15 ("Color", Color) = (1,1,1,1)
        _Color16 ("Color", Color) = (1,1,1,1)
        _Color17 ("Color", Color) = (1,1,1,1)
        _Color18 ("Color", Color) = (1,1,1,1)
        _Color19 ("Color", Color) = (1,1,1,1)
        _Color20 ("Color", Color) = (1,1,1,1)
        _Color21 ("Color", Color) = (1,1,1,1)
        _Color22 ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Normal ("Normal", 2D) = "white" {}
        _Emissive ("Emissive", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _Normal;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
            o.Normal = UnpackNormal (tex2D (_Normal, IN.uv_MainTex));
        }
        ENDCG
    }
    FallBack "Diffuse"
}