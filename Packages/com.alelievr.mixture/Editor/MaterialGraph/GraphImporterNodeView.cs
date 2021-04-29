using UnityEngine.UIElements;
using GraphProcessor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
namespace Mixture
{
	[NodeCustomEditor(typeof(GraphImporterNode))]
	public class GraphImporterNodeView : MixtureNodeView
	{
		GraphImporterNode node;

		public override void Enable(bool fromInspector)
		{
			base.Enable(fromInspector);
			var field = new ObjectField("Mixture Graph");
			field.objectType = typeof(Texture);
			controlsContainer.Add(field);
			node = nodeTarget as GraphImporterNode;

			field.RegisterValueChangedCallback(x =>
			{
				var graph = MixtureDatabase.GetGraphFromTexture(x.newValue as Texture);
				if (graph != null)
				{
					node.graph = graph;
					var processor = new MixtureGraphProcessor(graph);
					processor.Run();
					node.UpdateAllPorts();
				}
				else
				{
					Debug.Log("Graph Not Found");
				}
			});
		}
	}
}