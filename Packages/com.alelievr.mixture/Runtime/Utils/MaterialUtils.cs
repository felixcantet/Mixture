using GraphProcessor;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Rendering;


namespace Mixture
{
    public static class MaterialUtils
    {
        public static void AssignMaterialPropertiesFromGraph(MixtureGraph graph, List<SerializableEdge> edges,
            Material material, bool useBakedTextures = false)
        {
            Debug.Log("ASSIGN");
            edges.ForEach(w =>
            {
                Debug.Log($"Output Port Identifier : {w.inputPortIdentifier }");
            });
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
    }
}