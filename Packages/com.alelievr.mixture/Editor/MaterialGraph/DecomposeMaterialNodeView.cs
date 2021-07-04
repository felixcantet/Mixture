using System.Linq;
using UnityEngine.UIElements;
using GraphProcessor;
using UnityEngine;

namespace Mixture
{
	[NodeCustomEditor(typeof(DecomposeMaterialNode))]
	public class DecomposeMaterialNodeView : BaseMixtureMaterialNodeView
	{
		DecomposeMaterialNode		node;

		public override void Enable(bool fromInspector)
		{
			base.Enable(fromInspector);

			node = nodeTarget as DecomposeMaterialNode;

			onPortConnected += view =>
			{
				//node.output = node.input;
				controlsContainer.schedule.Execute(() => { ForceUpdatePorts(); Debug.Log("Update"); }).ExecuteLater(10);
				
			};
			onPortDisconnected += view =>
			{
				//node.output = node.input;
				controlsContainer.schedule.Execute(() => { ForceUpdatePorts(); Debug.Log("Update"); }).ExecuteLater(10);
				
			};
			
		}

		protected override void DrawDefaultInspector(bool fromInspector = false)
		{
			base.DrawDefaultInspector(fromInspector);
			ForceUpdatePorts();
		}
	}
}