using UnityEngine.UIElements;
using GraphProcessor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Linq;
namespace Mixture
{
	[NodeCustomEditor(typeof(MaterialImportNode))]
	public class MaterialImportNodeView : MixtureNodeView
	{
		MaterialImportNode node;

		public override void Enable(bool fromInspector)
		{
			base.Enable(fromInspector);

			node = nodeTarget as MaterialImportNode;
			var field = new ObjectField("Mixture Graph");
			field.objectType = typeof(Texture);
            controlsContainer.Add(field);
			var paramterPanel = new VisualElement();
			paramterPanel.style.flexGrow = 1;
			controlsContainer.Add(paramterPanel);

			field.RegisterValueChangedCallback(x => {
				var graph = MixtureDatabase.GetGraphFromTexture(x.newValue as Texture);
				var variant = MixtureVariant.CreateInstance<MixtureVariant>();
				variant.SetParent(graph);
				var graphParams = variant.GetAllParameters();
				paramterPanel.Clear();
				foreach(var item in graphParams) 
				{
					var type = item.GetValueType();
					if(type == typeof(int)) 
					{
						var intField = new IntegerField(item.name);
						paramterPanel.Add(intField);
					}
				}
				Debug.Log("Set Graph");
				//node.importGraph = graph;
				//Debug.Log(node.importGraph);
				//graph.outputTextures.Clear();
				//var variant = new MixtureVariant();
				//variant.parentGraph = graph;
				//MixtureAssetCallbacks.CreateMixtureVariant(graph, null);
				//var variant = graph.variants[graph.variants.Count - 1];
				node.UpdateAllPorts();
				//MixtureAssetCallbacks.CreateMixtureVariant(graph, variant);
				node.importGraph = variant;
				//variant.overrideParameters.Add(graphParams.First());
				variant.ProcessGraphWithOverrides();
				//node.GetPortsForOutput(node.GetAllEdges().ToList());
				node.InitializePorts();
				Debug.Log("Update Ports");
				variant.UpdateAllVariantTextures();
				node.UpdateAllPorts();
				//var processor = new MixtureGraphProcessor(graph);
				//processor.Run();
			});
		}



  //      protected override void DrawImGUIPreview(MixtureNode node, Rect previewRect, float currentSlice)
  //      {
		//	var outputNode = node as OutputNode;

		//	//switch (node.previewTexture.dimension)
		//	//{


		//	//	case TextureDimension.Tex2D:
		//	//		MixtureUtils.texture2DPreviewMaterial.SetTexture("_MainTex", node.previewTexture);
		//	//		MixtureUtils.texture2DPreviewMaterial.SetVector("_Size", new Vector4(node.previewTexture.width, node.previewTexture.height, 1, 1));
		//	//		MixtureUtils.texture2DPreviewMaterial.SetVector("_Channels", MixtureEditorUtils.GetChannelsMask(nodeTarget.previewMode));
		//	//		MixtureUtils.texture2DPreviewMaterial.SetFloat("_PreviewMip", nodeTarget.previewMip);
		//	//		MixtureUtils.texture2DPreviewMaterial.SetFloat("_EV100", nodeTarget.previewEV100);
		//	//		MixtureUtils.texture2DPreviewMaterial.SetFloat("_IsSRGB", outputNode != null && outputNode.mainOutput.sRGB ? 1 : 0);

		//	//		if (Event.current.type == EventType.Repaint)
		//	//			EditorGUI.DrawPreviewTexture(previewRect, node.previewTexture, MixtureUtils.texture2DPreviewMaterial, ScaleMode.ScaleToFit, 0, 0);
		//	//		break;
		//	//	case TextureDimension.Tex3D:
		//	//		MixtureUtils.texture3DPreviewMaterial.SetTexture("_Texture3D", node.previewTexture);
		//	//		MixtureUtils.texture3DPreviewMaterial.SetVector("_Channels", MixtureEditorUtils.GetChannelsMask(nodeTarget.previewMode));
		//	//		MixtureUtils.texture3DPreviewMaterial.SetFloat("_PreviewMip", nodeTarget.previewMip);
		//	//		MixtureUtils.texture3DPreviewMaterial.SetFloat("_Depth", currentSlice / nodeTarget.rtSettings.GetDepth(owner.graph));
		//	//		MixtureUtils.texture3DPreviewMaterial.SetFloat("_EV100", nodeTarget.previewEV100);
		//	//		MixtureUtils.texture3DPreviewMaterial.SetFloat("_IsSRGB", outputNode != null && outputNode.mainOutput.sRGB ? 1 : 0);

		//	//		if (Event.current.type == EventType.Repaint)
		//	//			EditorGUI.DrawPreviewTexture(previewRect, Texture2D.whiteTexture, MixtureUtils.texture3DPreviewMaterial, ScaleMode.ScaleToFit, 0, 0, ColorWriteMask.Red);
		//	//		break;
		//	//	case TextureDimension.Cube:
		//	//		MixtureUtils.textureCubePreviewMaterial.SetTexture("_Cubemap", node.previewTexture);
		//	//		MixtureUtils.textureCubePreviewMaterial.SetVector("_Channels", MixtureEditorUtils.GetChannelsMask(nodeTarget.previewMode));
		//	//		MixtureUtils.textureCubePreviewMaterial.SetFloat("_PreviewMip", nodeTarget.previewMip);
		//	//		MixtureUtils.textureCubePreviewMaterial.SetFloat("_EV100", nodeTarget.previewEV100);
		//	//		MixtureUtils.textureCubePreviewMaterial.SetFloat("_IsSRGB", outputNode != null && outputNode.mainOutput.sRGB ? 1 : 0);

		//	//		if (Event.current.type == EventType.Repaint)
		//	//			EditorGUI.DrawPreviewTexture(previewRect, Texture2D.whiteTexture, MixtureUtils.textureCubePreviewMaterial, ScaleMode.ScaleToFit, 0, 0);
		//	//		break;
		//	//	default:
		//	//		Debug.LogError(node.previewTexture + " is not a supported type for preview");
		//	//		break;
		//	//}
		//}
    }
}