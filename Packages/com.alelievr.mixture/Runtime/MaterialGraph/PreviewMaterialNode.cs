using UnityEngine;
using GraphProcessor;
using UnityEngine.Rendering;

namespace Mixture
{
	[System.Serializable, NodeMenuItem("Custom/PreviewMaterialNode")]
	public class PreviewMaterialNode : BaseMaterialNode
	{
        [Input]
        public MixtureMaterial input;

        [Output]
        public MixtureMaterial output;

		public override string	name => "PreviewMaterialNode";
		public override Material previewMaterial => input.material;

		public override bool hasPreview => true;
		//public override Texture previewTexture => output;

		protected override void Enable()
		{
			//UpdateTempRenderTexture(ref output);
		}

		protected override bool ProcessNode(CommandBuffer cmd)
		{
            if (!base.ProcessNode(cmd))
                return false;

            // Update temp target in case settings changes
			//UpdateTempRenderTexture(ref output);

            // Insert your code here 
            if (input == null)
	            return false;

            output = input;
			return true;
		}

        protected override void Disable()
		{
			base.Disable();
		}
    }
}