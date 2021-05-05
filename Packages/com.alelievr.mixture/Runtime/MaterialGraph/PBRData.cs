using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GraphProcessor;
using System.Linq;
using UnityEngine.Rendering;

namespace Mixture
{
	public struct PBRStructure
    {
		public Texture Albedo;
		public Texture Height;
		public Texture Normal;
		public Texture Metallic;
		public Texture Occlusion;
		public Texture Emissive;

		public PBRStructure(PBRStructureOut fromOut) 
		{
			Albedo = fromOut.Albedo;
			Height = fromOut.Height;
			Normal = fromOut.Normal;
			Metallic = fromOut.Metallic;
			Occlusion = fromOut.Occlusion;
			Emissive = fromOut.Emissive;
		}
    }

	public struct PBRStructureOut
	{
		public CustomRenderTexture Albedo;
		public CustomRenderTexture Height;
		public CustomRenderTexture Normal;
		public CustomRenderTexture Metallic;
		public CustomRenderTexture Occlusion;
		public CustomRenderTexture Emissive;
	}


	[System.Serializable, NodeMenuItem("Custom/Pack PBR Data")]
	public class PBRData : MixtureNode
	{

		[Input("Albedo")] public Texture Albedo;
		[Input("Height")] public Texture Height;
		[Input("Normal")] public Texture Normal;
		[Input("Metallic")] public Texture Metallic;
		[Input("Occlusion")] public Texture Occlusion;
		[Input("Emissive")] public Texture Emissive;

		[Output("PBR Structure")] public PBRStructure structure;

		public override string name => "Pack PBR Data";

        [CustomPortOutput(nameof(structure), typeof(PBRStructure))]
		public void OutputPortBehaviour(List<SerializableEdge> edges)
        {
			structure = new PBRStructure
			{
				Albedo = this.Albedo,
				Height = this.Height,
				Normal = this.Normal,
				Metallic = this.Metallic,
				Occlusion = this.Occlusion,
				Emissive = this.Emissive
			};
			foreach(var item in edges)
            {
				item.passThroughBuffer = structure;
            }
        }

    }
}