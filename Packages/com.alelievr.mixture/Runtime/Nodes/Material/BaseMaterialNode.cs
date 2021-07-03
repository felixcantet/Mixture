using UnityEngine;
using GraphProcessor;
using UnityEngine.Rendering;

namespace Mixture
{
	public class BaseMaterialNode : MixtureNode
	{
		public virtual Material previewMaterial => null;

		public override string	name => "BaseMaterialNode";
		public virtual bool needPropertySelector { get; }
		public virtual MixtureMaterial targetPropertySelector { get; }
		public sealed override Texture previewTexture => Texture2D.blackTexture;
	}
}