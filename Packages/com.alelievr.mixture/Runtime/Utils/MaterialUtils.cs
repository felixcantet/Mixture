using GraphProcessor;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


namespace Mixture
{
    public static class MaterialUtils
    {
        public static void AssignMaterialPropertiesFromEdges(MixtureMaterial material, List<SerializableEdge> edges)
        {
            foreach (var prop in material.shaderProperties)
            {
                if (!prop.displayInOutput)
                    continue;
                var edge = edges.FirstOrDefault(x => x.outputPortIdentifier == prop.name);
                if (edge == null)
                    continue;

                Debug.Log("Assign");
                SetMaterialPropertyFromData(material.material, prop, edge.passThroughBuffer);
            }
        }

        public static void SetMaterialPropertyFromData(Material material, ShaderPropertyData data, object value)
        {
            var type = GetTypeFromShaderProperty(material.shader, data);
            Debug.Log(type);
            switch (data.type)
            {
                case ShaderPropertyType.Texture:
                    switch (material.shader.GetPropertyTextureDimension(data.index))
                    {
                        case TextureDimension.Tex2D:
                        case TextureDimension.None:
                            material.SetTexture(data.name, value as Texture2D);
                            break;

                        case TextureDimension.Tex3D:
                            material.SetTexture(data.name, value as Texture3D);
                            break;
                        case TextureDimension.Cube:
                            material.SetTexture(data.name, value as Cubemap);
                            break;
                    }

                    break;
                case ShaderPropertyType.Float:
                case ShaderPropertyType.Range:
                    material.SetFloat(data.name, (float) value);
                    break;
                case ShaderPropertyType.Vector:
                    material.SetVector(data.name, (Vector4) value);
                    break;
                case ShaderPropertyType.Color:
                    material.SetColor(data.name, (Color) value);
                    break;
            }
        }

        public static void AssignMaterialPropertiesFromGraph(MixtureGraph graph, List<SerializableEdge> edges,
            Material material, bool useBakedTextures = false)
        {
            Debug.Log("ASSIGN");
            edges.ForEach(w => { Debug.Log($"Output Port Identifier : {w.inputPortIdentifier}"); });
            // Reset not exposed or not connected properties
            foreach (var item in graph.outputNode.enableParameters)
            {
                Debug.Log("Port Found : " + edges.FirstOrDefault(x => x.inputPortIdentifier == item.name));
                if (!item.displayInOutput || edges.FirstOrDefault(x => x.inputPortIdentifier == item.name) == null)
                {
                    if (item.type == ShaderPropertyType.Texture)
                        material.SetTexture(item.name, null);
                    else if (item.type == ShaderPropertyType.Float || item.type == ShaderPropertyType.Range)
                        material.SetFloat(item.name, material.shader.GetPropertyDefaultFloatValue(item.index));
                    else if (item.type == ShaderPropertyType.Color)
                        material.SetColor(item.name, material.shader.GetPropertyDefaultVectorValue(item.index));
                    else if (item.type == ShaderPropertyType.Vector)
                        material.SetVector(item.name, material.shader.GetPropertyDefaultVectorValue(item.index));
                }

                else
                {
                    if (item.type == ShaderPropertyType.Texture)
                    {
                        Debug.Log(graph.FindTextureOnDisk(item.name, false));
                        if (useBakedTextures)
                        {
                            material.SetTexture(item.name, graph.FindTextureOnDisk(item.name, false));
                        }
                        else
                        {
                            material.SetTexture(item.name,
                                graph.outputNode.outputTextureSettings.FirstOrDefault(x => x.name == item.name)
                                    .inputTexture);
                        }
                    }
                    else if (item.type == ShaderPropertyType.Color)
                    {
                        material.SetColor(item.name,
                            (Color) graph.outputNode.GetAllEdges()
                                .FirstOrDefault(x => x.inputPortIdentifier == item.name).passThroughBuffer);
                    }
                    else if (item.type == ShaderPropertyType.Float || item.type == ShaderPropertyType.Range)
                    {
                        material.SetFloat(item.name,
                            (float) graph.outputNode.GetAllEdges()
                                .FirstOrDefault(x => x.inputPortIdentifier == item.name).passThroughBuffer);
                    }
                    else if (item.type == ShaderPropertyType.Vector)
                    {
                        material.SetVector(item.name,
                            (Vector4) graph.outputNode.GetAllEdges()
                                .FirstOrDefault(x => x.inputPortIdentifier == item.name).passThroughBuffer);
                    }
                }
            }

            foreach (var edge in edges)
            {
                if (edge.passThroughBuffer is Texture2D)
                {
                }
            }
        }

        public static Type GetTypeFromShaderProperty(Shader shader, ShaderPropertyData prop)
        {
            if (prop.type == ShaderPropertyType.Texture)
            {
                switch (shader.GetPropertyTextureDimension(prop.index))
                {
                    case TextureDimension.Tex2D:
                        return typeof(Texture2D);
                    case TextureDimension.Tex3D:
                        return typeof(Texture3D);
                    case TextureDimension.Cube:
                        return typeof(Cubemap);
                }
            }
            else if (prop.type == ShaderPropertyType.Float || prop.type == ShaderPropertyType.Range)
            {
                return typeof(float);
            }
            else if (prop.type == ShaderPropertyType.Vector)
            {
                return typeof(Vector4);
            }
            else if (prop.type == ShaderPropertyType.Color)
                return typeof(Color);

            return typeof(float);
        }
    }
}