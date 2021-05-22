using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using GraphProcessor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using PopupWindow = UnityEditor.PopupWindow;

namespace Mixture
{
	using Status = UnityEngine.UIElements.DropdownMenuAction.Status;

	public class MixtureToolbar : ToolbarView
	{
		public MixtureToolbar(BaseGraphView graphView) : base(graphView) {}

		MixtureGraph			graph => graphView.graph as MixtureGraph;
		new MixtureGraphView	graphView => base.graphView as MixtureGraphView;

		class Styles
		{
			public const string realtimePreviewToggleText = "Always Update";
			public const string processButtonText = "Process";
            public const string saveAllText = "Save All";
			public const string parameterViewsText = "Parameters";
			public static GUIContent documentation = new GUIContent("Documentation", MixtureEditorUtils.documentationIcon);
			public static GUIContent bugReport = new GUIContent("Bug Report", MixtureEditorUtils.bugIcon);
			public static GUIContent featureRequest = new GUIContent("Feature Request", MixtureEditorUtils.featureRequestIcon);
			public static GUIContent improveMixture = new GUIContent("Improve Mixture", MixtureEditorUtils.featureRequestIcon);
			public static GUIContent shaderSettings = new GUIContent("Shader Parameters", MixtureEditorUtils.settingsIcon);
			public static GUIContent focusText = new GUIContent("Fit View");
			static GUIStyle _improveButtonStyle = null;
			public static GUIStyle improveButtonStyle => _improveButtonStyle == null ? _improveButtonStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft } : _improveButtonStyle;
		}

		public class ImproveMixturePopupWindow : PopupWindowContent
		{
			public static readonly int width = 150;
			
			public override Vector2 GetWindowSize()
			{
				return new Vector2(width, 94);
			}

			public override void OnGUI(Rect rect)
			{
				if (GUILayout.Button(Styles.documentation, Styles.improveButtonStyle))
					Application.OpenURL(@"https://alelievr.github.io/Mixture/");
				if (GUILayout.Button(Styles.bugReport, Styles.improveButtonStyle))
					Application.OpenURL(@"https://github.com/alelievr/Mixture/issues/new?assignees=alelievr&labels=bug&template=bug_report.md&title=%5BBUG%5D");
				if (GUILayout.Button(Styles.featureRequest, Styles.improveButtonStyle))
					Application.OpenURL(@"https://github.com/alelievr/Mixture/issues/new?assignees=alelievr&labels=enhancement&template=feature_request.md&title=");
			}
		}

		public class ShaderParametersPopupWindow : PopupWindowContent
		{
			public static readonly int width = 400;
			private Vector2 scrollPos;
			
			public MixtureGraph graph;
			public MixtureGraphView graphView;
			public ShaderParametersPopupWindow(MixtureGraph graph, MixtureGraphView view)
			{
				this.graph = graph;
				this.graphView = view;
			}
			
			public override Vector2 GetWindowSize()
			{
				return new Vector2(width, 500);
			}

