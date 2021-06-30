using UnityEngine;

namespace Mixture
{
    [ExecuteInEditMode]
    public class PaintTarget2D : MonoBehaviour
    {
        public float extendsIslandOffset = 1.0f;

        public RenderTexture extendIslandsRenderTexture;
        public RenderTexture uvIslandsRenderTexture;
        public RenderTexture maskRenderTexture;
        public RenderTexture supportTexture;

        private Renderer rd;
        private Collider col;
    
        public RenderTexture getMask() => maskRenderTexture;
        public RenderTexture getUVIslands() => uvIslandsRenderTexture;
        public RenderTexture getExtend() => extendIslandsRenderTexture;
        public RenderTexture getSupport() => supportTexture;
        public Renderer getRenderer() => rd;

        public Collider getCollider() => col;
    
        public GameObject cameraPosition = null;
        
        private void Start()
        {
            Debug.Log($"Paint Target {gameObject.name} Start()");
            rd = GetComponent<Renderer>();
            col = GetComponent<Collider>();
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