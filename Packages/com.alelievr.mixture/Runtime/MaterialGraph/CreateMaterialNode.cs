using System;
using System.Collections.Generic;
using UnityEngine;
using GraphProcessor;
using ICSharpCode.NRefactory.Ast;
using UnityEngine.Rendering;

namespace Mixture
{
    [System.Serializable, NodeMenuItem("Custom/Create Material")]
    public class CreateMaterialNode : MixtureNode
    {
        [Input] public List<object> inputs;
        [ShowInInspector][Output] public Material material;

        [HideInInspector]public string shaderName = "Standard";
        [HideInInspector]public Shader shader;
        public override bool showDefaultInspector => true;
        public override string name => "CreateMaterialNode";

        private List<ShaderPropertyData> properties;

        [HideInInspector] public Action OnShaderChange;

        // TODO : Custom Preview

        protected override void Enable()
        {
            if (material == null)
            {
                InitializeMaterial();
            }
        }

        [CustomPortBehavior(nameof(inputs))]
        IEnumerable<PortData> GetInputPort(List<SerializableEdge> edges)
        {
            BuildPropertyData();
            foreach (var item in properties)
            {
                Debug.Log(graph.outputNode.GetTypeFromShaderProperty(item));
                yield return new PortData
                {
                    displayName = item.description,
                    displayType = graph.outputNode.GetTypeFromShaderProperty(item),
                    identifier = item.index.ToString()
                };
            }
        }

        void BuildPropertyData()
        {
            if (properties == null)
                properties = new List<ShaderPropertyData>();

            // if (properties.Count == shader.GetPropertyCount())
            //     return;

            properties.Clear();
            for (int i = 0; i < shader.GetPropertyCount(); i++)
            {
                properties.Add(new ShaderPropertyData(shader, i));
            }
        }

        protected override bool ProcessNode(CommandBuffer cmd)
        {
            if (!base.ProcessNode(cmd))
                return false;


            return true;
        }

        public void UpdateShader()
        {
            if (shaderName != shader.name)
            {
                OnShaderChange?.Invoke();
                shader = Shader.Find(this.shaderName);
                Debug.Log(this.shaderName);
            }
        }

        public void InitializeMaterial()
        {
            if (GraphicsSettings.renderPipelineAsset == null)
            {
                shader = Shader.Find("Standard");
                shaderName = "Standard";
                this.material = new Material(shader);
            }
            else
            {
                shader = GraphicsSettings.defaultRenderPipeline.defaultMaterial.shader;
                shaderName = shader.name;
                this.material = new Material(GraphicsSettings.defaultRenderPipeline.defaultMaterial);
            }
        }

        [CustomPortOutput(nameof(material), typeof(Material))]
        public void PushMaterialOutput(List<SerializableEdge> edges)
        {
            foreach (var item in edges)
            {
                item.passThroughBuffer = material as Material;
            }
        }
        
        [CustomPortInput(nameof(inputs), typeof(Material))]
        void PushMaterialProperties(List<SerializableEdge> edges)
        {
            if (material == null)
                return;
            
            foreach (var item in edges)
            {
                var type = item.inputPort.portData.displayType;

                if (item.passThroughBuffer == null)
                    continue;
                var propertyIndex = int.Parse(item.inputPort.portData.identifier);
                var propName = material.shader.GetPropertyName(propertyIndex);
                switch (material.shader.GetPropertyType(propertyIndex))
                {
                    case ShaderPropertyType.Color:
                        material.SetColor(propName, MixtureConversions.ConvertObjectToColor(item.passThroughBuffer));
                        break;
                    case ShaderPropertyType.Texture:
                        // TODO: texture scale and offset
                        // Check texture dim before assigning:
                        if (item.passThroughBuffer is Texture t && t != null)
                        {
                            var tex = graph.FindOutputTexture(item.inputPortIdentifier, true);
                            if (material.shader.GetPropertyTextureDimension(propertyIndex) == t.dimension)
                                material.SetTexture(propName, tex == null ? t : tex);
                        }

                        break;
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:
                        switch (item.passThroughBuffer)
                        {
                            case float f:
                                material.SetFloat(propName, f);
                                break;
                            case Vector2 v:
                                material.SetFloat(propName, v.x);
                                break;
                            case Vector3 v:
                                material.SetFloat(propName, v.x);
                                break;
                            case Vector4 v:
                                material.SetFloat(propName, v.x);
                                break;
                            case int i:
                                material.SetFloat(propName, i);
                                break;
                            default:
                                throw new Exception(
                                    $"Can't assign {item.passThroughBuffer.GetType()} to material float property");
                        }

                        break;
                    case ShaderPropertyType.Vector:
                        material.SetVector(propName, MixtureConversions.ConvertObjectToVector4(item.passThroughBuffer));
                        break;
                }
            }
        }
    }
}