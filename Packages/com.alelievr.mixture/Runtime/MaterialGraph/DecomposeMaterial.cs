using UnityEngine;
using GraphProcessor;
using UnityEngine.Rendering;
using System.Collections.Generic;
namespace Mixture
{
	[System.Serializable, NodeMenuItem("Custom/DecomposeMaterial")]
	public class DecomposeMaterial : MixtureNode
	{
        [Input]
        public Material input;

        [Output]
        public Texture output;

		public override string	name => "Decompose Material";

		public override Texture previewTexture => output;

		protected override void Enable()
		{
		}

		protected override bool ProcessNode(CommandBuffer cmd)
		{
            if (!base.ProcessNode(cmd))
                return false;

            // Update temp target in case settings changes



            // Insert your code here 

			return true;
		}

        protected override void Disable()
		{
			base.Disable();
		}

		[CustomPortInput(nameof(input), typeof(Material))]
		public void GetFirstTexture(List<SerializableEdge> edges)
		{
			Debug.Log("Curstom POrt Input");
			Debug.Log(edges.Count);
			//Debug.Log(input.GetTexture("_MainTex"));
			if(edges.Count > 0)
			{
				this.input = edges[0].passThroughBuffer as Material;
			}
			if (input == null)
				return;
			var texs = input.GetTexturePropertyNames();
			Debug.Log("TextureName = " + texs[0]);
			output = input.GetTexture(texs[0]);
		}
    }
}