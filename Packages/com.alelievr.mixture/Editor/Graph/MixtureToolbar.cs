using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using GraphProcessor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using PopupWindow = UnityEditor.PopupWindow;

namespace Mixture
{
    using Status = UnityEngine.UIElements.DropdownMenuAction.Status;

    public class MixtureToolbar : ToolbarView
    {
        public MixtureToolbar(BaseGraphView graphView) : base(graphView)
        {
        }

        MixtureGraph graph => graphView.graph as MixtureGraph;
        new MixtureGraphView graphView => base.graphView as MixtureGraphView;
        private ToolbarButtonData shaderButtonData;

        public class Styles
        {
            public const string realtimePreviewToggleText = "Always Update";
            public const string processButtonText = "Process";
            public const string saveAllText = "Save";
            public const string parameterViewsText = "Parameters";

            public static GUIContent documentation =
                new GUIContent("Documentation", MixtureEditorUtils.documentationIcon);

            public static GUIContent bugReport = new GUIContent("Bug Report", MixtureEditorUtils.bugIcon);

            public static GUIContent featureRequest =
                new GUIContent("Feature Request", MixtureEditorUtils.featureRequestIcon);

            public static GUIContent improveMixture =
                new GUIContent("Improve Mixture", MixtureEditorUtils.featureRequestIcon);

            public static GUIContent discord = new GUIContent("Discord", MixtureEditorUtils.discordIcon);
            public static GUIContent focusText = new GUIContent("Fit View");
            public static GUIContent settingsIcon = new GUIContent(MixtureEditorUtils.settingsIcon24);

            public static GUIContent shaderSettings =
                new GUIContent("Shader Parameters", MixtureEditorUtils.settingsIcon);

            static GUIStyle _improveButtonStyle = null;

            public static GUIStyle improveButtonStyle => _improveButtonStyle == null
                ? _improveButtonStyle = new GUIStyle(GUI.skin.button) {alignment = TextAnchor.MiddleLeft}
                : _improveButtonStyle;
        }

        enum TextureType
        {
            [InspectorName("Type: 2D")] Type2D = OutputDimension.Texture2D,
            [InspectorName("Type: 3D")] Type3D = OutputDimension.Texture3D,
            [InspectorName("Type: Cubemap")] TypeCubemap = OutputDimension.CubeMap,
        }

        enum Resolution
        {
            [InspectorName("Size: 32")] Res32 = POTSize._32,
            [InspectorName("Size: 64")] Res64 = POTSize._64,
            [InspectorName("Size: 128")] Res128 = POTSize._128,
            [InspectorName("Size: 256")] Res256 = POTSize._256,
            [InspectorName("Size: 512")] Res512 = POTSize._512,
            [InspectorName("Size: 1024")] Res1024 = POTSize._1024,
            [InspectorName("Size: 2048")] Res2048 = POTSize._2048,
            [InspectorName("Size: 4096")] Res4096 = POTSize._4096,
            [InspectorName("Size: 8192")] Res8192 = POTSize._8192,
            [InspectorName("Custom")] Custom = POTSize.Custom,
        }

        public class ImproveMixturePopupWindow : PopupWindowContent
        {
            public static readonly int width = 150;

            public override Vector2 GetWindowSize()
            {
                return new Vector2(width, 124);
            }

            public override void OnGUI(Rect rect)
            {
                if (GUILayout.Button(Styles.documentation, Styles.improveButtonStyle))
                    Application.OpenURL(@"https://alelievr.github.io/Mixture/");
                if (GUILayout.Button(Styles.bugReport, Styles.improveButtonStyle))
                    Application.OpenURL(
                        @"https://github.com/alelievr/Mixture/issues/new?assignees=alelievr&labels=bug&template=bug_report.md&title=%5BBUG%5D");
                if (GUILayout.Button(Styles.featureRequest, Styles.improveButtonStyle))
                    Application.OpenURL(
                        @"https://github.com/alelievr/Mixture/issues/new?assignees=alelievr&labels=enhancement&template=feature_request.md&title=");
                if (GUILayout.Button(Styles.discord, Styles.improveButtonStyle))
                    Application.OpenURL(@"https://discord.gg/DGxZRP3qeg");
            }
        }

