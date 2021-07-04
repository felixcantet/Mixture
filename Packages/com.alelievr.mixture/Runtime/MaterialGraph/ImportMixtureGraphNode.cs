using UnityEngine;
using GraphProcessor;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;


namespace Mixture
{
	[System.Serializable, NodeMenuItem("Custom/Import Mixture Graph Node")]
	public class ImportMixtureGraphNode : MixtureNode
	{
        [Output]
        public List<object> output;

        [SerializeField] private Texture graphTexture;
        [HideInInspector] public MixtureGraph importedGraph;
        private Texture baseTexture;
        [HideInInspector] public MixtureVariant variant;
        [HideInInspector][SerializeField] public List<ExposedParameter> overrides;
        [HideInInspector] public List<Texture> readBack = new List<Texture>(); 
        
        public override bool showDefaultInspector => true;
        public override bool needsInspector => true;
        public override string	name => "Import Mixture Graph";
		public override bool isRenamable => true;
		public int previewIndex = 0;
		public override Texture previewTexture
		{
			get
			{
				if (importedGraph != null && readBack != null)
				{
					if (readBack.Count == 0)
						return null;
					
						return readBack[
							Mathf.Min(previewIndex, readBack.Count - 1)];
					
				}

				return null;
			}
		}

		protected override void Enable()
		{
			
		}

		public void LoadGraph()
		{
			importedGraph = MixtureDatabase.GetGraphFromTexture(graphTexture);
			variant = null;
			if (importedGraph == null)
			{
				AddMessage("The texture you assigned must be a Mixture Texture", NodeMessageType.Error);
				return;
			}

			
			variant = ScriptableObject.CreateInstance<MixtureVariant>();
			variant.SetParent(importedGraph);
			readBack = new List<Texture>();
			foreach (var item in importedGraph.outputNode.outputTextureSettings)
			{
				readBack.Add(new Texture2D(importedGraph.settings.GetResolvedWidth(importedGraph), importedGraph.settings.GetResolvedWidth(importedGraph)));
			}
		}
		
		protected override bool ProcessNode(CommandBuffer cmd)
		{
            if (!base.ProcessNode(cmd))
                return false;
            this.ClearMessages();
            if (graphTexture == null)
            {
	            this.AddMessage("There is no texture assign to load Mixture Graph", NodeMessageType.Error);
	            return false;
            }

            // if (baseTexture != graphTexture)
            // {
	           //  
            // }

            if (importedGraph == null)
            {
	            LoadGraph();
	            // importedGraph = MixtureDatabase.GetGraphFromTexture(graphTexture);
	            // if (graph == null)
	            // {
		           //  AddMessage("The texture you assigned must be a Mixture Texture", NodeMessageType.Error);
		           //  return false;
	            // }




            }

            if (importedGraph != null && variant != null)
            {
	            //MixtureGraphProcessor processor = new MixtureGraphProcessor(importedGraph);
	            //processor.Run();

	            foreach (var item in overrides)
	            {
		            if (item.value == null)
			            continue;
		            variant.SetParameterValue(item.name, item.value);
	            }
	            
	            variant.ProcessGraphWithOverrides();
	            // importedGraph.UpdateAllVariantTextures();
	            foreach (var item in variant.parentGraph.outputNode.outputTextureSettings)
	            {
		            var index = variant.parentGraph.outputNode.outputTextureSettings.IndexOf(item);
					variant.parentGraph.ReadBackTexture(variant.parentGraph.outputNode, item.finalCopyRT, externalTexture: this.readBack[index]);
		            
	            }

            }
            

            // Insert your code here 

			return true;
		}

		// void CreateNewVariant()
		// {
		// 	var graph = MixtureDatabase.GetGraphFromTexture(graphTexture);
		// 	if (graph == null)
		// 	{
		// 		AddMessage("The texture you assigned must be a Mixture Texture", NodeMessageType.Error);
		// 		return false;
		// 	}
		//
		// 	this.importedGraph = ScriptableObject.CreateInstance<MixtureVariant>();
		// 	importedGraph.SetParent(graph);
		// }
		[CustomPortBehavior(nameof(output))]
		IEnumerable<PortData> CreatePortDataForGraphOutput(List<SerializableEdge> edges)
		{
			
			
			if (importedGraph == null)
				yield break;

			foreach (var item in importedGraph.outputNode.outputTextureSettings)
			{
				yield return new PortData()
				{
					displayName = item.name,
					displayType = typeof(Texture2D),
					identifier = importedGraph.outputNode.outputTextureSettings.IndexOf(item).ToString()
				};
			}
		}

		[CustomPortOutput(nameof(output), typeof(object))]
		void PushOutput(List<SerializableEdge> edges)
		{
			if (importedGraph == null)
				return;
			foreach (var item in edges)
			{
				//item.passThroughBuffer = importedGraph.outputTextures[int.Parse(item.outputPortIdentifier)] as Texture;
				item.passThroughBuffer = this.readBack[int.Parse(item.outputPortIdentifier)] as Texture;
			}
		}
		
        protected override void Disable()
		{
			base.Disable();
		}
    }
}