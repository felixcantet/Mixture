Shader "Hidden/Mixture/HeightBlend"
{	
	Properties
	{
		// By default a shader node is supposed to handle all the input texture dimension, we use a prefix to determine which one is used
		[InlineTexture] _MapA_2D("MapA", 2D) = "white" {}
		[InlineTexture] _HeightA_2D("Height A", 2D) = "white" {}
		[InlineTexture] _MapB_2D("MapB", 2D) = "white" {}
		[InlineTexture] _HeightB_2D("Height B", 2D) = "white" {}
		
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
			#include "heightblend.cginc"
            #pragma vertex CustomRenderTextureVertexShader
			#pragma fragment MixtureFragment
			#pragma target 3.0

			// The list of defines that will be active when processing the node with a certain dimension
            #pragma shader_feature CRT_2D CRT_3D CRT_CUBE

			// This macro will declare a version for each dimention (2D, 3D and Cube)
			TEXTURE_SAMPLER_X(_MapA);
			TEXTURE_SAMPLER_X(_MapB);
			TEXTURE_SAMPLER_X(_HeightA);
			TEXTURE_SAMPLER_X(_HeightB);

			float4 mixture (v2f_customrendertexture i) : SV_Target
			{
				float4 mapA = SAMPLE_X(_MapA, i.localTexcoord.xyz, i.direction);
				float4 mapB = SAMPLE_X(_MapB, i.localTexcoord.xyz, i.direction);

				
				
				float heightA = SAMPLE_X(_HeightA, i.localTexcoord.xyz, i.direction).x;
				float heightB = SAMPLE_X(_HeightB, i.localTexcoord.xyz, i.direction).x;
				return heightblend(mapA, heightA, mapB, heightB);
			}
			ENDHLSL
		}
	}
}