        protected class ShaderSelectorPopup : PopupWindowContent
        {
            public static readonly int width = 150;
            public MixtureGraph graph;
            public MixtureGraphView graphView;
            private ToolbarButtonData toolbarButton;

            public ShaderSelectorPopup(MixtureGraph graph, MixtureGraphView view, ToolbarButtonData toolbarButton)
            {
                this.graph = graph;
                this.graphView = view;
                this.toolbarButton = toolbarButton;
            }

            public override Vector2 GetWindowSize()
            {
                return new Vector2(width, 1);
            }

            public override void OnGUI(Rect rect)
            {
                ShaderSelectionDropdown selector = new ShaderSelectionDropdown(graph.outputMaterial.shader,
                    (object shaderName) =>
                    {
                        var shader = Shader.Find(shaderName as string);
                        if (shader != null)
                        {
                            graph.outputMaterial.shader = shader;
                            toolbarButton.content = new GUIContent(shader.name);
                        }

                        var view = graphView.nodeViews.FirstOrDefault(x => x is OutputNodeView) as OutputNodeView;
                        if (view != null)
                        {
                            Debug.Log("SHADER CHANGED");
                            view.OnShaderChange();
                            view.ForceUpdatePorts();
                        }
                        
                    });
                selector.Show(rect);

                void Close()
                {
                    
                }
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

            Color GetGUIColor(ShaderPropertyData data)
            {
                // Color : 0.33 0.8 1 1
                // Vector : 0.066 0.46 1 1
                // Float : 0.2 0.2 1 1
                switch (data.type)
                {
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:
                        return new Color(0.2f, 0.2f, 1 ,1);
                    
                    case ShaderPropertyType.Texture:
                        return new Color(1.0f, 0.6f, 0.066f, 1.0f);
                    
                    case ShaderPropertyType.Color:
                        return new Color(0.33f, 0.8f, 1.0f, 1.0f);
                    
                    case ShaderPropertyType.Vector:
                        return new Color(0.066f, 0.46f, 1f, 1f);
                    
                }

                return new Color(1.0f, 1.0f, 1.0f, 1.0f);
            }

            public override void OnGUI(Rect rect)
            {
                var needUpdate = false;
                GUILayout.Label("Shader Parameters", EditorStyles.boldLabel);
                scrollPos = GUILayout.BeginScrollView(
                    scrollPos); //, GUILayout.Width(100), GUILayout.Height(100));
                int propCount = graph.outputMaterial.shader.GetPropertyCount();

                // Build Property List
                if (graph.outputNode.enableParameters.Count != propCount)
                {
                    for (int i = 0; i < propCount; ++i)
                    {
                        graph.outputNode.enableParameters.Add(
                            new ShaderPropertyData(graph.outputMaterial.shader, i));
                    }
                }
                //foreach(item in )
                for (int i = 0; i < propCount; i++)
                {
                    Rect r = EditorGUILayout.GetControlRect(false, 0);

                    r.height = 1;
                    EditorGUI.DrawRect(r, new Color(0.5f, 0.5f, 0.5f, 1));
                    var prevValue = graph.outputNode.enableParameters[i].displayInOutput;

                    GUILayout.BeginHorizontal();
                    var color = GUI.contentColor;
                    GUI.contentColor = GetGUIColor(graph.outputNode.enableParameters[i]);
                    GUILayout.Label( Styles.improveMixture.image);
                    GUI.contentColor = color;
                    string label = graph.outputNode.enableParameters[i].description + " (" +
                                   graph.outputNode.enableParameters[i].name + ")";
                    if (label.Length > 50)
                        label = label.Substring(0, 50) + "...";
                    //GUILayout.Label(graph.outputNode.enableParameters[i].description + " (" + graph.outputNode.enableParameters[i].name + ")");
                    GUILayout.Label(label);
                    //GUILayout.Space(10);
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
                    graphView.nodeViews.FirstOrDefault(x => x is OutputNodeView).ForceUpdatePorts();
                }
            }
        }

