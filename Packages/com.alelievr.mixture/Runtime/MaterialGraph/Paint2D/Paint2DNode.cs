using System.Collections;
using System.Collections.Generic;
using GraphProcessor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mixture
{
    [Documentation(@"To be filled.")]
    
    [System.Serializable, NodeMenuItem("Painting/Texture Painting")]
    public class Paint2DNode : MixtureNode, IUseCustomRenderTextureProcessing
    {
        [Input(name = "Texture Reference")]
        public Texture refTexture = null;
        
        [Output(name = "Out Mask")]
        public Texture mask = null;


        [SerializeField] public RenderTexture maskRenderTexture, extendIslandRenderTexture, uvIslandRenderTexture, supportTexture;
        List<CustomRenderTexture> crts;
        
        public override bool hasSettings => false;
        public override string name => "Texture Painting";
        
        public Material outMaterial = null;
        private const string unlitShader = "Unlit/Texture";
        
        
        public override void OnNodeCreated()
        {
            base.OnNodeCreated();
            
            this.maskRenderTexture = new RenderTexture(1024, 1024, 0);
            this.maskRenderTexture.filterMode = FilterMode.Bilinear;
            this.extendIslandRenderTexture = new RenderTexture(maskRenderTexture.descriptor);
            this.uvIslandRenderTexture = new RenderTexture(maskRenderTexture.descriptor);
            this.supportTexture = new RenderTexture(maskRenderTexture.descriptor);
        }
        
        protected override bool ProcessNode(CommandBuffer cmd)
        {
            if (!base.ProcessNode(cmd))
                return false;

            if (mask == null || outMaterial == null)
            {
                outMaterial = new Material(Shader.Find(unlitShader));
                InitializeCrts();
            }

            return true;
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
        
        public void InitializeCrts()
        {
            if (outMaterial == null)
                outMaterial = new Material(Shader.Find(unlitShader));
            
            foreach (var item in crts)
            {
                item.Release();
            }
            
            crts.Clear();
            
            var propList = new List<ShaderPropertyData>();
            for (int i = 0; i < outMaterial.shader.GetPropertyCount(); i++)
                propList.Add(new ShaderPropertyData(outMaterial.shader, i));

            foreach (var item in propList)
            {
                if (item.type == ShaderPropertyType.Texture)
                {
                    if(item.name.Contains("_Detail"))
                        continue;
                    
                    var crt = new CustomRenderTexture(graph.settings.width, graph.settings.height);
                    crt.material = new Material(Shader.Find("Hidden/Mixture/MixtureLerpTexture"));
                    crt.material.SetTexture("_Mask", extendIslandRenderTexture);
                    crt.material.SetTexture("_MatA", extendIslandRenderTexture);
                    crt.material.SetTexture("_MatB", extendIslandRenderTexture);
                    crt.name = item.name;
                    
                    mask = crt;
                    
                    crts.Add(crt);
                }

            }

            foreach (var item in propList)
            {
                if (item.type == ShaderPropertyType.Texture)
                {
                    if (outMaterial.shader.GetPropertyTextureDimension(item.index) != TextureDimension.Tex2D)
                        continue;
                    
                    if(item.name.Contains("_Detail"))
                        continue;
                    
                    Debug.Log("Assign : " + item.name);
                    outMaterial.SetTexture(item.name, crts.Find(x => x.name.Equals(item.name)));
                }
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