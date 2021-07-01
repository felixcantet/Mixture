using System.Collections;
using System.Collections.Generic;
using GraphProcessor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mixture
{
    [Documentation(@"To be filled.")]
    
    [System.Serializable, NodeMenuItem("Painting/Texture Painting")]
    public class Paint2DNode : PaintNode
    {
        [Input(name = "Texture Reference")]
        public Texture refTexture = null;
        
        [Output(name = "Out Mask")]
        public Texture mask = null;


        public override string name => "Texture Painting";
        
        public Material outMaterial = null;
        private const string unlitShader = "Unlit/Texture";
        
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
        
        public override void InitializeCrts()
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
    }
}