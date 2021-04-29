using UnityEngine;
using GraphProcessor;
using UnityEngine.Rendering;
using System.Collections.Generic;
namespace Mixture
{
    [System.Serializable, NodeMenuItem("Custom/Graph Importer Node")]
    public class GraphImporterNode : MixtureNode
    {
        [Output]
        public List<Texture> output;

        public override string name => "Graph Importer Node";
        public MixtureGraph graph;
        //public override Texture previewTexture => output;

        [CustomPortBehavior(nameof(output))]
        public IEnumerable<PortData> GetPortsForOutput(List<SerializableEdge> edges)
        {
            if (graph == null)
                yield break;
            yield return new PortData
            {
                acceptMultipleEdges = true,
                displayName = "All Textures",
                displayType = typeof(List<Texture>),
                identifier = "Output"
            };
            for (int i = 0; i < graph.outputNode.outputTextureSettings.Count; i++)
            {
                var outputTexture = graph.outputNode.outputTextureSettings[i];
                yield return new PortData
                {
                    acceptMultipleEdges = true,
                    displayName = outputTexture.name,
                    displayType = TextureUtils.GetTypeFromDimension(graph.outputNode.rtSettings.GetTextureDimension(graph)),
                    identifier = i.ToString()
                };
            }
        }

        [CustomPortOutput(nameof(output), typeof(Texture))]
        void PushOutputs(List<SerializableEdge> connectedEdges)
        {
            int i = 0;
            foreach (var edge in connectedEdges)
            {
                if (edge.outputPortIdentifier == "Output")
                {
                    edge.passThroughBuffer = output;
                }
                else
                {
                    int value = int.Parse(edge.outputPortIdentifier);
                    edge.passThroughBuffer = graph.outputTextures[value];
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