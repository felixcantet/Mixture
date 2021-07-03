using UnityEngine.UIElements;
using GraphProcessor;

namespace Mixture
{
	[NodeCustomEditor(typeof(ModifyMaterialParameterNode))]
	public class ModifyMateriarPropertyNodeView : BaseMixtureMaterialNodeView
	{
		ModifyMaterialParameterNode		node;

		// public override void OnCreated()
		// {
		// 	base.OnCreated();
		// 	
		// }

		public override void Enable(bool fromInspector)
		{
			base.Enable(fromInspector);

			node = nodeTarget as ModifyMaterialParameterNode;
			
			onPortConnected += view =>
			{
				controlsContainer.schedule.Execute(() => { ForceUpdatePorts(); }).ExecuteLater(10);
			};
			onPortDisconnected += view =>
			{
				controlsContainer.schedule.Execute(() => { ForceUpdatePorts(); }).ExecuteLater(10);
			};

			// var edges = node.GetAllEdges();
			//
			// ForceUpdatePorts();
			
		}
	}
}