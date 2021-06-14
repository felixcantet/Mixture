Shader "Hidden/Mixture/MixtureLerpTexture"
{	
	Properties
	{
		// By default a shader node is supposed to handle all the input texture dimension, we use a prefix to determine which one is used
		[InlineTexture]_Mask("Source", 2D) = "white" {}
		[InlineTexture]_MatA("Source", 2D) = "white" {}
		[InlineTexture]_MatB("Source", 2D) = "white" {}

		// Other parameters
	}
		SubShader
	{
		Tags { "RenderType" = "Opaque" }
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
			//TEXTURE_SAMPLER_X(_Source);
			sampler _Mask;
			sampler _MatA;
			sampler _MatB;

			float4 mixture (v2f_customrendertexture i) : SV_Target
			{
				float mask = tex2D(_Mask, i.localTexcoord.xy);
				float4 matA = tex2D(_MatA, i.localTexcoord.xy);
				float4 matB = tex2D(_MatB, i.localTexcoord.xy);
				return lerp(matA, matB, mask);
			}
			ENDHLSL
		}
	}
}
