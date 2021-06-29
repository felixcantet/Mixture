using System.Collections.Generic;
using System.Linq;
using GraphProcessor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mixture
{
    [Documentation(@"To be filled.")]

    [System.Serializable, NodeMenuItem("Painting/Material Blend - Painting")]
    public class Paint3DNode : MixtureNode, IUseCustomRenderTextureProcessing
    {
        [Input(name = "In Mesh")]
        public Mesh inMesh;

        [Input(name = "Material A")]
        public Material materialA;
        
        [Input(name = "Material B")]
        public Material materialB;
        
        [Output(name = "Out Material")]
        public Material outMaterial; // Je sais pas on out quoi par contre
                                     // Le mat du mesh ? 


        [SerializeField] public RenderTexture maskRenderTexture, extendIslandRenderTexture, uvIslandRenderTexture, supportTexture;
        List<CustomRenderTexture> crts;
        
        public override bool hasSettings => false;
        public override string name => "Material Blend - Painting";

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

        protected override bool ProcessNode(CommandBuffer cmd)
        {
            if (!base.ProcessNode(cmd))
                return false;

            // Convert Mask to Material
            if (materialA == null || materialB == null)
                return false;

            if (!materialA.shader.Equals(materialB.shader))
                return false;
            
            if (outMaterial == null || outMaterial.shader != materialA.shader)
            {
                outMaterial = new Material(materialA.shader);
                InitializeCrts();
            }
            // Assign Texture to material

            
            return true;
        }

        public void InitializeCrts()
        {
            if (materialA == null || materialB == null)
            {
                AddMessage("Paint Node should have 2 materials", NodeMessageType.Error);
                return;
            }
            
            if (outMaterial == null)
                outMaterial = new Material(materialA.shader);
            
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
                    if (outMaterial.shader.GetPropertyTextureDimension(item.index) != TextureDimension.Tex2D)
                        continue;
                    
                    if(item.name.Contains("_Detail"))
                        continue;
                    
                    var crt = new CustomRenderTexture(graph.settings.width, graph.settings.height);
                    crt.material = new Material(Shader.Find("Hidden/Mixture/MixtureLerpTexture"));
                    crt.material.SetTexture("_Mask", extendIslandRenderTexture);
                    crt.material.SetTexture("_MatA", materialA.GetTexture(item.name));
                    crt.material.SetTexture("_MatB", materialB.GetTexture(item.name));
                    crt.name = item.name;
                    
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

