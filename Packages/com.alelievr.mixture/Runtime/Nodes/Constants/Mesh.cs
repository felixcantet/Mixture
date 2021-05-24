using System.Collections.Generic;
using UnityEngine;
using GraphProcessor;
using System.Linq;
using UnityEngine.Rendering;
using System;

namespace Mixture
{
	[Documentation(@"
Mesh in mixture, it also have transform a so you can apply any kind of transform operation, it will be applied when the node is processed. 
")]

	[System.Serializable, NodeMenuItem("Mesh/Mesh")]
	public class MeshNode : MixtureNode, ICreateNodeFrom<Mesh>
	{
		[Output("Mesh")]
		public MixtureMesh output;

		public override string	name => "Mesh";

		public override bool hasPreview => false;
		public override bool showDefaultInspector => true;
        public override bool	isRenamable => true;

		[Input, SerializeField]
        public Mesh mesh;
		public Vector3 scale = Vector3.one;
		// There is an issue with json serialization and new keyword :) 
		public Vector3 pos = Vector3.zero;
		public Vector3 eulerAngles = Vector3.zero;

		public bool InitializeNodeFromObject(Mesh value)
		{
			mesh = value;
			return true;
		}

		protected override bool ProcessNode(CommandBuffer cmd)
		{
			if (mesh == null)
				return false;

            output = new MixtureMesh(mesh);

			// Apply matrix to mesh
			var combine = new CombineInstance[1];
			combine[0].mesh = output.mesh;
			combine[0].transform = Matrix4x4.TRS(pos, Quaternion.Euler(eulerAngles), scale);

			output.mesh = new Mesh{ indexFormat = IndexFormat.UInt32 };
			output.mesh.CombineMeshes(combine);

			return true;
		}
    }
}