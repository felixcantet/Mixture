using System.Collections.Generic;
using System.Linq;
using GraphProcessor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mixture
{
    [System.Serializable]
    public abstract class PaintNode : MixtureNode, IUseCustomRenderTextureProcessing
    {
        
        [SerializeField] public RenderTexture maskRenderTexture, extendIslandRenderTexture, uvIslandRenderTexture, supportTexture;
        protected List<CustomRenderTexture> crts;
        
        public override bool hasSettings => false;
        public override string name => "Paint Base Node";

        public override void OnNodeCreated()
        {
            base.OnNodeCreated();
            
            this.maskRenderTexture = new RenderTexture(1024, 1024, 0);
            this.maskRenderTexture.filterMode = FilterMode.Bilinear;
            this.extendIslandRenderTexture = new RenderTexture(maskRenderTexture.descriptor);
            this.uvIslandRenderTexture = new RenderTexture(maskRenderTexture.descriptor);
            this.supportTexture = new RenderTexture(maskRenderTexture.descriptor);
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

