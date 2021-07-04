using UnityEngine;
using GraphProcessor;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


namespace Mixture
{
    [System.Serializable, NodeMenuItem("Material/Decompose Material")]
    public class DecomposeMaterialNode : BaseMaterialNode
    {
        [Input] public MixtureMaterial input;
        [Output] public MixtureMaterial output;
        [Output] public List<object> parameters;
        [SerializeField] [HideInInspector] private Material baseMaterial;
        public override bool needPropertySelector => true;


        public override MixtureMaterial targetPropertySelector
        {
            get
            {
                if (output != null)
                    return output;
                return null;
            }
        }

        public override string name => "Decompose Material";

        public override Material previewMaterial
        {
            get
            {
                if (output != null)
                    return output.material;
                return null;
            }
        }


        protected override void Enable()
        {
        }


        protected override bool ProcessNode(CommandBuffer cmd)
        {
            if (!base.ProcessNode(cmd))
                return false;
            if (input == null || input.material == null)
                return false;
            if (output == null)
            {
                output = new MixtureMaterial(new Material(input.material));
                baseMaterial = input.material;
            }
            else if (baseMaterial != input.material)
            {
                output = new MixtureMaterial(new Material(input.material));
                baseMaterial = input.material;
            }
            // Insert your code here 

            return true;
        }


        [CustomPortBehavior(nameof(parameters))]
        IEnumerable<PortData> GeneratePortDataForParameters(List<SerializableEdge> edges)
        {
            if (output == null || output.material == null)
            {
                yield break;
            }

            foreach (var item in output.shaderProperties)
            {
                if (!item.displayInOutput)
                    continue;

                yield return new PortData()
                {
                    displayName = item.name,
                    displayType = MaterialUtils.GetTypeFromShaderProperty(output.shader, item),
                    identifier = item.name,
                    acceptMultipleEdges = true
                };
            }
        }

        [CustomPortOutput(nameof(parameters), typeof(object))]
        void PushMaterialData(List<SerializableEdge> edges)
        {
            foreach (var item in edges)
            {
                var prop = output.shaderProperties.FirstOrDefault(x => x.name == item.outputPortIdentifier);
                if (prop == null)
                    continue;

                switch (prop.type)
                {
                    case ShaderPropertyType.Texture:
                        switch (output.shader.GetPropertyTextureDimension(prop.index))
                        {
                            case TextureDimension.Tex2D:
                            case TextureDimension.None:
                                item.passThroughBuffer = output.material.GetTexture(prop.name);
                                continue;
                            case TextureDimension.Tex3D:
                                item.passThroughBuffer = output.material.GetTexture(prop.name) as Texture3D;
                                continue;
                            case TextureDimension.Cube:
                                item.passThroughBuffer = output.material.GetTexture(prop.name) as Cubemap;
                                continue;
                        }

                        continue;
                    case ShaderPropertyType.Color:
                        item.passThroughBuffer = (Color) output.material.GetColor(prop.name);
                        continue;
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:
                        item.passThroughBuffer = (float) output.material.GetFloat(prop.name);
                        continue;
                    case ShaderPropertyType.Vector:
                        item.passThroughBuffer = output.material.GetVector(prop.name);
                        continue;
                }
            }
        }

        [CustomPortInput(nameof(input), typeof(MixtureMaterial))]
        void CopyInputMaterial(List<SerializableEdge> edges)
        {
            foreach (var item in edges)
            {
                var mm = item.passThroughBuffer as MixtureMaterial;
                List<ShaderPropertyData> propertyList = null;
                if (mm == null)
                    continue;
                if (output != null)
                {
                    if (mm.shader == output.shader)
                    {
                        propertyList = output.shaderProperties;
                    }
                }

                output = new MixtureMaterial(mm, true, propertyList);
            }
        }

        protected override void Disable()
        {
            base.Disable();
        }
    }
}