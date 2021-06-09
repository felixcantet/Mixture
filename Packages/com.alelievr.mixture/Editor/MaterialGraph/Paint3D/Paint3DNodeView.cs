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

                    if (node.refMat == null)
                        return;
                    
                    Painting3DPreviewSceneStage.ShowWindow(node.inMesh, node.refMat);
                    
                });
            button.name = "Open 3D Paiting";
            button.text = "Open 3D Paiting";
            
            controlsContainer.Add(button);
        }
    }
}