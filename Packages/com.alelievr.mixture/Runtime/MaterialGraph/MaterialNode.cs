using System.Collections.Generic;
using GraphProcessor;
using UnityEngine;

namespace Mixture
{
    [Documentation(@"
Material constant value.
")]
    [System.Serializable, NodeMenuItem("Constants/Material")]
    public class MaterialNode : MixtureNode
    {
        [Input(name = "In Material")] 
        public Material inMaterial;
        
        [Output(name = "Out Material")]
        public Material outMaterial;

        public override bool 	hasSettings => false;
        public override string	name => "Material";

        [CustomPortOutput(nameof(outMaterial), typeof(Material))]
        public void MaterialOutputHandler(List<SerializableEdge> edges)
        {
            Debug.Log("HELLO !");
            foreach (var e in edges)
            {
                e.passThroughBuffer = inMaterial;
            }
        }
    }
}