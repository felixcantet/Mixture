using UnityEngine.UIElements;
using GraphProcessor;
using UnityEditor.UIElements;
using UnityEditor;
using UnityEngine;
using System;
namespace Mixture
{
	[NodeCustomEditor(typeof(PBRMaterialBlend))]
	public class MaterialBlendingNodeView : MixtureNodeView
	{
		PBRMaterialBlend		node;

		public override void Enable(bool fromInspector)
		{
			base.Enable(fromInspector);
			controlsContainer.Clear();
			node = nodeTarget as PBRMaterialBlend;
			if (node.MaterialA != null)
			{
				var dropDown = new DropdownMenu();
				var heightLabel = new Label("Height Channel");
				var d = new ToolbarMenu();
				VisualElement element = new VisualElement();
				element.style.flexDirection = FlexDirection.Row;
				element.style.flexGrow = 1;
				d.style.flexGrow = 1;
				element.Add(heightLabel);
				element.Add(d);
				contentContainer.Add(element);
				// heightLabel.style.flexDirection = FlexDirection.Row;
				// d.style.flexGrow = 1;
				// heightLabel.style.alignContent = Align.FlexEnd;
				// heightLabel.Add(d);
				// contentContainer.Add(heightLabel);
				// heightLabel.style.flexGrow = 1;
				foreach (var item in node.MaterialA.GetTexturePropertyNames())
				{
					d.menu.AppendAction(item, x =>
					{
						node.heightPropertyName = item;
						d.text = item;
					} );
				}
				
				var slider = new Slider(0.0f, 1.0f);
				slider.label = "Height Blend";
				slider.RegisterValueChangedCallback(x =>
				{
					node.HeightBlendValue = x.newValue;
				});
				contentContainer.Add(slider);

			}

			
		}
	}
}