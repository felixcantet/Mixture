using System.Collections.Generic;
using System.Linq;
using GraphProcessor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace Mixture
{
    [System.Serializable]
    public abstract class PaintNode : MixtureNode, IUseCustomRenderTextureProcessing
    {
        [SerializeField]
        public RenderTexture maskRenderTexture, extendIslandRenderTexture, uvIslandRenderTexture, supportTexture;

        protected List<CustomRenderTexture> crts;

        [SerializeField, HideInInspector, FormerlySerializedAs("output")]
        internal Texture2D savedTexture;

        public override bool hasSettings => false;
        public override string name => "Paint Base Node";

        
        protected bool isInitialized = false;

        
        public override void OnNodeCreated()
        {
            base.OnNodeCreated();

            this.isInitialized = false;
        }

        protected override void Enable()
        {
            base.Enable();
        }
        
        protected override bool ProcessNode(CommandBuffer cmd)
        {
            if (!base.ProcessNode(cmd))
                return false;

            if (!this.isInitialized)
            {
                this.maskRenderTexture = new RenderTexture(graph.settings.width, graph.settings.height, 0);
                this.maskRenderTexture.filterMode = FilterMode.Bilinear;
                this.extendIslandRenderTexture = new RenderTexture(maskRenderTexture.descriptor);
                this.uvIslandRenderTexture = new RenderTexture(maskRenderTexture.descriptor);
                this.supportTexture = new RenderTexture(maskRenderTexture.descriptor);
                
                LoadTexture();
                
                this.isInitialized = true;
            }


            return this.isInitialized;
        }

        protected override void Destroy()
        {
            base.Destroy();
            Debug.Log("Node destroyed");

            foreach (var item in crts)
            {
                item.Release();
            }

            crts.Clear();

            maskRenderTexture?.Release();
            extendIslandRenderTexture?.Release();
            uvIslandRenderTexture?.Release();
            supportTexture?.Release();
        }

        public abstract void InitializeCrts();

        protected void LoadTexture()
        {
            if (graph.IsObjectInGraph(savedTexture))
            {
                Debug.Log("Saved texture is into graph");
                var references = graph.GetObjectsReferences();
                foreach (var obj in references)
                {
                    if (obj is Texture2D)
                    {
                        Texture2D tex = obj as Texture2D;
                        if (tex.Equals(savedTexture))
                        {
                            savedTexture = tex;
                            
                            Graphics.Blit(savedTexture, maskRenderTexture);
                            Graphics.Blit(savedTexture, supportTexture);
                            Graphics.Blit(savedTexture, extendIslandRenderTexture);
                            
                            Debug.Log("Texture finded");
                            break;
                        }
                    }
                }
            }
            else
            {
                Debug.Log("Not in graph");
                UpdateRenderTextures();
            }
        }
        
        public void SaveCurrentTexture()
        {
            Debug.Log("Save Texture");
            
            // Temp texture for the readback (before compression)
            Texture2D tmp = new Texture2D(savedTexture.width, savedTexture.height, GraphicsFormat.R32G32B32A32_SFloat,
                TextureCreationFlags.None);
            
            // Radback color & depth:
            RenderTexture.active = extendIslandRenderTexture;
            tmp.ReadPixels(new Rect(0, 0, savedTexture.width, savedTexture.height), 0, 0);
            RenderTexture.active = null;
            tmp.Apply();

#if UNITY_EDITOR
            savedTexture.SetPixels(tmp.GetPixels());
            savedTexture.Apply();
#endif

            graph.NotifyNodeChanged(this);
            
            if (!graph.IsObjectInGraph(savedTexture))
                graph.AddObjectToGraph(savedTexture);
        }

        protected void UpdateRenderTextures()
        {
            if (savedTexture == null || settings.NeedsUpdate(graph, savedTexture, false))
            {
                Debug.Log("Update render textures");
                if (graph.IsObjectInGraph(savedTexture))
                {
                    graph.RemoveObjectFromGraph(savedTexture);
                    Object.DestroyImmediate(savedTexture, true);
                }
                savedTexture = new Texture2D(settings.GetResolvedWidth(graph), settings.GetResolvedHeight(graph), GraphicsFormat.R32G32B32A32_SFloat, TextureCreationFlags.None) { name = "SceneNode Rendering"};
                savedTexture.hideFlags = HideFlags.NotEditable;
                graph.AddObjectToGraph(savedTexture);
            }
        }

        public IEnumerable<CustomRenderTexture> GetCustomRenderTextures()
        {
            if (crts == null)
                crts = new List<CustomRenderTexture>();

            foreach (var item in crts)
            {
                yield return item;
            }
        }
    }
}