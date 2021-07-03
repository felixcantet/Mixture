using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using GraphProcessor;
using ICSharpCode.NRefactory.Ast;
using UnityEngine.Rendering;

namespace Mixture
{
    [System.Serializable, NodeMenuItem("Material/Modify Material Parameter")]
    public class ModifyMaterialParameterNode : BaseMaterialNode
    {
        [Input] public MixtureMaterial input;
        [Input] public List<object> parameters = new List<object>();
        [Output] public MixtureMaterial output;
        [SerializeField] [HideInInspector] private Material baseMaterial;
        [SerializeField] public Material inspectMaterial;
        private List<(ShaderPropertyData, object)> shaderValues;
        public override string name => "ModifyMaterialParameterNode";
        public override bool needPropertySelector => true;
        public override bool showDefaultInspector => true;

        public override MixtureMaterial targetPropertySelector
        {
            get
            {
                if (output != null)
                    return output;
                return null;
            }
        }

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
            base.Enable();

            shaderValues = new List<(ShaderPropertyData, object)>();
            UpdateAllPorts();
            
            var edges = GetAllEdges().ToList();
            Debug.Log("test");
            // foreach (var item in edges)
            // {
            //     
            // }
            
        }

        protected override bool ProcessNode(CommandBuffer cmd)
        {
            //UpdateAllPorts();
            if (!base.ProcessNode(cmd))
                return false;

            if (input == null || input.material == null)
                return false;
            // if (output == null)
            // {
            //     output = new MixtureMaterial(input);
            //     inspectMaterial = output.material;
            //     baseMaterial = input.material;
            // }
            output = new MixtureMaterial(input, true);

            foreach (var item in shaderValues)
            {
                var data = item.Item1;
                if (data.type == ShaderPropertyType.Texture)
                {
                    var value = item.Item2 as Texture;
                    output.material.SetTexture(data.name, value);
                }
                else if (data.type == ShaderPropertyType.Float)
                {
                    var value = (float)item.Item2;
                    output.material.SetFloat(data.name, value);
                }
                else if (data.type == ShaderPropertyType.Color)
                {
                    var value = (Color)item.Item2;
                    output.material.SetColor(data.name, value);
                }
                else if (data.type == ShaderPropertyType.Vector)
                {
                    var value = (Vector4)item.Item2;
                    output.material.SetVector(data.name, value);
                }
                
            }
            
            shaderValues.Clear();
            
            // else if (baseMaterial != input.material)
            // {
            //     output = new MixtureMaterial(input);
            //     inspectMaterial = output.material;
            //     baseMaterial = input.material;
            // }

            // Insert your code here 

            return true;
        }

        public override IEnumerable<FieldInfo> OverrideFieldOrder(IEnumerable<FieldInfo> fields)
        {
            return fields.OrderBy(f1 => {
                if (f1.Name == nameof(input))
                    return 0;
                else
                    return 1;
            });
        }

        
        [CustomPortBehavior(nameof(parameters))]
        IEnumerable<PortData> GeneratePropertyPorts(List<SerializableEdge> edges)
        {
            // if (input == null || input.material == null)
            //     yield break;
            // if (input != null)
            // {
            //     output = new MixtureMaterial(input, true);
            //     inspectMaterial = output.material;
            // }
            if (output == null || output.material == null)
                yield break;
            
            
            foreach (var item in output.shaderProperties)
            {
                if (!item.displayInOutput)
                    continue;

                
                yield return new PortData()
                {
                    displayName = item.name,
                    displayType = MaterialUtils.GetTypeFromShaderProperty(output.shader, item),
                    identifier = item.name
                };
            }
        }
        
        [CustomPortInput(nameof(input), typeof(MixtureMaterial))]
        void CustomInputPort(List<SerializableEdge> edges)
        {
            
            foreach (var item in edges)
            {
                input = new MixtureMaterial(item.passThroughBuffer as MixtureMaterial, true);
                inspectMaterial = input.material;
            }
            
        }
        

        [CustomPortInput(nameof(parameters), typeof(object))]
        void GatherPropertyToOverride(List<SerializableEdge> edges)
        {
            foreach (var item in edges)
            {
                var displayType = item.inputPort.portData.displayType;
                var name = item.inputPort.portData.identifier;
                if (displayType == typeof(Texture) || displayType == typeof(Texture2D))
                {
                    var value = item.passThroughBuffer as Texture;
                    var data = new ShaderPropertyData(name, ShaderPropertyType.Texture);
                    shaderValues.Add((data, value));
                }
                else if (displayType == typeof(float))
                {
                    var value = (float)item.passThroughBuffer;
                    var data = new ShaderPropertyData(name, ShaderPropertyType.Float);
                    shaderValues.Add((data, value));
                }
                else if (displayType == typeof(Color))
                {
                    var value = (Color)item.passThroughBuffer;
                    var data = new ShaderPropertyData(name, ShaderPropertyType.Color);
                    shaderValues.Add((data, value));
                }
                else if (displayType == typeof(Vector4))
                {
                    var value = (Vector4)item.passThroughBuffer;
                    var data = new ShaderPropertyData(name, ShaderPropertyType.Vector);
                    shaderValues.Add((data, value));
                }
                
            }
        }

        //[CustomPortInput(nameof(parameters), typeof(object))]
        void SetMaterialProperties(List<SerializableEdge> edges)
        {
            if (input == null)
                return;
            output = input;
            
            foreach (var item in edges)
            {
                var prop = output.shaderProperties.FirstOrDefault(x => x.name == item.inputPortIdentifier);
                if (prop == null)
                    continue;

                
                switch (prop.type)
                {
                    case ShaderPropertyType.Texture:
                        switch (output.shader.GetPropertyTextureDimension(prop.index))
                        {
                            case TextureDimension.Tex2D:
                            case TextureDimension.None:
                                var tex = item.passThroughBuffer as Texture;
                                output.material.SetTexture(prop.name, tex);
                                continue;
                            case TextureDimension.Tex3D:
                                var tex3D = item.passThroughBuffer as Texture3D;
                                output.material.SetTexture(prop.name, tex3D);
                                continue;
                            case TextureDimension.Cube:
                                var cubemap = item.passThroughBuffer as Cubemap;
                                output.material.SetTexture(prop.name, cubemap);
                                continue;
                        }

                        continue;
                    case ShaderPropertyType.Color:
                        output.material.SetColor(prop.name, (Color) item.passThroughBuffer);
                        continue;
                    case ShaderPropertyType.Vector:
                        output.material.SetVector(prop.name, (Vector4) item.passThroughBuffer);
                        continue;
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:
                        output.material.SetFloat(prop.name, (float) item.passThroughBuffer);
                        continue;
                }
            }
        }

        

        protected override void Disable()
        {
            base.Disable();
        }
    }
}