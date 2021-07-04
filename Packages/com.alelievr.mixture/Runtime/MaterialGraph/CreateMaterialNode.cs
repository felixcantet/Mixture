using System;
using UnityEngine;
using GraphProcessor;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Linq;


namespace Mixture
{
    [System.Serializable, NodeMenuItem("Material/Create Material")]
    public class CreateMaterialNode : BaseMaterialNode
    {
        [Input] public List<object> input;

        [ShowInInspector][Output] public MixtureMaterial output;

        [HideInInspector] public Shader shader;

        public override string name => "Create Material";

        public override bool showDefaultInspector => true;

        public override Material previewMaterial
        {
            get
            {
                if (output == null)
                {
                    CreateOutputMaterial();
                }

                return output.material;
            }
        }

        protected override void Enable()
        {
        }

        protected override bool ProcessNode(CommandBuffer cmd)
        {
            if (!base.ProcessNode(cmd))
                return false;

            if (output == null)
                CreateOutputMaterial();

            if (shader != null && shader != output.material.shader)
            {
                CreateOutputMaterial();
                Debug.Log("Create Material");
            }

            //MaterialUtils.AssignMaterialPropertiesFromEdges(output, GetAllEdges().ToList());
           // output.ResetAllPropertyToDefault();
            //CustomInputFunction(GetAllEdges().Where(x => x.outputPort.fieldName == "input").ToList());
            return true;
        }

        [CustomPortBehavior(nameof(input))]
        IEnumerable<PortData> CreateMaterialPropertyPorts(List<SerializableEdge> edges)
        {
            if (output == null)
                yield break;

            foreach (var item in output.shaderProperties)
            {
                if (item.displayInOutput)
                {
                    yield return new PortData()
                    {
                        displayName = item.name,
                        displayType = MaterialUtils.GetTypeFromShaderProperty(output.shader, item),
                        identifier = item.name
                    };
                }
            }
        }
        
        

        [CustomPortInput(nameof(input), typeof(object))]
        void CustomInputFunction(List<SerializableEdge> edges)
        {
            if (output == null)
                return;

           //output.ResetAllPropertyToDefault();

            foreach (var item in inputPorts)
            {
                //var edge = item.GetEdges().FirstOrDefault(x => x.outputPortIdentifier == item.portData.identifier);
                var prop = output.shaderProperties.FirstOrDefault(x => x.name == item.portData.identifier);
                output.SetPropertyFromEdge(prop, item.GetEdges());
            }

            return;
            
            foreach (var item in edges)
            {
                var prop = output.shaderProperties.FirstOrDefault(x => x.name == item.inputPortIdentifier);
                if (prop == null)
                    continue;


                switch (prop.type)
                {
                    case ShaderPropertyType.Texture:
                        // switch (output.material.shader.GetPropertyTextureDimension(prop.index))
                        // {
                        //case TextureDimension.Any
                        if (item.passThroughBuffer is Texture t && t != null)
                        {
                            Debug.Log($"Texte : {t}" );
                            if (output.material.shader.GetPropertyTextureDimension(prop.index) == t.dimension)
                            {
                                Debug.Log($"PASSED");   
                                output.material.SetTexture(prop.name, (Texture) item.passThroughBuffer);
                            }
                        }
                        //     
                        //     case TextureDimension.Tex2D:
                        //         var texture2DValue = item.passThroughBuffer as Texture2D;
                        //         output.material.SetTexture(prop.name, texture2DValue);
                        //         Debug.Log($"Prop = {prop.name}");
                        //         Debug.Log($"Texture : {texture2DValue}");
                        //         continue;
                        //     case TextureDimension.None:
                        //         var texture = item.passThroughBuffer as Texture;
                        //         output.material.SetTexture(prop.name, texture);
                        //         Debug.Log($"Texture : {texture}");
                        //         continue;
                        //     case TextureDimension.Tex3D:
                        //         var texture3DValue = item.passThroughBuffer as Texture3D;
                        //         output.material.SetTexture(prop.name, texture3DValue);
                        //         continue;
                        //     case TextureDimension.Cube:
                        //         var textureCubeValue = item.passThroughBuffer as Cubemap;
                        //         output.material.SetTexture(prop.name, textureCubeValue);
                        //         continue;
                        // }

                        continue;
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:
                        output.material.SetFloat(prop.name, (float) item.passThroughBuffer);
                        continue;
                    case ShaderPropertyType.Color:
                        output.material.SetColor(prop.name, (Color) item.passThroughBuffer);
                        continue;
                    case ShaderPropertyType.Vector:
                        output.material.SetVector(prop.name, (Vector4) item.passThroughBuffer);
                        continue;
                }
            }
        }

        public void CreateOutputMaterial()
        {
            Debug.Log("Reganerate Material");
            if (shader == null)
            {
                if (GraphicsSettings.renderPipelineAsset != null)
                    output = new MixtureMaterial(GraphicsSettings.renderPipelineAsset.defaultShader);
                else
                    output = new MixtureMaterial(Shader.Find("Standard"));
            }
            else
            {
                output = new MixtureMaterial(shader);
            }
        }

        protected override void Disable()
        {
            base.Disable();
        }
    }
}