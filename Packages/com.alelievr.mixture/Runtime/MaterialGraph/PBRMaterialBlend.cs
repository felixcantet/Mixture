using UnityEngine;
using GraphProcessor;
using UnityEngine.Rendering;

namespace Mixture
{
	[System.Serializable, NodeMenuItem("Custom/PBR Material Blending")]
	public class PBRMaterialBlend : MixtureNode
	{
        [Input("Material A")]
        public PBRStructure MaterialA;

        [Input("Material B")]
        public PBRStructure MaterialB;

        [Output]
        public PBRStructure output;

        RenderTexture controlMap;

		public override string	name => "PBR Material Blender";

        protected override void Enable()
        {
            
            graph.outputNode.onSettingsChanged += CreateControlMap;
        }

        void CreateControlMap()
        {
            if(this.controlMap != null && this.controlMap.IsCreated())
            {
                this.controlMap.Release();
            }
            controlMap = new RenderTexture(graph.outputNode.rtSettings.width, graph.outputNode.rtSettings.height, 0);
            controlMap.enableRandomWrite = true;
            controlMap.Create();
        }


        protected override void Disable()
        {
            graph.outputNode.onSettingsChanged -= CreateControlMap;
        }

        protected override void Destroy()
        {
            graph.outputNode.onSettingsChanged -= CreateControlMap;
        }

        protected override bool ProcessNode(CommandBuffer cmd)
		{
            if (!base.ProcessNode(cmd))
                return false;
            //cmd.Blit()
            GetControlMap();


			return true;
		}

		Texture GetControlMap()
        {
           
        }

        
    }
}