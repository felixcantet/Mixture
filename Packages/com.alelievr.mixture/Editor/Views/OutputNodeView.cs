using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using GraphProcessor;
using UnityEngine.Rendering;
using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;

namespace Mixture
{
    [NodeCustomEditor(typeof(OutputNode))]
    public class OutputNodeView : MixtureNodeView
    {
        protected OutputNode outputNode;
        protected MixtureGraph graph;

        protected Dictionary<string, OutputTextureView> inputPortElements = new Dictionary<string, OutputTextureView>();

        private MaterialEditor editorPreview;

        public Action OnShaderChange;

        public override void Enable(bool fromInspector)
        {
            capabilities &= ~Capabilities.Deletable;
            outputNode = nodeTarget as OutputNode;
            graph = owner.graph as MixtureGraph;

            BuildOutputNodeSettings();

            if (graph.type == MixtureGraphType.Material)
            {
                Debug.Log("Register Events");
                
                OnShaderChange += () =>
                {
                    //owner.RemoveEdges();
                    
                   for(int i = 0; i < inputPortElements.Values.Count; i++)// item in inputPortElements.Values)
                    {
                        var port = inputPortElements.Values.ElementAt(i);
                        for(int j = 0; j < port.portView.GetEdges().Count; j++)// var edge in port.portView.GetEdges())
                        {
                            var edge = port.portView.GetEdges()[j];
                            owner.DisconnectView(edge);
                        }
                    }
                };
                OnShaderChange += () =>
                {
                    for(int i = 0; i < outputNode.outputTextureSettings.Count; i++)
                    {
                        outputNode.RemoveTextureOutput(outputNode.outputTextureSettings[i]);
                    }
                    outputNode.outputTextureSettings.Clear();
                };
                // OnShaderChange += UpdatePortView;
                // OnShaderChange += RefreshOutputPortSettings;
                OnShaderChange += outputNode.UpdatePropertyList;
                OnShaderChange += outputNode.BuildOutputFromShaderProperties;
                OnShaderChange += ForceUpdatePorts;
                OnShaderChange += () => { outputNode.SetMaterialPropertiesFromEdges(outputNode.GetAllEdges().ToList(), graph.outputMaterial); };
            }

            base.Enable(fromInspector);

            if (!fromInspector)
            {
                // We don't need the code for removing the material because this node can't be removed
                foreach (var output in outputNode.outputTextureSettings)
                {
                    if (output.finalCopyMaterial != null && !owner.graph.IsObjectInGraph(output.finalCopyMaterial))
                    {
                        // Check if the material we have is ours
                        if (owner.graph.IsExternalSubAsset(output.finalCopyMaterial))
                        {
                            output.finalCopyMaterial = new Material(output.finalCopyMaterial);
                            output.finalCopyMaterial.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
                        }

                        owner.graph.AddObjectToGraph(output.finalCopyMaterial);
                    }
                }

                outputNode.onTempRenderTextureUpdated += () =>
                {
                    RefreshOutputPortSettings();
                    UpdatePreviewImage();
                };
                graph.onOutputTextureUpdated += UpdatePreviewImage;

                // Clear the input when disconnecting it:
                onPortDisconnected += port =>
                {
                    var outputSlot = outputNode.outputTextureSettings.Find(o => o.name == port.portData.identifier);
                    if (outputSlot != null)
                        outputSlot.inputTexture = null;
                };

                InitializeDebug();
            }
        }


        void RefreshOutputPortSettings()
        {
            if (graph.type != MixtureGraphType.Material || true)
            {
                foreach (var view in inputPortElements.Values)
                    view.RefreshSettings();
            }
            else
            {
            }
        }

        void UpdatePortView()
        {
            foreach (var output in outputNode.outputTextureSettings)
            {
                var portView = GetPortViewFromFieldName(nameof(outputNode.outputTextureSettings), output.name);

                if (portView == null)
                    continue;

                if (!inputPortElements.ContainsKey(output.name))
                {
                    inputPortElements[output.name] = new OutputTextureView(owner, this, output);
                    inputContainer.Add(inputPortElements[output.name]);
                }

                inputPortElements[output.name].MovePort(portView);
            }

            // Remove unused output texture views
            foreach (var name in inputPortElements.Keys.ToList())
            {
                if (!outputNode.outputTextureSettings.Any(o => o.name == name))
                {
                    inputPortElements[name].RemoveFromHierarchy();
                    inputPortElements.Remove(name);
                }
            }
        }

        public override bool RefreshPorts()
        {
            bool result = base.RefreshPorts();
            UpdatePortView();
            return result;
        }

