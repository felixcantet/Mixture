using System.Collections.Generic;
using System.Linq;
using GraphProcessor;
using UnityEditor;
using UnityEngine;

namespace Mixture
{
    [Documentation(@"To be filled.")]
    
    [System.Serializable, NodeMenuItem("Painting/Paint 3D")]
    public class Paint3DNode : MixtureNode
    {
        [Input(name = "In Mesh")] 
        public Mesh inMesh;

        [Input(name = "Reference Material")] 
        public Material refMat;
        
        [Input]
        public IEnumerable<Material> palette;
        
        [Output(name = "Out Material")]
        public Material outMaterial; // Je sais pas on out quoi par contre
        // Le mat du mesh ? 
        
        public override bool 	hasSettings => false;
        public override string	name => "Paint 3D";
        
        [CustomPortBehavior(nameof(palette))]
        IEnumerable< PortData > GetPortsForInputs(List< SerializableEdge > edges)
        {
            yield return new PortData{ displayName = "In ", displayType = typeof(Material), acceptMultipleEdges = true};
        }

        [CustomPortInput(nameof(palette), typeof(Material), allowCast = true)]
        public void GetInputs(List< SerializableEdge > edges)
        {
            palette = edges.Select(e => (Material)e.passThroughBuffer);
        }
        
        [CustomPortOutput(nameof(outMaterial), typeof(Material))]
        public void MaterialOutputHandler(List<SerializableEdge> edges)
        {
            outMaterial = refMat; //new Material(Shader.Find("Standard"));
        }
    }
}

