using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.UIElements;
using GraphProcessor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEditor;

namespace Mixture
{
    [NodeCustomEditor(typeof(ImportMixtureGraphNode))]
    public class ImportMixtureGraphNodeView : MixtureNodeView
    {
        ImportMixtureGraphNode node;
        private VisualElement parameterOverrideContainer;
        private MixtureGraph prevGraph;

        public override void Enable(bool fromInspector)
        {
            base.Enable(fromInspector);
            node = nodeTarget as ImportMixtureGraphNode;
            var graphPicker = controlsContainer.Q(null, "unity-object-field") as ObjectField;
            if (parameterOverrideContainer == null)
            {
                parameterOverrideContainer = new VisualElement();
                controlsContainer.Add(parameterOverrideContainer);
            }

//            parameterOverrideContainer.Clear();
            graphPicker.RegisterValueChangedCallback(x => { SetupOverrideParameterFields(); });

            if (node.importedGraph != null)
            {
                SetupOverrideParameterFields();
            }
            node.graph.NotifyNodeChanged(node);
        }


        void SetupOverrideParameterFields()
        {
            node.previewIndex = 0;
            node.LoadGraph();
            ForceUpdatePorts();


            var param = node.variant.GetAllParameters();

            prevGraph = node.importedGraph;
            this.parameterOverrideContainer.Clear();
            if (node.overrides == null)
                node.overrides = new List<ExposedParameter>();

            foreach (var item in param)
            {
                if (node.overrides.FirstOrDefault(x => x.name == item.name) == null)
                {
                    var clone = item.Clone();
                    node.overrides.Add(clone);
                    node.variant.overrideParameters.Add(clone);
                }
                
            }

            var parameters = node.importedGraph.exposedParameters;
            foreach (var item in node.overrides)
            {
                if (item.value == null || item.GetValueType() == null)
                    continue;
                if (item.GetValueType() == typeof(bool))
                {
                    var f = new Toggle(item.name);
                    f.name = item.name;
                    f.value = (bool) item.value;
                    f.RegisterValueChangedCallback(x =>
                    {
                        node.overrides.FirstOrDefault(y => y.name == item.name).value = x.newValue;
                        node.graph.NotifyNodeChanged(node);
                    });
                    parameterOverrideContainer.Add(f);
                }

                else if (item.GetValueType() == typeof(Color))
                {
                    var f = new ColorField(item.name);
                    f.name = item.name;
                    f.value = (Color) item.value;
                    f.RegisterValueChangedCallback(x =>
                    {
                        node.overrides.FirstOrDefault(y => y.name == item.name).value = x.newValue;
                        node.graph.NotifyNodeChanged(node);
                    });
                    parameterOverrideContainer.Add(f);
                }
                else if (item.GetValueType() == typeof(int))
                {
                    var f = new IntegerField(item.name);
                    f.name = item.name;
                    f.value = (int) item.value;
                    f.RegisterValueChangedCallback(x =>
                    {
                        node.overrides.FirstOrDefault(y => y.name == item.name).value = x.newValue;
                        node.graph.NotifyNodeChanged(node);
                    });
                    parameterOverrideContainer.Add(f);
                }
                else if (item.GetValueType() == typeof(Vector2))
                {
                    var f = new Vector2Field(item.name);
                    f.name = item.name;
                    f.value = (Vector2) item.value;
                    f.RegisterValueChangedCallback(x =>
                    {
                        node.overrides.FirstOrDefault(y => y.name == item.name).value = x.newValue;
                        node.graph.NotifyNodeChanged(node);
                    });
                    parameterOverrideContainer.Add(f);
                }
                else if (item.GetValueType() == typeof(Vector3))
                {
                    var f = new Vector3Field(item.name);
                    f.name = item.name;
                    f.value = (Vector3) item.value;
                    f.RegisterValueChangedCallback(x =>
                    {
                        node.overrides.FirstOrDefault(y => y.name == item.name).value = x.newValue;
                        node.graph.NotifyNodeChanged(node);
                    });
                    parameterOverrideContainer.Add(f);
                }
                else if (item.GetValueType() == typeof(Vector4))
                {
                    var f = new Vector4Field(item.name);
                    f.name = item.name;
                    f.value = (Vector3) item.value;
                    f.RegisterValueChangedCallback(x =>
                    {
                        node.overrides.FirstOrDefault(y => y.name == item.name).value = x.newValue;
                        node.graph.NotifyNodeChanged(node);
                    });
                    parameterOverrideContainer.Add(f);
                }
                else if (item.GetValueType() == typeof(Texture))
                {
                    var f = new ObjectField(item.name);
                    f.name = item.name;
                    f.objectType = typeof(Texture);
                    f.value = item.value as Texture;
                    f.RegisterValueChangedCallback(x =>
                    {
                        node.overrides.FirstOrDefault(y => y.name == item.name).value = x.newValue;
                        node.graph.NotifyNodeChanged(node);
                    });
                    parameterOverrideContainer.Add(f);
                }
                else if (item.GetValueType() == typeof(RenderTexture))
                {
                    var f = new ObjectField(item.name);
                    f.name = item.name;
                    f.objectType = typeof(RenderTexture);
                    f.value = item.value as RenderTexture;
                    f.RegisterValueChangedCallback(x =>
                    {
                        node.overrides.FirstOrDefault(y => y.name == item.name).value = x.newValue;
                        node.graph.NotifyNodeChanged(node);
                    });
                    parameterOverrideContainer.Add(f);
                }
                else if (item.GetValueType() == typeof(Gradient))
                {
                    var f = new GradientField(item.name);
                    f.name = item.name;
                    f.value = item.value as Gradient;
                    f.RegisterValueChangedCallback(x =>
                    {
                        node.overrides.FirstOrDefault(y => y.name == item.name).value = x.newValue;
                        node.graph.NotifyNodeChanged(node);
                    });
                    parameterOverrideContainer.Add(f);
                }
                else if (item.GetValueType() == typeof(MixtureMesh))
                {
                    var f = new ObjectField(item.name);
                    f.name = item.name;
                    f.objectType = typeof(MixtureMesh);
                    f.RegisterValueChangedCallback(x =>
                    {
                        node.overrides.FirstOrDefault(y => y.name == item.name).value = x.newValue;
                        node.graph.NotifyNodeChanged(node);
                    });
                    parameterOverrideContainer.Add(f);
                }
                else if (item.GetValueType() == typeof(Material))
                {
                    var f = new ObjectField(item.name);
                    f.name = item.name;
                    f.objectType = typeof(Material);
                    f.value = item.value as Material;
                    f.RegisterValueChangedCallback(x =>
                    {
                        node.overrides.FirstOrDefault(y => y.name == item.name).value = x.newValue;
                        node.graph.NotifyNodeChanged(node);
                    });
                    parameterOverrideContainer.Add(f);
                }
            }
        }


        protected override void DrawDefaultInspector(bool fromInspector = false)
        {
            //fromInspector = true;


            if (node == null)
                node = nodeTarget as ImportMixtureGraphNode;
            base.DrawDefaultInspector(fromInspector);
            if (node.importedGraph == null)
                return;


            //controlsContainer.Add(field);
            //AddControlField(field.name);
        }

        protected override void DrawPreviewSettings(Texture texture)
        {
            if (node.importedGraph == null)
                return;
            using (new GUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(12)))
            {
                if (GUILayout.Button("<"))
                {
                    if (node.readBack == null)
                        return;
                    this.node.previewIndex--;
                    this.node.previewIndex = this.node.previewIndex % node.readBack.Count;
                }

                GUILayout.Label(this.node.previewIndex.ToString());
                if (GUILayout.Button(">"))
                {
                    if (node.readBack == null)
                        return;
                    this.node.previewIndex++;
                    this.node.previewIndex = this.node.previewIndex % node.readBack.Count;
                }
            }

            base.DrawPreviewSettings(texture);
        }
    }
}