        protected override VisualElement CreateSettingsView()
        {
            var sv = base.CreateSettingsView();

            OutputDimension currentDim = nodeTarget.settings.dimension;
            settingsView.RegisterChangedCallback(() =>
            {
                // Reflect the changes on the graph output texture but not on the asset to avoid stalls.
                graph.UpdateOutputTextures();
                RefreshOutputPortSettings();

                // When the dimension is updated, we need to update all the node ports in the graph
                var newDim = nodeTarget.settings.dimension;
                if (currentDim != newDim)
                {
                    // We delay the port refresh to let the settings finish it's update 
                    schedule.Execute(() =>
                    {
                        owner.ProcessGraph();
                        // Refresh ports on all the nodes in the graph
                        foreach (var nodeView in owner.nodeViews)
                        {
                            nodeView.nodeTarget.UpdateAllPortsLocal();
                            nodeView.RefreshPorts();
                        }
                    }).ExecuteLater(1);
                    currentDim = newDim;
                    NodeProvider.LoadGraph(graph);
                }
                else
                {
                    schedule.Execute(() => { owner.ProcessGraph(); }).ExecuteLater(1);
                }
            });

            return sv;
        }

        protected virtual void BuildOutputNodeSettings()
        {
            controlsContainer.Add(new Button(() =>
            {
                var addOutputMenu = new GenericMenu();
                addOutputMenu.AddItem(new GUIContent("Color"), false,
                    () => AddOutputPreset(OutputTextureSettings.Preset.Color));
                addOutputMenu.AddItem(new GUIContent("Normal"), false,
                    () => AddOutputPreset(OutputTextureSettings.Preset.Normal));
                addOutputMenu.AddItem(new GUIContent("Height"), false,
                    () => AddOutputPreset(OutputTextureSettings.Preset.Height));
                addOutputMenu.AddItem(new GUIContent("Mask (HDRP)"), false,
                    () => AddOutputPreset(OutputTextureSettings.Preset.MaskHDRP));
                addOutputMenu.AddItem(new GUIContent("Detail (HDRP)"), false,
                    () => AddOutputPreset(OutputTextureSettings.Preset.DetailHDRP));
                addOutputMenu.AddItem(new GUIContent("Raw (not compressed)"), false,
                    () => AddOutputPreset(OutputTextureSettings.Preset.Raw));
                // addOutputMenu.AddItem(new GUIContent("Detail (URP)"), false, () => AddOutputPreset(OutputTextureSettings.Preset.DetailURP));
                addOutputMenu.ShowAsContext();

                void AddOutputPreset(OutputTextureSettings.Preset preset)
                {
                    outputNode.AddTextureOutput(preset);
                    ForceUpdatePorts();
                }
            }) {text = "Add Output"});

            if (graph.type != MixtureGraphType.Realtime)
            {
                controlsContainer.Add(new Button(SaveAllTextures)
                {
                    text = "Save All"
                });
            }
        }

        void SaveAllTextures()
        {
            graph.SaveAll();
            graph.UpdateLinkedVariants();
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            string mode = graph.type == MixtureGraphType.Realtime ? "Static" : "Realtime";
            evt.menu.AppendSeparator();
            evt.menu.AppendAction($"Convert Mixture to {mode}", _ => MixtureEditorUtils.ToggleMixtureGraphMode(graph),
                DropdownMenuAction.AlwaysEnabled);
        }

        void InitializeDebug()
        {
            var crtList = new VisualElement();

            outputNode.onProcessed += UpdateCRTList;

            void UpdateCRTList()
            {
                if (crtList.childCount == outputNode.outputTextureSettings.Count)
                    return;

                crtList.Clear();

                foreach (var output in outputNode.outputTextureSettings)
                {
                    crtList.Add(new ObjectField(output.name)
                        {objectType = typeof(CustomRenderTexture), value = output.finalCopyRT});
                }
            }

            debugContainer.Add(crtList);
        }

        void UpdatePreviewImage() => CreateTexturePreview(previewContainer, outputNode);

        protected override void DrawImGUIPreview(MixtureNode node, Rect previewRect, float currentSlice)
        {
            if (graph.type != MixtureGraphType.Material)
                base.DrawImGUIPreview(node, previewRect, currentSlice);
            else
            {
                if (editorPreview == null)
                    editorPreview = MaterialEditor.CreateEditor(this.graph.outputMaterial) as MaterialEditor;
                //editor.DrawPreview(previewRect);
                editorPreview.PropertiesChanged();
                editorPreview.OnInteractivePreviewGUI(previewRect, GUIStyle.none);
            }
        }

        public override void Disable()
        {
            base.Disable();
            if (this.graph.type == MixtureGraphType.Material)
            {
                // OnShaderChange -= UpdatePortView;
                // OnShaderChange -= RefreshOutputPortSettings;
                OnShaderChange -= outputNode.UpdatePropertyList;
                OnShaderChange -= outputNode.BuildOutputFromShaderProperties;
                OnShaderChange -= ForceUpdatePorts;
                
            }
        }
    }
}