using System.Collections.Generic;
using System.Linq;
using GraphProcessor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mixture
{
    [Documentation(@"To be filled.")]

    [System.Serializable, NodeMenuItem("Painting/Paint 3D")]
    public class Paint3DNode : MixtureNode, IUseCustomRenderTextureProcessing
    {
        [Input(name = "In Mesh")]
        public Mesh inMesh;

        [Input(name = "Reference Material")]
        public Material refMat;

        [Input(name = "Materials Palette")]
        public IEnumerable<Material> materialsPalette;

        [Output(name = "Out Material")]
        public Material outMaterial; // Je sais pas on out quoi par contre
                                     // Le mat du mesh ? 


        [SerializeField] public RenderTexture maskRenderTexture, extendIslandRenderTexture, uvIslandRenderTexture, supportTexture;
        List<CustomRenderTexture> crts;
        public override bool hasSettings => false;
        public override string name => "Paint 3D";

        public override void OnNodeCreated()
        {
            base.OnNodeCreated();
            this.maskRenderTexture = new RenderTexture(1024, 1024, 0);
            this.maskRenderTexture.filterMode = FilterMode.Bilinear;
            this.extendIslandRenderTexture = new RenderTexture(maskRenderTexture.descriptor);
            this.uvIslandRenderTexture = new RenderTexture(maskRenderTexture.descriptor);
            this.supportTexture = new RenderTexture(maskRenderTexture.descriptor);
        }

        public int frame = 0;
        
        protected override bool ProcessNode(CommandBuffer cmd)
        {
            if (!base.ProcessNode(cmd))
                return false;

            // Convert Mask to Material
            if (materialsPalette == null || materialsPalette.Count() < 2)
                return false;

            if (outMaterial == null || outMaterial.shader != materialsPalette.First().shader)
            {
                //Debug.Log("Process");
                outMaterial = new Material(materialsPalette.First().shader);
                InitializeCrts();
            }
            //InitializeCrts();

            // for (int i = 0; i < outMaterial.shader.GetPropertyCount(); i++)
            // {
            //     if (outMaterial.shader.GetPropertyType(i) == ShaderPropertyType.Texture)
            //     {
            //     }
            // }

            // frame++;
            //
            // if (frame >= 120)
            // {
            //     frame = 0;
            //     InitializeCrts();
            //     
            //     Debug.Log("Frame updated");
            // }
            
            // Assign Texture to material

            return true;
        }

        void InitializeCrts()
        {
            if (materialsPalette.Count() < 2)
            {
                AddMessage("Paint Node should have 2 materials", NodeMessageType.Error);
                return;
            }
            
            if (outMaterial == null)
                outMaterial = new Material(materialsPalette.First().shader);
            
            ///Debug.Log("Test");
            //for (int i = 0; i < materialsPalette.Count(); i++)
            //{
            //    if (outMaterial.shader != materialsPalette.ElementAt(i).shader)
            //    {
            //        AddMessage($"Material shader {i} is not OKAY", NodeMessageType.Error);
            //        return;
            //    }
            //}

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
                    crt.material.SetTexture("_MatA", materialsPalette.ElementAt(0).GetTexture(item.name));
                    crt.material.SetTexture("_MatB", materialsPalette.ElementAt(1).GetTexture(item.name));
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

        [CustomPortBehavior(nameof(materialsPalette))]
        IEnumerable<PortData> GetPortsForInputs(List<SerializableEdge> edges)
        {
            yield return new PortData { displayName = "In ", displayType = typeof(Material), acceptMultipleEdges = true };
        }

        [CustomPortInput(nameof(materialsPalette), typeof(Material), allowCast = true)]
        public void GetInputs(List<SerializableEdge> edges)
        {
            materialsPalette = edges.Select(e => (Material)e.passThroughBuffer);
        }

        [CustomPortOutput(nameof(outMaterial), typeof(Material))]
        public void MaterialOutputHandler(List<SerializableEdge> edges)
        {
            //outMaterial = refMat; //new Material(Shader.Find("Standard"));
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

