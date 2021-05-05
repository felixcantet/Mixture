using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GraphProcessor;
namespace Mixture
{
    [System.Serializable, NodeMenuItem(menuTitle ="PBR Convertion")]
    public class PBR_CRIConversion : MixtureNode
    {
        public override string name => "CRI PBR Converter";

        [Input("Out Structure type")] public PBRStructureOut input;
        [Output("PBR Structure")] public PBRStructure output;

        [CustomPortOutput(nameof(output), typeof(PBRStructure))]
        void CustomOutputBehaviour(List<SerializableEdge> edges)
        {
            output = new PBRStructure(input);
            foreach(var item in edges)
            {
                item.passThroughBuffer = output;
            }
        }

    }
}
