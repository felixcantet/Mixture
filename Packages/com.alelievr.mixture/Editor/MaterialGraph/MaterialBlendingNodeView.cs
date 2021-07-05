using UnityEngine.UIElements;
using GraphProcessor;
using UnityEditor.UIElements;
using UnityEngine;

namespace Mixture
{
	[NodeCustomEditor(typeof(MaterialBlending))]
	public class MaterialBlendingNodeView : BaseMixtureMaterialNodeView
	{
		MaterialBlending		node;

		public override void Enable(bool fromInspector)
		{
			base.Enable(fromInspector);

			node = nodeTarget as MaterialBlending;

			var blendMode = controlsContainer.Q(nameof(node.blendMode)) as EnumField;
			blendMode.RegisterValueChangedCallback(x =>
			{
				ForceUpdatePorts();
				node.GenerateBlendMaterials();
				UpdateInspector();
			});
			
			var useThreshold = controlsContainer.Q(nameof(node.useThreshold)) as Toggle;
			useThreshold.RegisterValueChangedCallback(x =>
			{
				ForceUpdatePorts();
				node.GenerateBlendMaterials();
				UpdateInspector();
			});

			UpdateInspector();
			// onPortConnected += view =>
			// {
			// 	Debug.Log("Generate Ports");
			// 	node.GenerateBlendMaterials();
			// };
			// onPortDisconnected += view => node.GenerateBlendMaterials();
		}

		void UpdateInspector()
		{
			if (node.blendMode == MaterialBlending.BlendMode.Height)
			{
				var useThreshold = contentContainer.Q(nameof(node.useThreshold)) as Toggle;
				useThreshold.SetEnabled(true);

				if (node.useThreshold)
				{
					var threshold = controlsContainer.Q(nameof(node.threshold)) as FloatField;
					
					threshold.SetEnabled(true);
					
					var blendAmount = controlsContainer.Q(nameof(node.blendAmount)) as FloatField;
					blendAmount.SetEnabled(false);
				}
				else
				{
					var threshold = controlsContainer.Q(nameof(node.threshold)) as FloatField;
					threshold.SetEnabled(false);
					
					var blendAmount = controlsContainer.Q(nameof(node.blendAmount)) as FloatField;
					blendAmount.SetEnabled(true);
				}
			}
			else
			{
				var useThreshold = contentContainer.Q(nameof(node.useThreshold)) as Toggle;
				useThreshold.SetEnabled(false);
				
				var threshold = controlsContainer.Q(nameof(node.threshold)) as FloatField;
				threshold.SetEnabled(false);
					
				var blendAmount = controlsContainer.Q(nameof(node.blendAmount)) as FloatField;
				blendAmount.SetEnabled(false);
				
			}
		}
		
	}
}