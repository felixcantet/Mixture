Shader "Hidden/Mixture/BlendTextureByMask"
{	
	Properties
	{
		// By default a shader node is supposed to handle all the input texture dimension, we use a prefix to determine which one is used
		[InlineTexture] _MapA_2D("MapA", 2D) = "white" {}
		[InlineTexture] _MapB_2D("MapA", 2D) = "white" {}
		[InlineTexture] _Mask_2D("MapA", 2D) = "white" {}

		// Other parameters
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			HLSLPROGRAM
			#include "Packages/com.alelievr.mixture/Runtime/Shaders/MixtureFixed.hlsl"
            #pragma vertex CustomRenderTextureVertexShader
			#pragma fragment MixtureFragment
			#pragma target 3.0

			// The list of defines that will be active when processing the node with a certain dimension
            #pragma shader_feature CRT_2D CRT_3D CRT_CUBE

			// This macro will declare a version for each dimention (2D, 3D and Cube)
			TEXTURE_SAMPLER_X(_MapA);
			TEXTURE_SAMPLER_X(_MapB);
			TEXTURE_SAMPLER_X(_Mask);

			float4 mixture (v2f_customrendertexture i) : SV_Target
			{
				// The SAMPLE_X macro handles sampling for 2D, 3D and cube textures
				float4 mapA = SAMPLE_X(_MapA, i.localTexcoord.xyz, i.direction);
				float4 mapB = SAMPLE_X(_MapB, i.localTexcoord.xyz, i.direction);
				float mask = SAMPLE_X(_Mask, i.localTexcoord.xyz, i.direction);

				
				return lerp(mapA, mapB, mask);
			}
			ENDHLSL
		}
	}
}
