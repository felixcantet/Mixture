using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using GraphProcessor;
using ICSharpCode.NRefactory.Ast;
using UnityEngine.Rendering;


namespace Mixture
{
    [Serializable]
    public class MixtureMaterial
    {
        public Material material;
        public Shader shader
        {
            get { return material == null ? null : material.shader; }
        }

        public List<ShaderPropertyData> shaderProperties;

        public MixtureMaterial(Material material)
        {
            this.material = material;
            this.shaderProperties = new List<ShaderPropertyData>();

            // Cache Shader Property
            this.CreatePropertyCache();
        }

        public MixtureMaterial(MixtureMaterial mixtureMat, bool deepcCopy = false, List<ShaderPropertyData> propertyList = null)
        {
            if(!deepcCopy)
                this.material = mixtureMat.material;
            else
            {
                this.material = new Material(mixtureMat.material);
            }
            if(propertyList == null)
                this.shaderProperties = mixtureMat.shaderProperties.ToList();
            else
            {
                this.shaderProperties = propertyList.ToList();
            }
        }
        
        public MixtureMaterial(Shader shader) : this(new Material(shader))
        {
        }

        // public MixtureMaterial() : this(new Material(Shader.Find("Standard")))
        // {
        // }

        public void CreatePropertyCache()
        {
            if (this.shaderProperties == null)
                this.shaderProperties = new List<ShaderPropertyData>();
            else
            {
                this.shaderProperties.Clear();
            }

            for (int i = 0; i < shader.GetPropertyCount(); i++)
            {
                shaderProperties.Add(new ShaderPropertyData(shader, i));
            }
        }

        public void UpdatePropertyCache()
        {
        }


        public ShaderPropertyData GetPropertyFromDescription(string desc)
        {
            return shaderProperties.FirstOrDefault(x => x.description == desc);
        }

        public ShaderPropertyData GetPropertyFromName(string name)
        {
            return shaderProperties.FirstOrDefault(x => x.name == name);
        }

        List<string> GetKeywordList()
        {
            return material.shaderKeywords.ToList();
        }
        
        public void SetPropertyFromEdge(ShaderPropertyData prop, List<SerializableEdge> edges)
        {
            var edge = edges.FirstOrDefault(x => x.inputPortIdentifier == prop.name);
            if (edge != null)
            {
                switch (prop.type)
                {
                    case ShaderPropertyType.Texture:
                        // switch (output.material.shader.GetPropertyTextureDimension(prop.index))
                        // {
                        //case TextureDimension.Any
                        if (edge.passThroughBuffer is Texture t && t != null)
                        {
                            Debug.Log($"Texte : {t}");
                            if (material.shader.GetPropertyTextureDimension(prop.index) == t.dimension)
                            {
                                Debug.Log($"PASSED");
                                material.SetTexture(prop.name, (Texture) edge.passThroughBuffer);
                            }
                        }

                        break;
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:
                        material.SetFloat(prop.name, (float) edge.passThroughBuffer);
                        break;
                    case ShaderPropertyType.Color:
                        material.SetColor(prop.name, (Color) edge.passThroughBuffer);
                        break;
                    case ShaderPropertyType.Vector:
                        material.SetVector(prop.name, (Vector4) edge.passThroughBuffer);
                        break;
                }
            }
            else
            {
                switch (prop.type)
                {
                    case ShaderPropertyType.Texture:
                        material.SetTexture(prop.name, null);
                        break;
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:
                        var floatValue = material.shader.GetPropertyDefaultFloatValue(prop.index);
                        material.SetFloat(prop.name, floatValue);
                        break;
                    case ShaderPropertyType.Vector:
                        var vectorValue = material.shader.GetPropertyDefaultVectorValue(prop.index);
                        material.SetVector(prop.name, vectorValue);
                        break;
                    case ShaderPropertyType.Color:
                        var colorValue = material.shader.GetPropertyDefaultVectorValue(prop.index);
                        material.SetColor(prop.name, colorValue);
                        break;
                }
            }
        }

        public void ResetAllPropertyToDefault()
        {
            foreach (var item in shaderProperties)
            {
                switch (item.type)
                {
                    case ShaderPropertyType.Texture:
                        material.SetTexture(item.name, null);
                        break;
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:
                        var floatValue = material.shader.GetPropertyDefaultFloatValue(item.index);
                        material.SetFloat(item.name, floatValue);
                        break;
                    case ShaderPropertyType.Vector:
                        var vectorValue = material.shader.GetPropertyDefaultVectorValue(item.index);
                        material.SetVector(item.name, vectorValue);
                        break;
                    case ShaderPropertyType.Color:
                        var colorValue = material.shader.GetPropertyDefaultVectorValue(item.index);
                        material.SetColor(item.name, colorValue);
                        break;
                }
            }
        }
    }
}