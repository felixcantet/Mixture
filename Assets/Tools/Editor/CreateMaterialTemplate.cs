using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace Mixture
{
    public class CreateMaterialTemplate : Editor
    {
        [MenuItem("Custom/Generate Material Template Graph")]
        public static void GenerateMaterialTemplate()
        {
            //     var g = ScriptableObject.CreateInstance<MixtureGraph>();
            //     g.type = MixtureGraphType.Material;
            //     g.nodes.Add(new OutputNode());
            //     AssetDatabase.CreateAsset(g, "Assets/Resources/MaterialGraphTemplate.asset");
            var graph = AssetDatabase.LoadAllAssetsAtPath("Assets/Samples/Material2.asset")
                .FirstOrDefault(o => o is MixtureGraph) as MixtureGraph;
            //var graph = AssetDatabase.LoadAssetAtPath<MixtureGraph>("Assets/Samples/TestMaterial.asset");
            Debug.Log($"Type ={graph.type}");
            Debug.Log($"output ={graph.outputTextures} \n Count : {graph.outputTextures.Count}");
            Debug.Log($"Material ={graph.outputMaterial} \n Shader : {graph.outputMaterial.shader}");
            Debug.Log($"Graph : {graph.outputNode.outputTextureSettings.Count}");
            
            foreach (var item in graph.outputNode.outputTextureSettings)
            {
                Debug.Log($"FCM : {item.finalCopyMaterial}");
            }
            foreach (var item in graph.outputTextures)
            {
                Debug.Log($"Output texture : {item.width}");
            }

            foreach (var item in graph.outputNode.outputTextureSettings)
            {
                Debug.Log("output Texture Settings : " + item.name + " | input Tex : " + item.inputTexture);
            }

        }
    }
}