			public override void OnGUI(Rect rect)
			{
				var needUpdate = false;
				GUILayout.Label("Shader Parameters", EditorStyles.boldLabel);
				scrollPos = GUILayout.BeginScrollView(
					scrollPos);//, GUILayout.Width(100), GUILayout.Height(100));
				int propCount = graph.outputMaterial.shader.GetPropertyCount();

				// Build Property List
				if (graph.outputNode.enableParameters.Count != propCount)
				{
					for (int i = 0; i < propCount; ++i)
					{
						graph.outputNode.enableParameters.Add(
							new OutputNode.ShaderPropertyData(graph.outputMaterial.shader, i));
					}
				}
				
				for(int i = 0; i < propCount; i++)
				{
					Rect r = EditorGUILayout.GetControlRect(false, 0);

					r.height = 1;
					EditorGUI.DrawRect(r, new Color ( 0.5f,0.5f,0.5f, 1 ) );
					var prevValue = graph.outputNode.enableParameters[i].displayInOutput;

					GUILayout.BeginHorizontal();
					GUILayout.Label(graph.outputNode.enableParameters[i].description);
					GUILayout.Space(10);
					GUILayout.FlexibleSpace();
					// if (!graph.outputNode.enableParameters.ContainsKey(graph.outputMaterial.shader.GetPropertyName(i)))
					// {
					// 	graph.outputNode.enableParameters.Add(graph.outputMaterial.shader.GetPropertyName(i), false);
					// }
					var newValue = GUILayout.Toggle(graph.outputNode.enableParameters[i].displayInOutput, "");
					graph.outputNode.enableParameters[i].displayInOutput = newValue;
					if (prevValue != newValue)
						needUpdate = true;
					//graph.outputNode.enableParameters[graph.outputMaterial.shader.GetPropertyName(i)] = GUILayout.Toggle(graph.outputNode.enableParameters[graph.outputMaterial.shader.GetPropertyName(i)], "");
					GUILayout.EndHorizontal();
					GUILayout.Space(5);
				}
				
				GUILayout.EndScrollView();

				if (needUpdate)
				{
					graph.outputNode.BuildOutputFromShaderProperties();
					graphView.nodeViews.Where(x => x is OutputNodeView).FirstOrDefault().ForceUpdatePorts();
				}
					
			}
		}

		
        protected override void AddButtons()
		{
			// Add the hello world button on the left of the toolbar
			AddButton(Styles.processButtonText, Process, left: false);
			ToggleRealtime(graph.realtimePreview);
			AddToggle(Styles.realtimePreviewToggleText, graph.realtimePreview, ToggleRealtime, left: false);

			// bool exposedParamsVisible = graphView.GetPinnedElementStatus< ExposedParameterView >() != Status.Hidden;
			// For now we don't display the show parameters
			// AddToggle("Show Parameters", exposedParamsVisible, (v) => graphView.ToggleView<ExposedParameterView>());
			AddButton("Show In Project", () => {
				EditorGUIUtility.PingObject(graph.mainOutputAsset);
				ProjectWindowUtil.ShowCreatedAsset(graph.mainOutputAsset);
				// Selection.activeObject = graph;
			});
			AddToggle(Styles.parameterViewsText, graph.isParameterViewOpen, ToggleParameterView, left: true);
			AddButton(Styles.focusText, () => graphView.FrameAll(), left: true);

			if (graph.type != MixtureGraphType.Realtime)
				AddButton(Styles.saveAllText, SaveAll , left: false);
			// AddButton(Styles.bugReport, ReportBugCallback, left: false);
			AddDropDownButton(Styles.improveMixture, ShowImproveMixtureWindow, left: false);

			if (graph.type == MixtureGraphType.Material)
			{
				AddDropDownButton(Styles.shaderSettings, ShowShaderParametersWindow, left: false);
			}
		}

        void ShowShaderParametersWindow()
        {
	        var rect = EditorWindow.focusedWindow.position;
	        rect.xMin = rect.width - ShaderParametersPopupWindow.width;
	        rect.yMin = 21;
	        rect.size = Vector2.zero;
	        PopupWindow.Show(rect, new ShaderParametersPopupWindow(graph, graphView));
	        
        }
        
		void ShowImproveMixtureWindow()
		{
			var rect = EditorWindow.focusedWindow.position;
			// rect.position = Vector2.zero;
			rect.xMin = rect.width - ImproveMixturePopupWindow.width;
			rect.yMin = 21;
			rect.size = Vector2.zero;
			PopupWindow.Show(rect, new ImproveMixturePopupWindow());
		}

        void SaveAll()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Mixture", "Saving All...", 0.0f);

                graph.SaveAll();
				graph.UpdateLinkedVariants();

                List<ExternalOutputNode> externalOutputs = new List<ExternalOutputNode>();

                foreach(var node in graph.nodes)
                {
                    if(node is ExternalOutputNode && (node as ExternalOutputNode).asset != null)
                    {
                        externalOutputs.Add(node as ExternalOutputNode);
                    }
                }

                int i = 0;
                foreach(var node in externalOutputs)
                {
                    EditorUtility.DisplayProgressBar("Mixture", $"Saving {node.asset.name}...", (float)i/externalOutputs.Count);
                    (node as ExternalOutputNode).OnProcess();
                    graph.SaveExternalTexture((node as ExternalOutputNode), false);
                    i++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

		void ToggleRealtime(bool state)
		{
			if (state)
			{
				HideButton(Styles.processButtonText);
				MixtureUpdater.AddGraphToProcess(graphView);
			}
			else
			{
				ShowButton(Styles.processButtonText);
				MixtureUpdater.RemoveGraphToProcess(graphView);
			}
			graph.realtimePreview = state;
		}

		void ToggleParameterView(bool state)
		{
			graphView.ToggleView<MixtureParameterView>();
			graph.isParameterViewOpen = state;
		}

		void AddProcessButton()
		{
		}

		void Process()
		{
			EditorApplication.delayCall += graphView.processor.Run;
		}
	}
}