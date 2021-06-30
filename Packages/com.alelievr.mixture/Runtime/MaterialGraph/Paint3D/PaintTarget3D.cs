using System.Collections.Generic;
using UnityEngine;

namespace Mixture
{
    [ExecuteInEditMode]
    public class PaintTarget3D : MonoBehaviour
    {
        public List<Material> materialsPalette = new List<Material>();

        public float extendsIslandOffset = 1.0f;

        public RenderTexture extendIslandsRenderTexture;
        public RenderTexture uvIslandsRenderTexture;
        public RenderTexture maskRenderTexture;
        public RenderTexture supportTexture;

        private Renderer rd;

        public RenderTexture getMask() => maskRenderTexture;
        public RenderTexture getUVIslands() => uvIslandsRenderTexture;
        public RenderTexture getExtend() => extendIslandsRenderTexture;
        public RenderTexture getSupport() => supportTexture;
        public Renderer getRenderer() => rd;

        private void Start()
        {
            Debug.Log($"Paint Target {gameObject.name} Start()");
            rd = GetComponent<Renderer>();
        }

        void OnDestroy()
        {
            Debug.Log($"Paint Target {gameObject.name} OnDestroy()");

            //maskRenderTexture.Release();
            //uvIslandsRenderTexture.Release();
            //extendIslandsRenderTexture.Release();
            //supportTexture.Release();
        }
    }
}