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

                Graphics.SetRenderTarget(this.maskRenderTexture);
                GL.Clear(true, true, new Color(0.0f, 0.0f, 0.0f, 0.0f));

                Graphics.SetRenderTarget(this.extendIslandRenderTexture);
                GL.Clear(true, true, new Color(0.0f, 0.0f, 0.0f, 0.0f));

                Graphics.SetRenderTarget(this.supportTexture);
                GL.Clear(true, true, new Color(0.0f, 0.0f, 0.0f, 0.0f));

                Graphics.SetRenderTarget(null);

                LoadTexture();

                this.isInitialized = true;
            }

            UpdateRenderTextures();

            return this.isInitialized;
        }

        protected override void Destroy()
        {
            base.Destroy();
            Debug.Log("Node destroyed");

            if (graph.IsObjectInGraph(savedTexture))
            {
                graph.RemoveObjectFromGraph(savedTexture);
                Object.DestroyImmediate(savedTexture, true);
            }
            
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
                    if (obj is Texture2D tex)
                    {
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
            if (savedTexture == null)
            {
                if (graph.IsObjectInGraph(savedTexture))
                {
                    graph.RemoveObjectFromGraph(savedTexture);
                    Object.DestroyImmediate(savedTexture, true);
                }

                Debug.Log("Create Saved Texture");
                savedTexture =
                    new Texture2D(settings.GetResolvedWidth(graph), settings.GetResolvedHeight(graph),
                            GraphicsFormat.R32G32B32A32_SFloat, TextureCreationFlags.None)
                        {name = "PaintMask"};

                savedTexture.filterMode = settings.GetResolvedFilterMode(graph);
                savedTexture.wrapMode = settings.GetResolvedWrapMode(graph);
            }
            

            if (settings.NeedsUpdate(graph, savedTexture, false))
            {
                if (graph.IsObjectInGraph(savedTexture))
                    graph.RemoveObjectFromGraph(savedTexture);


                Debug.Log("Resize Saved Texture");
                savedTexture = TextureRescale(savedTexture, settings.GetResolvedWidth(graph),
                    settings.GetResolvedHeight(graph));
                
                this.isInitialized = false;
                
                maskRenderTexture?.Release();
                extendIslandRenderTexture?.Release();
                uvIslandRenderTexture?.Release();
                supportTexture?.Release();
                
                savedTexture.hideFlags = HideFlags.NotEditable;
                savedTexture.filterMode = settings.GetResolvedFilterMode(graph);
                savedTexture.wrapMode = settings.GetResolvedWrapMode(graph);

                graph.AddObjectToGraph(savedTexture);
            }
        }

        public Texture2D TextureRescale(Texture2D tex, int width, int height)
        {
            RenderTexture rd = RenderTexture.GetTemporary(width, height, 0);
            Graphics.Blit(tex, rd);

            tex.Resize(width, height, GraphicsFormat.R32G32B32A32_SFloat, false);
            
            RenderTexture.active = rd;
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            RenderTexture.active = null;
            tex.Apply();

            RenderTexture.ReleaseTemporary(rd);
            
            return tex;
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