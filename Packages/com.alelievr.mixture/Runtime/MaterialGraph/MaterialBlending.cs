using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GraphProcessor;
using UnityEngine.Rendering;

namespace Mixture
{
    [System.Serializable, NodeMenuItem("Custom/Material Blending")]
    public class MaterialBlending : BaseMaterialNode, IUseCustomRenderTextureProcessing
    {
        public enum BlendMode
        {
            Height,
            CustomMask
        }

        [Input] public MixtureMaterial materialA;

        [Input] public MixtureMaterial materialB;

        [Input] public Texture heightMaterialA;
        [Input] public Texture heightMaterialB;
        [Input] public Texture customMask;

        [Output] public MixtureMaterial output;
        [Output] public CustomRenderTexture mask;

        [SerializeField] public BlendMode blendMode;
        [SerializeField] public bool useThreshold;
        [SerializeField] [Range(0, 1)] public float threshold;
        [SerializeField] [Range(0.001f, 4.0f)] public float blendAmount;
        private Dictionary<string, Material> blendMaterials; // (propertyName, Material)
        private Dictionary<string, CustomRenderTexture> processResult; // (propertyName, Material)

        public override bool showDefaultInspector => true;

        public override string name => "MaterialBlending";

        public override Material previewMaterial
        {
            get
            {
                if (output == null)
                {
                    return null;
                }

                return output.material;
            }
        }

        protected override void Enable()
        {
        }

        protected override bool ProcessNode(CommandBuffer cmd)
        {
            if (!base.ProcessNode(cmd))
                return false;
            ClearMessages();
            if (materialA == null || materialB == null)
                return false;
            if (materialA.shader != materialB.shader)
            {
                AddMessage("Both materials have to be of same shader", NodeMessageType.Error);
                return false;
            }


            if (blendMode == BlendMode.Height)
            {
                if (heightMaterialA == null || heightMaterialB == null)
                {
                    AddMessage("You have to provide two Height Map", NodeMessageType.Error);
                    return false;
                }
            }
            else if (blendMode == BlendMode.CustomMask)
            {
                if (this.customMask == null)
                {
                    AddMessage("You have to provide a Mask", NodeMessageType.Error);
                    return false;
                }
            }

            var targetShader = materialA.shader;
            if (output == null)
            {
                output = new MixtureMaterial(targetShader);
                GenerateBlendMaterials();
            }
            else if (output.shader != targetShader)
            {
                output = new MixtureMaterial(targetShader);
                GenerateBlendMaterials();
            }

            if (processResult == null || processResult.Count == 0)
            {
                GenerateBlendMaterials();
            }

            // GenerateBlendMaterials();

            foreach (var item in processResult)
            {
                var propName = item.Key;
                var crt = item.Value;


                UpdateTempRenderTexture(ref crt);
                MixtureUtils.SetTextureWithDimension(crt.material, "_MapA",
                    materialA.material.GetTexture(propName));
                MixtureUtils.SetTextureWithDimension(crt.material, "_MapB",
                    materialB.material.GetTexture(propName));
                if (blendMode == BlendMode.Height)
                {
                    MixtureUtils.SetTextureWithDimension(crt.material, "_HeightA", heightMaterialA);
                    MixtureUtils.SetTextureWithDimension(crt.material, "_HeightB", heightMaterialB);
                    crt.material.SetFloat("_HeightThreshold", threshold);
                    crt.material.SetFloat("_HeightmapBlending", blendAmount);
                    
                    if (useThreshold)
                    {
                        crt.material.EnableKeyword("USE_THRESHOLD");
                        if (propName.Contains("Height"))
                        {
                            crt.material.EnableKeyword("IS_HEIGHTMAP");
                        }
                        else
                        {
                            crt.material.DisableKeyword("IS_HEIGHTMAP");
                        }
                    }
                    else
                        crt.material.DisableKeyword("USE_THRESHOLD");
                }
                else if (blendMode == BlendMode.CustomMask)
                {
                    MixtureUtils.SetTextureWithDimension(crt.material, "_Mask", this.customMask);
                }
            }

            // Insert your code here 

            return true;
        }

