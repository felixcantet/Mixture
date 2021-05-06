using UnityEngine;
using GraphProcessor;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Linq;
namespace Mixture
{
    [System.Serializable, NodeMenuItem("Custom/MaterialGeneratorNode")]
    public class MaterialGeneratorNode : MixtureNode
    {
        [Input]
        public List<object> input;

        [Output]
        public Material output;

        [SerializeField]
        [ShowInInspector]
        public Shader targetShader;

        public override string name => "MaterialGeneratorNode";

        //public override Texture previewTexture => output;

        public override bool showDefaultInspector => true;

        System.Type GetTypeFromShaderType(ShaderPropertyType type, int index)
        {
            if (type == ShaderPropertyType.Color)
            {
                return typeof(Color);
            }
            if (type == ShaderPropertyType.Float)
            {
                return typeof(float);
            }
            if (type == ShaderPropertyType.Vector)
            {
                return typeof(Vector4);
            }
            if (type == ShaderPropertyType.Texture)
            {
                return TextureUtils.GetTypeFromDimension(targetShader.GetPropertyTextureDimension(index));
            }
            else
            {
                return typeof(int);
            }
        }

        [CustomPortBehavior(nameof(input))]
        IEnumerable<PortData> AppendShaderProperties(List<SerializableEdge> edges)
        {
            if (targetShader == null)
                yield break;
            for (int i = 0; i < targetShader.GetPropertyCount(); i++)
            {
                var propName = targetShader.GetPropertyName(i);
                var type = targetShader.GetPropertyType(i);
                var desc = targetShader.GetPropertyDescription(i);
                if (targetShader.GetPropertyAttributes(i).Any(f => f == "HideInInspector" || f == "NonModifiableTextureData"))
                    continue;
                yield return new PortData
                {
                    displayName = desc,
                    identifier = propName,
                    acceptMultipleEdges = false,
                    displayType = GetTypeFromShaderType(type, i)
                };


            }
        }

        [CustomPortInput(nameof(input), typeof(Material))]
        void PushMaterialProperties(List<SerializableEdge> edges)
        {
            if (this.output == null)// || this.output.shader != targetShader)
                this.output = new Material(targetShader);
            if (this.output.shader != targetShader)
                this.output = new Material(targetShader);

            foreach (var item in edges)
            {
                var type = item.outputPort.portData.displayType;
                
                if (type == typeof(Texture) || type == typeof(Texture2D) || type == typeof(Texture3D))
                    this.output.SetTexture(item.inputPort.portData.identifier, item.passThroughBuffer as Texture);
                else if (type == typeof(float))
                {
                    this.output.SetFloat(item.inputPort.portData.identifier, (float)item.passThroughBuffer);
                }
                else if (type == typeof(int))
                {
                    this.output.SetInt(item.inputPort.portData.identifier, (int)item.passThroughBuffer);
                }
                else if (type == typeof(Vector4))
                {
                    this.output.SetVector(item.inputPort.portData.identifier, (Vector4)item.passThroughBuffer);
                }
                else if (type == typeof(Color))
                {
                    this.output.SetColor(item.inputPort.portData.identifier, (Color)item.passThroughBuffer);
                }
            }
        }



        protected override void Enable()
        {
            this.targetShader = Shader.Find("Standard");

            //UpdateTempRenderTexture(ref output);
        }

        protected override bool ProcessNode(CommandBuffer cmd)
        {
            if (!base.ProcessNode(cmd))
                return false;

            // Update temp target in case settings changes
            //UpdateTempRenderTexture(ref output);

            // Insert your code here 

            return true;
        }

        protected override void Disable()
        {
            base.Disable();
        }
    }
}