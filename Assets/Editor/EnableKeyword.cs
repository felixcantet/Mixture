using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
public class EnableKeyword : Editor
{
    [MenuItem("Custom/EnableShaderKeyword")]
    public static void EnableMaterialKeyword()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Samples/DemoMaterialGraph.asset");
        if (mat == null)
        {
            Debug.Log("Bad Path");
            return;
        }
        
        mat.EnableKeyword("USE_NORMAL");
        mat.EnableKeyword("USE_SPECULAR");
        mat.EnableKeyword("USE_VERTEX");
        mat.EnableKeyword("USE_PHONG");
        mat.EnableKeyword("USE_OCCLUSION");
        mat.EnableKeyword("USE_ALBEDO");
        mat.EnableKeyword("USE_DETAILALBEDO");
        mat.EnableKeyword("USE_DETAILNORMAL");
        foreach (var item in mat.shaderKeywords)
        {
            Debug.Log(item);
        }
        
    }
    
}