        public class SettingsMixturePopupWindow : PopupWindowContent
        {
            public static readonly int width = 300;
            public int height = 240;

            MixtureGraphView graphView;

            public SettingsMixturePopupWindow(MixtureGraphView graphView)
            {
                this.graphView = graphView;
            }

            public override Vector2 GetWindowSize()
            {
                return new Vector2(width, height);
            }

            public override void OnClose()
            {
            }

            public override void OnGUI(Rect rect)
            {
            }

            public override void OnOpen()
            {
                var settingsView =
                    new MixtureSettingsView(graphView.graph.settings, graphView, "Graph Settings", false);
                settingsView.AddToClassList("RTSettingsView");
                settingsView.style.height = new StyleLength(new Length(100, LengthUnit.Percent));

                var otherHeader = new Label("Advanced Settings");
                otherHeader.AddToClassList(MixtureSettingsView.headerStyleClass);
                settingsView.Add(otherHeader);

                var defaultInheritanceMode = new EnumField(graphView.graph.defaultNodeInheritanceMode)
                {
                    label = "Node Inheritance Mode"
                };
                defaultInheritanceMode.RegisterValueChangedCallback(e =>
                {
                    graphView.RegisterCompleteObjectUndo("Changed node inheritance mode");
                    graphView.graph.defaultNodeInheritanceMode = (NodeInheritanceMode) e.newValue;

                    graphView.graph.UpdateNodeInheritanceMode();
                    graphView.RefreshNodeSettings();

                    graphView.ProcessGraph();
                });
                settingsView.Add(defaultInheritanceMode);

                editorWindow.rootVisualElement.Add(settingsView);
            }
        }

