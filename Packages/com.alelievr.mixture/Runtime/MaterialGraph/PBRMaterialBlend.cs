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

        [Output]
        public Material output;

        CustomRenderTexture[] shaderInputs;
        List<Material> blendingMaterials;
        Material controlMapMaterial;
        CustomRenderTexture controlMap;

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
                    var mat = new Material(Shader.Find("Hidden/Mixture/ColorMatte"));
                    mat.SetColor("_Color", Color.red);
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
            for (int i = 0; i < shaderInputs.Length; i++)
            {
                var tex = shaderInputs[i];
                UpdateTempRenderTexture(ref tex);
                tex.material.color = Color.blue;
            }
            Debug.Log("Texture Count : " + this.shaderInputs.Length);

            output.SetTexture("_MainTex", this.shaderInputs[0]);



            return true;
        }

        Texture GetControlMap()
        {


            return null;
        }


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