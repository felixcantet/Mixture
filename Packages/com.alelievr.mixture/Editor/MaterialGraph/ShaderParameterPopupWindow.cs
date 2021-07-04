using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mixture
{
    public class ShaderParameterPopupWindow : PopupWindowContent
        {
            public static readonly int width = 400;
            private Vector2 scrollPos;

            public MixtureMaterial material;
            public MixtureGraphView graphView;
            public Action onParameterChange;
            

            public ShaderParameterPopupWindow(MixtureMaterial material, Action onParameterChange)
            {
                this.material = material;
                this.onParameterChange = onParameterChange;
            }

            public override Vector2 GetWindowSize()
            {
                return new Vector2(width, 500);
            }

            Color GetGUIColor(ShaderPropertyData data)
            {
                // Color : 0.33 0.8 1 1
                // Vector : 0.066 0.46 1 1
                // Float : 0.2 0.2 1 1
                switch (data.type)
                {
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:
                        return new Color(0.2f, 0.2f, 1 ,1);
                    
                    case ShaderPropertyType.Texture:
                        return new Color(1.0f, 0.6f, 0.066f, 1.0f);
                    
                    case ShaderPropertyType.Color:
                        return new Color(0.33f, 0.8f, 1.0f, 1.0f);
                    
                    case ShaderPropertyType.Vector:
                        return new Color(0.066f, 0.46f, 1f, 1f);
                    
                }

                return new Color(1.0f, 1.0f, 1.0f, 1.0f);
            }

            public override void OnGUI(Rect rect)
            {
                var needUpdate = false;
                GUILayout.Label("Shader Parameters", EditorStyles.boldLabel);
                scrollPos = GUILayout.BeginScrollView(
                    scrollPos); //, GUILayout.Width(100), GUILayout.Height(100));
                // int propCount = graph.outputMaterial.shader.GetPropertyCount();
                //
                // // Build Property List
                // if (graph.outputNode.enableParameters.Count != propCount)
                // {
                //     for (int i = 0; i < propCount; ++i)
                //     {
                //         graph.outputNode.enableParameters.Add(
                //             new ShaderPropertyData(graph.outputMaterial.shader, i));
                //     }
                // }
                //foreach(item in )

                foreach (var item in material.shaderProperties)
                {
                    Rect r = EditorGUILayout.GetControlRect(false, 0);

                    r.height = 1;
                    EditorGUI.DrawRect(r, new Color(0.5f, 0.5f, 0.5f, 1));
                    var prevValue = item.displayInOutput;

                    GUILayout.BeginHorizontal();
                    var color = GUI.contentColor;
                    GUI.contentColor = GetGUIColor(item);
                    GUILayout.Label( MixtureToolbar.Styles.improveMixture.image);
                    GUI.contentColor = color;
                    string label = item.description + " (" +
                                   item.name + ")";
                    if (label.Length > 50)
                        label = label.Substring(0, 50) + "...";
                    //GUILayout.Label(graph.outputNode.enableParameters[i].description + " (" + graph.outputNode.enableParameters[i].name + ")");
                    GUILayout.Label(label);
                    //GUILayout.Space(10);
                    GUILayout.FlexibleSpace();
                    // if (!graph.outputNode.enableParameters.ContainsKey(graph.outputMaterial.shader.GetPropertyName(i)))
                    // {
                    // 	graph.outputNode.enableParameters.Add(graph.outputMaterial.shader.GetPropertyName(i), false);
                    // }
                    var newValue = GUILayout.Toggle(item.displayInOutput, "");
                    item.displayInOutput = newValue;
                    if (prevValue != newValue)
                        needUpdate = true;
                    //graph.outputNode.enableParameters[graph.outputMaterial.shader.GetPropertyName(i)] = GUILayout.Toggle(graph.outputNode.enableParameters[graph.outputMaterial.shader.GetPropertyName(i)], "");
                    GUILayout.EndHorizontal();
                    GUILayout.Space(5);
                }

                GUILayout.EndScrollView();

                if (needUpdate)
                {
                    onParameterChange();
                }
            }
        }
}