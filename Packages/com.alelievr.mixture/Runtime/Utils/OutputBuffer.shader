﻿Shader "Hidden/Mixture/OutputBuffer"
{
    HLSLINCLUDE

    #pragma vertex Vert

    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone vulkan metal switch

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"

    // The PositionInputs struct allow you to retrieve a lot of useful information for your fullScreenShader:
    // struct PositionInputs
    // {
    //     float3 positionWS;  // World space position (could be camera-relative)
    //     float2 positionNDC; // Normalized screen coordinates within the viewport    : [0, 1) (with the half-pixel offset)
    //     uint2  positionSS;  // Screen space pixel coordinates                       : [0, NumPixels)
    //     uint2  tileCoord;   // Screen tile coordinates                              : [0, NumTiles)
    //     float  deviceDepth; // Depth from the depth buffer                          : [0, 1] (typically reversed)
    //     float  linearDepth; // View space Z coordinate                              : [Near, Far]
    // };

    // To sample custom buffers, you have access to these functions:
    // But be careful, on most platforms you can't sample to the bound color buffer. It means that you
    // can't use the SampleCustomColor when the pass color buffer is set to custom (and same for camera the buffer).
    // float4 SampleCustomColor(float2 uv);
    // float4 LoadCustomColor(uint2 pixelCoords);
    // float LoadCustomDepth(uint2 pixelCoords);
    // float SampleCustomDepth(float2 uv);

    // There are also a lot of utility function you can use inside Common.hlsl and Color.hlsl,
    // you can check them out in the source code of the core SRP package.

    float _OutputMode;

    float4 FullScreenPass(Varyings varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);
        float depth = LoadCameraDepth(varyings.positionCS.xy);
        PositionInputs posInput = GetPositionInput(varyings.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
        float3 viewDirection = GetWorldSpaceNormalizeViewDir(posInput.positionWS);
        float4 color = float4(CustomPassLoadCameraColor(varyings.positionCS.xy, 0), 1);
        float linearEyeDepth = LinearEyeDepth(posInput.positionWS, GetWorldToViewMatrix());
        float f = 1 / _ZBufferParams.w;
        float n = rcp((_ZBufferParams.x + 1) * _ZBufferParams.w);

        NormalData normalData;
        DecodeFromNormalBuffer(posInput.positionSS.xy, normalData);

        // Keep in sync with SceneNode.OutputBuffer
        switch (_OutputMode)
        {
            case 0: // Color
                return float4(color.rgb, depth != 0);
            case 1: // Eye Depth
                return float4((linearEyeDepth - n).xxx, depth != 0);
            case 2: // 01 Depth
                // Convert eye depth into linear (supports orthographic)
                float linear01Depth = (linearEyeDepth - n) / (f - n);
                return float4(1 - linear01Depth.xxx, depth != 0);
            case 3: // World Normal
                return float4(normalData.normalWS, depth != 0);
            case 4: // Tangent Space Normal
                float3 unsignedNormal = normalData.normalWS * 0.5 * (depth != 0) + 0.5;
                float3 tangentSpaceNormal = normalize(float3(unsignedNormal.xz, 1));
                return float4(tangentSpaceNormal, depth != 0);
            case 5: // World Position
                return float4(GetAbsolutePositionWS(posInput.positionWS), depth != 0);
            default: return color;
        }
    }

    ENDHLSL

    SubShader
    {
        // TODO: enable this for 2021.2
        // PackageRequirements {
        //     "com.unity.render-pipelines.high-definition"
        // }
        Pass
        {
            Name "Custom Pass 0"

            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
                #pragma fragment FullScreenPass
            ENDHLSL
        }
    }
    Fallback Off
}
