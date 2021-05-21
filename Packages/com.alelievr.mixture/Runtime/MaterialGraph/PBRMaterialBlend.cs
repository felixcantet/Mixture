using System.Collections.Generic;
using UnityEngine;
using GraphProcessor;
using UnityEngine.Rendering;

namespace Mixture
{
    [System.Serializable, NodeMenuItem("Custom/PBR Material Blending")]
    public class PBRMaterialBlend : MixtureNode, IUseCustomRenderTextureProcessing
    {
        [Input("Material A")]
        public Material MaterialA;

        [Input("Material B")]
        public Material MaterialB;

        [Output][ShowInInspector(showInNode = true)]
        public Material output;

        CustomRenderTexture[] shaderInputs;
        List<Material> blendingMaterials;
        Material controlMapMaterial;
        CustomRenderTexture controlMap;
        [ShowInInspector(showInNode = true)]public string heightPropertyName;
        [ShowInInspector(showInNode = true)]public float HeightBlendValue;
        public override string name => "PBR Material Blender";

        protected override void Enable()
        {
            //shaderInputs = new List<CustomRenderTexture>();
            blendingMaterials = new List<Material>();
            if (MaterialA != null)
            {
                output = new Material(Shader.Find(MaterialA.shader.name));
                var props = MaterialA.GetTexturePropertyNames();
                shaderInputs = new CustomRenderTexture[props.Length];
               // shaderInputs.Capacity = props.Length;
                for(int i = 0; i < props.Length; i++)
                {
                    var mat = new Material(Shader.Find("Hidden/Mixture/HeightBlend"));
                    blendingMaterials.Add(mat);
                    shaderInputs[i] = new CustomRenderTexture(graph.outputNode.rtSettings.width, graph.outputNode.rtSettings.height) { material = mat };
                }

            }
        }

        void CreateControlMap()
        {


        }


        protected override void Disable()
        {
        }

        protected override void Destroy()
        {
        }

        protected override bool ProcessNode(CommandBuffer cmd)
        {
            if (!base.ProcessNode(cmd))
                return false;
            var materialTexProps = MaterialA.GetTexturePropertyNames();
            for (int i = 0; i < shaderInputs.Length; i++)
            {
                
                var tex = shaderInputs[i];
                UpdateTempRenderTexture(ref tex);
                // tex.material.SetTexture("_MapA", MaterialA.GetTexture(materialTexProps[i]));
                // tex.material.SetTexture("_MapB", MaterialB.GetTexture(materialTexProps[i]));
                // tex.material.SetTexture("_HeightA", MaterialA.GetTexture("_ParallaxMap"));
                // tex.material.SetTexture("_HeightB", MaterialB.GetTexture("_ParallaxMap"));
                Debug.Log(i + " : " +  MaterialA.GetTexture(materialTexProps[i]));
                MixtureUtils.SetTextureWithDimension(tex.material,"_MapA", MaterialA.GetTexture(materialTexProps[i]));
                MixtureUtils.SetTextureWithDimension(tex.material,"_MapB", MaterialB.GetTexture(materialTexProps[i]));
                MixtureUtils.SetTextureWithDimension(tex.material,"_HeightA", MaterialA.GetTexture(heightPropertyName));
                MixtureUtils.SetTextureWithDimension(tex.material,"_HeightB", MaterialB.GetTexture(heightPropertyName));
                tex.material.SetFloat("_HeightmapBlending", HeightBlendValue);
            }
            
            Debug.Log("Texture Count : " + this.shaderInputs.Length);

            for (int i = 0; i < shaderInputs.Length; i++)
            {
                
                output.SetTexture(materialTexProps[i], shaderInputs[i]); 
            }
            
            Debug.Log("Shader : " + output.shader);
 

            return true;
        }

        Texture GetControlMap()
        {


            return null;
        }
        //
        // [CustomPortOutput(nameof(output), typeof(Material))]
        // void PushOutputs(List<SerializableEdge> edges)
        // {
        //     
        // }
        
        
        public IEnumerable<CustomRenderTexture> GetCustomRenderTextures()
        {
            //yield return controlMap;
            foreach (var item in shaderInputs)
            {
                
                yield return item;
            }

        }
    }
}