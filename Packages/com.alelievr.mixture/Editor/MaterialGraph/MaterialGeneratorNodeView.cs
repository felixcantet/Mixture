using UnityEngine.UIElements;
using GraphProcessor;
using UnityEditor;
namespace Mixture
{
	[NodeCustomEditor(typeof(MaterialGeneratorNode))]
	public class MaterialGeneratorNodeView : MixtureNodeView
	{
		MaterialGeneratorNode node;

		public override void Enable(bool fromInspector)
		{
			base.Enable(fromInspector);

			node = nodeTarget as MaterialGeneratorNode;

			var button = new Button(
				() => 
			{
				if (node.output == null)
					return;
				
				AssetDatabase.CreateAsset(node.output, "Assets/Resources/GeneratedMaterial.mat");
				
			});
			
			
			
            controlsContainer.Add(button);
		}
	}
}