        protected override void AddButtons()
        {
            // Left buttons
            AddButton(Styles.processButtonText, Process, left: true);

            ToggleRealtime(graph.realtimePreview);
            AddToggle(Styles.realtimePreviewToggleText, graph.realtimePreview, ToggleRealtime, left: true);

            if (graph.type != MixtureGraphType.Realtime)
                AddButton(Styles.saveAllText, SaveAll);

            AddSeparator(5);

            AddButton("Show In Project", ShowInProject);

            AddSeparator(5);

            AddButton(Styles.focusText, () => graphView.FrameAll());

            // Right buttons

            AddCustom(DrawResolutionAndDimensionFields, left: false);

            AddFlexibleSpace(left: false);

            AddToggle(Styles.parameterViewsText, graph.isParameterViewOpen, ToggleParameterView, left: false);

            AddButton(Styles.settingsIcon, ShowSettingsWindow, left: false);

            AddDropDownButton(Styles.improveMixture, ShowImproveMixtureWindow, left: false);

            if (graph.type == MixtureGraphType.Material)
            {
                this.shaderButtonData = AddButton(new GUIContent(graph.outputMaterial.shader.name), ShowShaderPopup,
                    left: false);
                AddDropDownButton(Styles.settingsIcon, ShowShaderParametersWindow, left: false);
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

        void ShowInProject()
        {
            EditorGUIUtility.PingObject(graph.mainOutputAsset);
            ProjectWindowUtil.ShowCreatedAsset(graph.mainOutputAsset);
        }

        void DrawResolutionAndDimensionFields()
        {
            // Draw the resolution of the graph
            EditorGUI.BeginChangeCheck();
            if (graph.settings.potSize != POTSize.Custom)
            {
                var newPOTValue = (POTSize) EditorGUILayout.EnumPopup((Resolution) graph.settings.potSize,
                    EditorStyles.toolbarDropDown, GUILayout.Width(116));
                if (newPOTValue != POTSize.Custom)
                    graph.settings.SetPOTSize((int) newPOTValue);
                else
                    graph.settings.potSize = newPOTValue;
            }
            else
            {
                graph.settings.potSize = (POTSize) EditorGUILayout.EnumPopup((Resolution) graph.settings.potSize,
                    EditorStyles.toolbarDropDown, GUILayout.Width(116));
                graph.settings.width = EditorGUILayout.IntField(graph.settings.width, GUILayout.Width(50));
                EditorGUILayout.LabelField("x", GUILayout.Width(10));
                graph.settings.height = EditorGUILayout.IntField(graph.settings.height, GUILayout.Width(50));
                if (graph.settings.GetResolvedTextureDimension(graph) == TextureDimension.Tex3D)
                {
                    EditorGUILayout.LabelField("x", GUILayout.Width(10));
                    graph.settings.depth = EditorGUILayout.IntField(graph.settings.depth, GUILayout.Width(50));
                }
            }

            if (EditorGUI.EndChangeCheck())
                graphView.ProcessGraph();

            EditorGUI.BeginChangeCheck();

            var newDimension = (OutputDimension) EditorGUILayout.EnumPopup((TextureType) graph.settings.dimension,
                EditorStyles.toolbarDropDown, GUILayout.Width(114));
            if (EditorGUI.EndChangeCheck())
            {
                // When the dimension is updated, we need to update all the node ports in the graph
                if (graph.settings.dimension != newDimension)
                {
                    // We delay the port refresh to let the settings finish it's update 
                    schedule.Execute(() =>
                    {
                        {
                            // Refresh ports on all the nodes in the graph
                            foreach (var node in graph.nodes)
                                node.UpdateAllPortsLocal();
                        }
                    }).ExecuteLater(1);
                }

                graph.settings.dimension = newDimension;

                if (newDimension == OutputDimension.Texture3D)
                {
                    long pixelCount = graph.settings.GetResolvedWidth(graph) * graph.settings.GetResolvedHeight(graph) *
                                      graph.settings.GetResolvedDepth(graph);

                    // Above 16M pixels in a texture3D, processing can take too long and crash the GPU when a conversion happen
                    if (pixelCount > 16777216)
                        graph.settings.SetPOTSize(64);
                }

                graphView.ProcessGraph();
            }
        }

        void ShowImproveMixtureWindow()
        {
            var rect = EditorWindow.focusedWindow.position;
            rect.xMin = rect.width - ImproveMixturePopupWindow.width;
            rect.yMin = 21;
            rect.size = Vector2.zero;
            PopupWindow.Show(rect, new ImproveMixturePopupWindow());
        }

        void ShowShaderPopup()
        {
            var rect = EditorWindow.focusedWindow.position;

            rect.xMin = rect.width - ImproveMixturePopupWindow.width;
            rect.yMin = 21;
            rect.size = Vector2.zero;
            PopupWindow.Show(rect, new ShaderSelectorPopup(graph, graphView, shaderButtonData));
        }

        void ShowSettingsWindow()
        {
            var rect = EditorWindow.focusedWindow.position;
            rect.xMin = rect.width - SettingsMixturePopupWindow.width;
            rect.yMin = 0;
            rect.size = Vector2.zero;
            PopupWindow.Show(rect, new SettingsMixturePopupWindow(graphView));
        }

        void SaveAll()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Mixture", "Saving All...", 0.0f);

                graph.SaveAll();
                graph.UpdateLinkedVariants();

                List<ExternalOutputNode> externalOutputs = new List<ExternalOutputNode>();

                foreach (var node in graph.nodes)
                {
                    if (node is ExternalOutputNode && (node as ExternalOutputNode).asset != null)
                    {
                        externalOutputs.Add(node as ExternalOutputNode);
                    }
                }

                int i = 0;
                foreach (var node in externalOutputs)
                {
                    EditorUtility.DisplayProgressBar("Mixture", $"Saving {node.asset.name}...",
                        (float) i / externalOutputs.Count);
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