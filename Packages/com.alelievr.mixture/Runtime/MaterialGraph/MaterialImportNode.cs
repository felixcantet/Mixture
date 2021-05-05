using UnityEngine;
using GraphProcessor;
using UnityEngine.Rendering;
using System.Collections.Generic;
namespace Mixture
{
    [System.Serializable, NodeMenuItem("Custom/MaterialImportNode")]
    public class MaterialImportNode : MixtureNode
    {
        [Input]
        public Texture input;

        [Output]
        public List<Texture> output;
        [ShowInInspector]
        public Texture mixtureTexture;
        public MixtureVariant importGraph;

        public override bool showDefaultInspector => true;
        public override string name => "MaterialImportNode";

        //public override Texture previewTexture => output.GetEnumerator().Current;

        protected override void Enable()
        {
            //foreach(var item in output)
            //	UpdateTempRenderTexture(ref item);
        }

        [CustomPortBehavior(nameof(output))]
        public IEnumerable<PortData> GetPortsForOutput(List<SerializableEdge> edges)
        {
            if (importGraph == null)
            {
                Debug.Log("No Graph");
                yield break;

            }
            Debug.Log(importGraph.outputTextures.Count);
            //yield return null;
            foreach (var item in importGraph.outputTextures)
            {
                Debug.Log(item);
                //var settings = importGraph.outputNode.outputTextureSettings;
                yield return new PortData { acceptMultipleEdges = true, displayName = item.name, displayType = TextureUtils.GetTypeFromDimension(TextureDimension.Tex2D), identifier = importGraph.outputTextures.IndexOf(item).ToString() };
            }
            //yield return new PortData { displayName = "Out 0", displayType = typeof(Texture), identifier = "0" };
            //yield return new PortData { displayName = "Out 1", displayType = typeof(Texture), identifier = "1" };
            //yield return new PortData { displayName = "Out 2", displayType = typeof(Texture), identifier = "2" };
            //yield return new PortData { displayName = "Out 3", displayType = typeof(Texture), identifier = "3" };
            //yield return new PortData { displayName = "Out 4", displayType = typeof(Texture), identifier = "4" };
            //yield return new PortData { displayName = "Out 5", displayType = typeof(Texture), identifier = "5" };
            //yield return new PortData { displayName = "Out 6", displayType = typeof(Texture), identifier = "6" };
            //yield return new PortData { displayName = "Out 7", displayType = typeof(Texture), identifier = "7" };
        }
        

        [CustomPortOutput(nameof(output), typeof(Texture))]
        void PushOutputs(List<SerializableEdge> connectedEdges)
        {
            
            //var processor = new MixtureGraphProcessor(this.importGraph);
            //processor.Run();
            int i = 0;
            foreach (var edge in connectedEdges)
            {
                int value = int.Parse(edge.outputPortIdentifier);
                edge.passThroughBuffer = importGraph.outputTextures[value];
                i++;
            }
            //if (this.importGraph == null)
            //    return;
            //Debug.Log($"There is {connectedEdges.Count} connected Output");
            //foreach(var edge in connectedEdges)
            //{
            //    foreach (var item in this.importGraph.outputNode.outputTextureSettings)
            //    {
            //        Debug.Log($"Edge name = {edge.inputFieldName} \n Texture name = {item.name}");
            //        if(edge.outputFieldName == item.name)
            //        {
            //            edge.passThroughBuffer = TextureUtils.GetBlackTexture(TextureDimension.Tex2D);
            //        }
            //    }
            //}
        }

        //protected override bool ProcessNode(CommandBuffer cmd)
        //{
        //          if (!base.ProcessNode(cmd))
        //              return false;

        //          // Update temp target in case settings changes
        //	//UpdateTempRenderTexture(ref output);

        //          // Insert your code here 

        //	return true;
        //}

        protected override void Disable()
        {
            base.Disable();
        }

    }
}