        public void GenerateBlendMaterials()
        {
            blendMaterials = new Dictionary<string, Material>();
            if (processResult != null)
            {
                foreach (var customRenderTexture in processResult.Values)
                {
                    customRenderTexture.Release();
                }

                processResult.Clear();
            }
            else
                processResult = new Dictionary<string, CustomRenderTexture>();

            foreach (var item in materialA.material.GetTexturePropertyNames())
            {
                if (materialA.material.GetTexture(item) == null ||
                    materialB.material.GetTexture(item) == null)
                    continue;
                bool isNormalMap = false;
                // Naive NormalMap identification
                if (item.Contains("Normal") || item.Contains("Bump"))
                {
                    isNormalMap = true;
                }

                // Create Materials
                if (blendMode == BlendMode.Height)
                {
                    var mat = new Material(Shader.Find("Hidden/Mixture/HeightBlend"));
                    blendMaterials.Add(item, mat);
                    // Create CRT
                    processResult.Add(item, new CustomRenderTexture(graph.settings.GetResolvedWidth(graph),
                        graph.settings.GetResolvedHeight(graph))
                    {
                        material = mat
                    });
                }
                else
                {
                    var mat = new Material(Shader.Find("Hidden/Mixture/BlendTextureByMask"));
                    blendMaterials.Add(item, mat);
                    processResult.Add(item, new CustomRenderTexture(graph.settings.GetResolvedWidth(graph),
                        graph.settings.GetResolvedHeight(graph))
                    {
                        material = mat
                    });
                }
            }
        }


        [CustomPortOutput(nameof(output), typeof(MixtureMaterial))]
        void AssignOutput(List<SerializableEdge> edges)
        {
            if (materialA == null || materialB == null)
                return;
            if (materialA.shader != materialB.shader)
                return;
            if (materialA.material == null || materialB.material == null)
                return;
            output = new MixtureMaterial(materialA, true);
            foreach (var item in output.material.GetTexturePropertyNames())
            {
                if (processResult.ContainsKey(item))
                {
                    output.material.SetTexture(item, processResult[item]);
                }
                else
                {
                    var texA = materialA.material.GetTexture(item);
                    var texB = materialB.material.GetTexture(item);
                    if (texA != null)
                    {
                        output.material.SetTexture(item, texA);
                    }
                    else if (texB != null)
                    {
                        output.material.SetTexture(item, texB);
                    }
                }
            }

            foreach (var item in output.shaderProperties)
            {
                // Blend other values
                if (item.type == ShaderPropertyType.Texture)
                    continue;

                else if (item.type == ShaderPropertyType.Color)
                {
                    var colA = materialA.material.GetColor(item.name);
                    var colB = materialB.material.GetColor(item.name);
                    output.material.SetColor(item.name, Color.Lerp(colA, colB, 0.5f));
                    continue;
                }
                else if (item.type == ShaderPropertyType.Color)
                {
                    var vecA = materialA.material.GetVector(item.name);
                    var vecB = materialB.material.GetVector(item.name);
                    output.material.SetVector(item.name, Vector4.Lerp(vecA, vecB, 0.5f));
                    continue;
                }
                else if (item.type == ShaderPropertyType.Float || item.type == ShaderPropertyType.Range)
                {
                    var fA = materialA.material.GetFloat(item.name);
                    var fB = materialB.material.GetFloat(item.name);
                    output.material.SetFloat(item.name, Mathf.Lerp(fA, fB, 0.5f));
                    continue;
                }
            }

            foreach (var item in edges)
            {
                item.passThroughBuffer = output as MixtureMaterial;
            }
        }

        #region Display Port

        [CustomPortBehavior(nameof(customMask))]
        IEnumerable<PortData> ShowMaskInput(List<SerializableEdge> edges)
        {
            if (this.blendMode == BlendMode.CustomMask)
                yield return new PortData()
                {
                    displayName = "Mask",
                    displayType = typeof(Texture2D),
                    identifier = "customMask"
                };

            yield break;
        }

        [CustomPortBehavior(nameof(heightMaterialA))]
        IEnumerable<PortData> ShowHeight1Input(List<SerializableEdge> edges)
        {
            if (this.blendMode == BlendMode.Height)
                yield return new PortData()
                {
                    displayName = "Height Material A",
                    displayType = typeof(Texture2D),
                    identifier = "HeightA"
                };

            yield break;
        }

        [CustomPortBehavior(nameof(heightMaterialB))]
        IEnumerable<PortData> ShowHeight2Input(List<SerializableEdge> edges)
        {
            if (this.blendMode == BlendMode.Height)
                yield return new PortData()
                {
                    displayName = "Height Material B",
                    displayType = typeof(Texture2D),
                    identifier = "HeightB"
                };

            yield break;
        }

        #endregion


        protected override void Disable()
        {
            base.Disable();
        }


        public IEnumerable<CustomRenderTexture> GetCustomRenderTextures()
        {
            if (processResult == null)
                yield break;
            foreach (var item in processResult)
            {
                yield return item.Value;
            }
        }
    }
}