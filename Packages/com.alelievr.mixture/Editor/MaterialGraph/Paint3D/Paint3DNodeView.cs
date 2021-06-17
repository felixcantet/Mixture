using System.Collections;
using System.Collections.Generic;
using GraphProcessor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mixture
{
    [NodeCustomEditor(typeof(Paint3DNode))]
    public class Paint3DNodeView : MixtureNodeView
    {
        Paint3DNode node;

        public override void Enable(bool fromInspector)
        {
            base.Enable(fromInspector);

            node = nodeTarget as Paint3DNode;

            var button = new Button(
                () =>
                {
                    if (node.inMesh == null)
                        return;

                    if (node.materialA == null)
                        return;
                    
                    node.InitializeCrts();
                    
                    Painting3DPreviewSceneStage.ShowWindow(node.inMesh, node.outMaterial, 
                        new List<Material>(){ node.materialA, node.materialB },
                        node.extendIslandRenderTexture, node.uvIslandRenderTexture, node.maskRenderTexture, node.supportTexture);
                    
                });
            button.name = "Open 3D Painting Scene";
            button.text = "Open 3D Painting Scene";
            
            controlsContainer.Add(button);
        }
    }
}