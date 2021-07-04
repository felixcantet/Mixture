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
    public class Paint3DNode : PaintNode
    {
        [Input(name = "In Mesh")]
        public Mesh inMesh;

        [Input(name = "Material A")]
        public MixtureMaterial materialA = null;
        
        [Input(name = "Material B")]
        public MixtureMaterial materialB = null;
        
        [Output(name = "Out Material")]
        public MixtureMaterial outMaterial;

        public override string name => "Material Blend - Painting";
        

        protected override bool ProcessNode(CommandBuffer cmd)
        {
            if (!base.ProcessNode(cmd))
                return false;

            if (materialA == null || materialB == null)
                return false;

            if (materialA.material == null || materialB.material == null)
                return false;
            
            if (!materialA.shader.Equals(materialB.shader))
                return false;
            
            if (outMaterial == null || outMaterial.shader != materialA.shader)
            {
                outMaterial = new MixtureMaterial(materialA.shader);//new Material(materialA.shader);
                InitializeCrts();
            }
            
            return true;
        }

        public override void InitializeCrts()
        {
            if (materialA == null || materialB == null)
            {
                AddMessage("Paint Node should have 2 materials", NodeMessageType.Error);
                return;
            }
            
            if (outMaterial == null)
                outMaterial = new MixtureMaterial(materialA.shader);//new Material(materialA.shader);
            
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
                    crt.material.SetTexture("_MatA", materialA.material.GetTexture(item.name));
                    crt.material.SetTexture("_MatB", materialB.material.GetTexture(item.name));
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
                    outMaterial.material.SetTexture(item.name, crts.Find(x => x.name.Equals(item.name)));
                }
            }
        }
    }
}

