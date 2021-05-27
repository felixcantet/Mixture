using UnityEngine;
using GraphProcessor;
using UnityEngine.Rendering;
using System.Collections.Generic;
namespace Mixture
{
    public class MaterialData
    {
        public Texture texture;
        public string Label;
        public MixtureSettings settings;
        public string Name;

    }

    [System.Serializable, NodeMenuItem("Custom/Graph Importer Node")]
    public class GraphImporterNode : MixtureNode
    {
        [Output]
        public List<MaterialData> output;

        public override string name => "Graph Importer Node";
        public MixtureGraph importedGraph;
        //public override Texture previewTexture => output;
        public override bool showDefaultInspector => false;
        [CustomPortBehavior(nameof(output))]
        public IEnumerable<PortData> GetPortsForOutput(List<SerializableEdge> edges)
        {
            if (importedGraph == null)
                yield break;
            yield return new PortData
            {
                acceptMultipleEdges = true,
                displayName = "All Textures",
                displayType = typeof(List<MaterialData>),
                identifier = "Output"
            };
            for (int i = 0; i < output.Count; i++)
            {
                var outputTexture = output[i];
                yield return new PortData
                {
                    acceptMultipleEdges = true,
                    displayName = outputTexture.Name,
                    displayType = TextureUtils.GetTypeFromDimension(outputTexture.settings.GetResolvedTextureDimension(importedGraph)),
                    identifier = i.ToString()
                };
            }
            //for (int i = 0; i < importerGraph.outputNode.outputTextureSettings.Count; i++)
            //{
            //    var outputTexture = importerGraph.outputNode.outputTextureSettings[i];
            //    yield return new PortData
            //    {
            //        acceptMultipleEdges = true,
            //        displayName = outputTexture.name,
            //        displayType = TextureUtils.GetTypeFromDimension(importerGraph.outputNode.rtSettings.GetTextureDimension(importerGraph)),
            //        identifier = i.ToString()
            //    };
            //}
        }

        [CustomPortOutput(nameof(output), typeof(Texture))]
        void PushOutputs(List<SerializableEdge> connectedEdges)
        {
            int i = 0;
            foreach (var edge in connectedEdges)
            {
                if (edge.outputPortIdentifier == "Output")
                {
                    Debug.Log($"Type = {edge.inputPort.portData.displayType.Name}");
                    edge.passThroughBuffer = output;
                }
                else
                {
                    int value = int.Parse(edge.outputPortIdentifier);
                    edge.passThroughBuffer = output[value].texture;
                    i++;
                }
            }
        }

        protected override void Disable()
        {
            base.Disable();
        }
    }
}