using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GraphProcessor;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mixture
{
    [NodeCustomEditor(typeof(Paint3DNode))]
    public class Paint3DNodeView : MixtureNodeView
    {
        Paint3DNode node;

        //private Button button;
        private List<string> connectedFields = new List<string>();

        public string materialAFieldName => "materialA";
        public string materialBFieldName => "materialB";
        public string inMeshFieldName => "inMesh";

        private int btnIdx = -1;
        
        public override void Enable(bool fromInspector)
        {
            base.Enable(fromInspector);

            node = nodeTarget as Paint3DNode;

            Button button = new Button(
                () =>
                {
                    if (node.inMesh == null)
                        return;

                    if (node.materialA == null)
                        return;

                    node.InitializeCrts();

                    Painting3DPreviewSceneStage.ShowWindow(node.inMesh, node.outMaterial,
                        new List<Material>() {node.materialA, node.materialB},
                        node.extendIslandRenderTexture, node.uvIslandRenderTexture, node.maskRenderTexture,
                        node.supportTexture);
                });
            button.name = "Open 3D Painting Scene";

            button.text = "Open 3D Painting Scene";

            bool test = this.connectedFields.Count > 2 && (this.connectedFields.TrueForAll(h =>
                h.Contains(inMeshFieldName) ||
                h.Contains(materialAFieldName) ||
                h.Contains(materialBFieldName)
            ));
            

            node.onAfterEdgeConnected -= OnPortConnectedCallback;
            node.onAfterEdgeConnected += OnPortConnectedCallback;

            node.onAfterEdgeDisconnected -= OnPortDisconnectedCallback;
            node.onAfterEdgeDisconnected += OnPortDisconnectedCallback;

            controlsContainer.Add(button);
            btnIdx = controlsContainer.Children().Count(x => x.Equals(button));
            
            
            controlsContainer[btnIdx].SetEnabled(test);
        }

        public override void Disable()
        {
            node.onAfterEdgeConnected -= OnPortConnectedCallback;
            node.onAfterEdgeDisconnected -= OnPortDisconnectedCallback;

            connectedFields.Clear();

            base.Disable();
        }

        private void OnPortConnectedCallback(SerializableEdge v)
        {
            Debug.Log("Port connected");

            var hash = v.inputPort.fieldName;

            if (!this.connectedFields.Contains(hash))
                this.connectedFields.Add(hash);
            
            

            bool test = this.connectedFields.Count > 2 && (this.connectedFields.TrueForAll(h =>
                            h.Contains(inMeshFieldName) ||
                            h.Contains(materialAFieldName) ||
                            h.Contains(materialBFieldName)
                        ));
            
            controlsContainer[btnIdx].SetEnabled(test);
        }

        private void OnPortDisconnectedCallback(SerializableEdge v)
        {
            Debug.Log("Port disconnected");
            
            var hash = v.inputPort.fieldName;

            if (this.connectedFields.Contains(hash))
                this.connectedFields.Remove(hash);

            
            bool test = this.connectedFields.Count > 2 && (this.connectedFields.TrueForAll(h =>
                h.Contains(inMeshFieldName) ||
                h.Contains(materialAFieldName) ||
                h.Contains(materialBFieldName)
            ));
            
            controlsContainer[btnIdx].SetEnabled(test);
        }
    }
}