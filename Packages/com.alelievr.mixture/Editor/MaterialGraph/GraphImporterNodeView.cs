using UnityEngine.UIElements;
using GraphProcessor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
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
                    node.importedGraph = graph;
                    var processor = new MixtureGraphProcessor(graph);
                    processor.Run();

                    if (node.output == null)
                        node.output = new List<MaterialData>();
                    else
                        node.output.Clear();

                    foreach (var item in node.importedGraph.outputNode.outputTextureSettings)
                    {
                        node.output.Add(new MaterialData
                        {
                            Label = "No Label",
                            texture = item.inputTexture,
                            settings = node.importedGraph.outputNode.rtSettings,
                            Name = item.name
                        });
                    }
                    controlsContainer.Clear();
                    
                    var overrideParameterView = Resources.Load<VisualTreeAsset>("UI Blocks/MixtureVariantParameter");
                    var parameterContainer = new VisualElement();
                    parameterContainer.style.flexGrow = 1;
                    controlsContainer.Add(parameterContainer);
                    var propertyFactory = new ExposedParameterFieldFactory(node.importedGraph, node.importedGraph.exposedParameters);
                    var headerLabel = new Label("Exposed Parameters");
                    headerLabel.AddToClassList("Header");
                    parameterContainer.Add(headerLabel);
                    
                    foreach (var item in node.importedGraph.exposedParameters)
                    {
                        var prop = new VisualElement();
                        prop.AddToClassList("Indent");
                        prop.style.display = DisplayStyle.Flex;
                        var parameterView = overrideParameterView.CloneTree();
                        prop.Add(parameterView);

                        var parameterValueField = propertyFactory.GetParameterValueField(item, (newValue) => {
                            item.value = newValue;
                            var processor = new MixtureGraphProcessor(node.importedGraph);
                            processor.Run();
                        });
                        var paramContainer = parameterView.Q("Parameter");
                        paramContainer.Add(parameterValueField);
                        parameterContainer.Add(parameterValueField);
                    }


                    node.UpdateAllPorts();
                }
                else
                {
                    Debug.Log("Graph Not Found");
                }
            });
        }

        private void GenerateFields()
        {

        }
    }
}