using System.Collections;
using System.Collections.Generic;
using GraphProcessor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mixture
{
    [NodeCustomEditor(typeof(Paint2DNode))]
    public class Paint2DNodeView : MixtureNodeView
    {
        private Paint2DNode node;
        
        public override void Enable(bool fromInspector)
        {
            base.Enable(fromInspector);

            node = nodeTarget as Paint2DNode;

            Button button = new Button(
                () =>
                {
                    if(node.outMaterial == null || node.mask == null)
                        node.InitializeCrts();
                    
                    
                    Paint2DPreviewSceneStage.ShowWindow(node.outMaterial, node.extendIslandRenderTexture, node.uvIslandRenderTexture, node.maskRenderTexture,
                        node.supportTexture);
                });
            
            button.name = "Open 2D Window";
            button.text = "Open 2D Window";

            controlsContainer.Add(button);
        }
    }
}