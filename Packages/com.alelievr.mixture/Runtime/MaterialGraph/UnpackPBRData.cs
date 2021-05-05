using UnityEngine;
using GraphProcessor;
using UnityEngine.Rendering;
using System.Collections.Generic;
namespace Mixture
{
	[System.Serializable, NodeMenuItem("Custom/Unpack PBR Data")]
	public class UnpackPBRData : MixtureNode
	{
        [Input("PBR Structure")]
        public PBRStructure input;

        [Output]
        public List<Texture> output;

		public override string	name => "UnpackPBRData";



        [CustomPortBehavior(nameof(output))]
        IEnumerable<PortData> GetPortsForOutput(List<SerializableEdge> edges) 
        {
            yield return new PortData
            {
                displayName = "Albedo",
                displayType = typeof(Texture),
                identifier = "Albedo"
            };
            yield return new PortData
            {
                displayName = "Height",
                displayType = typeof(Texture),
                identifier = "Height"
            };
            yield return new PortData
            {
                displayName = "Normal",
                displayType = typeof(Texture),
                identifier = "Normal"
            };
            yield return new PortData
            {
                displayName = "Metallic",
                displayType = typeof(Texture),
                identifier = "Metalic"
            };
            yield return new PortData
            {
                displayName = "Occlusion",
                displayType = typeof(Texture),
                identifier = "Occlusion"
            };
            yield return new PortData
            {
                displayName = "Emissive",
                displayType = typeof(Texture),
                identifier = "Emissive"
            };
        }

        [CustomPortOutput(nameof(output), typeof(Texture))]
        public void PushOutputDatas(List<SerializableEdge> edges)
        {
            foreach(var item in edges) 
            {
                if(item.outputPortIdentifier == "Albedo")
                {
                    if (input.Albedo != null)
                        item.passThroughBuffer = input.Albedo;
                    else
                        item.passThroughBuffer = Texture2D.whiteTexture;
                }

                if (item.outputPortIdentifier == "Height")
                {
                    if (input.Albedo != null)
                        item.passThroughBuffer = input.Height;
                    else
                        item.passThroughBuffer = Texture2D.blackTexture;
                }

                if (item.outputPortIdentifier == "Normal")
                {
                    if (input.Albedo != null)
                        item.passThroughBuffer = input.Normal;
                    else
                        item.passThroughBuffer = Texture2D.normalTexture;
                }

                if (item.outputPortIdentifier == "Metallic")
                {
                    if (input.Albedo != null)
                        item.passThroughBuffer = input.Metallic;
                    else
                        item.passThroughBuffer = Texture2D.blackTexture;
                }

                if (item.outputPortIdentifier == "Occlusion")
                {
                    if (input.Albedo != null)
                        item.passThroughBuffer = input.Occlusion;
                    else
                        item.passThroughBuffer = Texture2D.whiteTexture;
                }

                if (item.outputPortIdentifier == "Emissive")
                {
                    if (input.Albedo != null)
                        item.passThroughBuffer = input.Emissive;
                    else
                        item.passThroughBuffer = Texture2D.whiteTexture;
                }
            }
        }
    }
}