using System.Collections.Generic;
using UnityEngine;
using GraphProcessor;
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Profiling;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.Universal;

namespace Mixture
{
    [System.Serializable]
    public class ShaderPropertyData
    {
        public int index;
        public string name;
        public string description;
        public ShaderPropertyType type;
        public TextureDimension dimension;
        public bool displayInOutput = false;
        public object defaultValue;
        public ShaderPropertyData(Shader shader, int index)
        {
            this.index = index;
            this.name = shader.GetPropertyName(index);
            this.description = shader.GetPropertyDescription(index);
            this.type = shader.GetPropertyType(index);
            if (this.type == ShaderPropertyType.Texture)
            {
                this.dimension = shader.GetPropertyTextureDimension(index);
            }

            var flag = shader.GetPropertyFlags(index);
            if ((flag & ShaderPropertyFlags.MainTexture) != 0 ||
                (flag & ShaderPropertyFlags.Normal) != 0 ||
                (flag & ShaderPropertyFlags.MainColor) != 0 ||
                (flag & ShaderPropertyFlags.MainTexture) != 0)
            {
                this.displayInOutput = true;
            }
            else
            {
                this.displayInOutput = false;
            }

            if ((flag & ShaderPropertyFlags.HideInInspector) != 0)
                this.displayInOutput = false;
        }
    }


    [System.Serializable]
    public class OutputNode : MixtureNode, IUseCustomRenderTextureProcessing
    {
        [Input, SerializeField]
        public List<OutputTextureSettings> outputTextureSettings = new List<OutputTextureSettings>();

        [Input, SerializeField] public List<object> shaderParameters = new List<object>();

        public OutputTextureSettings mainOutput => outputTextureSettings[0];

        public event Action onTempRenderTextureUpdated;

        public override string name => "Output Texture Asset";

        public override Texture previewTexture => graph?.type == MixtureGraphType.Realtime
            ? graph.mainOutputAsset as Texture
            : outputTextureSettings.Count > 0
                ? outputTextureSettings[0].finalCopyRT
                : null;

        public override float nodeWidth => 350;

        [NonSerialized] protected HashSet<string> uniqueMessages = new HashSet<string>();

        [HideInInspector] public List<ShaderPropertyData> enableParameters = new List<ShaderPropertyData>();

        protected override MixtureSettings defaultSettings
        {
            get => new MixtureSettings()
            {
                sizeMode = OutputSizeMode.Absolute,
                outputChannels = OutputChannel.RGBA,
                outputPrecision = OutputPrecision.Half,
                potSize = POTSize._1024,
                editFlags = EditFlags.POTSize | EditFlags.Width | EditFlags.Height | EditFlags.Depth |
                            EditFlags.Dimension | EditFlags.TargetFormat | EditFlags.SizeMode
            };
        }

        protected override void Enable()
        {
            base.Enable();
            // Checks that the output have always at least one element:
            if (outputTextureSettings.Count == 0)
                AddTextureOutput(OutputTextureSettings.Preset.Color);

            // Sanitize main texture value:
            if (graph.type != MixtureGraphType.Material)
            {
                if (outputTextureSettings.Count((o => o.isMain)) != 1)
                {
                    outputTextureSettings.ForEach(o => o.isMain = false);
                    outputTextureSettings.First().isMain = true;
                }
            }
            
            // Initialize preview
            Debug.Log(this.graph.previewOutputMaterial.shader);
        }

        public void BuildOutputFromShaderProperties()
        {
            Debug.Log("Build Output");
            for (int i = 0; i < enableParameters.Count; i++)
            {
                if (enableParameters[i].type != ShaderPropertyType.Texture)
                    continue;
                // If the parameter is currently display
                if (outputTextureSettings.Any(x => x.name == enableParameters[i].name))
                {
                    if (!enableParameters[i].displayInOutput)
                    {
                        RemoveTextureOutput(outputTextureSettings.Find(x => x.name == enableParameters[i].name));
                    }
                }
                else
                {
                    if (enableParameters[i].displayInOutput)
                    {
                        var output = AddTextureOutput(OutputTextureSettings.Preset.Color);
                        output.name = enableParameters[i].name;
                        
                    }
                }
            }

            // Sort Output Settings by index of parameters list
            //this.outputTextureSettings = outputTextureSettings.OrderBy(x =>
            //    enableParameters.IndexOf(enableParameters.Find(y => y.name == x.name))
            //).ToList();

            //this.outputTextureSettings.ForEach(x =>
            //    Debug.Log($"Index of : {x.name} = {outputTextureSettings.IndexOf(x)}"));


            // //this.outputTextureSettings.ForEach(x => this.RemoveTextureOutput(x));
            // for (int i = 0; i < outputTextureSettings.Count; i++)
            // {
            //     Debug.Log(outputTextureSettings[i].name);
            //     this.RemoveTextureOutput(this.outputTextureSettings[i]);
            // }
            //
            // //this.outputTextureSettings.Clear();
            // var shader = graph.outputMaterial.shader;
            // var propCount = shader.GetPropertyCount();
            // if (propCount != enableParameters.Count)
            // {
            //     Debug.LogWarning("Enable Properties List don't match with current shader");
            //     return;
            // }
            //
            // for (int i = 0; i < propCount; i++)
            // {
            //     if (enableParameters[i].displayInOutput)
            //     {
            //         if (enableParameters[i].type == ShaderPropertyType.Texture)
            //         {
            //             var output = AddTextureOutput(OutputTextureSettings.Preset.Color);
            //             output.name = name;
            //         }
            //     }
            // }
        }

        // Disable reset on output texture settings
        protected override bool CanResetPort(NodePort port) => false;

        // TODO: output texture setting presets when adding a new output

        public OutputTextureSettings AddTextureOutput(OutputTextureSettings.Preset preset)
        {
            var output = new OutputTextureSettings
            {
                inputTexture = null,
                name = $"Input {outputTextureSettings?.Count + 1}",
                finalCopyMaterial = CreateFinalCopyMaterial(),
            };

            if (graph.type == MixtureGraphType.Realtime)
                output.finalCopyRT = graph.mainOutputAsset as CustomRenderTexture;

            // output.finalCopyRT can be null here if the graph haven't been imported yet
            if (output.finalCopyRT != null)
                output.finalCopyRT.material = output.finalCopyMaterial;

            // Try to guess the correct setup for the user
#if UNITY_EDITOR
            var names = outputTextureSettings.Select(o => o.name).Concat(new List<string> {graph.mainOutputAsset.name})
                .ToArray();
            output.SetupPreset(preset, (name) => UnityEditor.ObjectNames.GetUniqueName(names, name));
#endif
            if (graph.type != MixtureGraphType.Material)
            {
                // Output 0 is always Main Texture
                if (outputTextureSettings.Count == 0)
                {
                    output.name = "Main Texture";
                    output.isMain = true;
                }
            }

            outputTextureSettings.Add(output);

#if UNITY_EDITOR
            if (graph.type == MixtureGraphType.Realtime)
                graph.UpdateRealtimeAssetsOnDisk();
#endif

            return output;
        }

        public void RemoveTextureOutput(OutputTextureSettings settings)
        {
            outputTextureSettings.Remove(settings);

#if UNITY_EDITOR
            // When the graph is realtime, we don't have the save all button, so we call is automatically
            if (graph.type == MixtureGraphType.Realtime)
                graph.UpdateRealtimeAssetsOnDisk();
#endif
        }

        Material CreateFinalCopyMaterial()
        {
            var finalCopyShader = Shader.Find("Hidden/Mixture/FinalCopy");

            if (finalCopyShader == null)
            {
                Debug.LogError("Can't find Hidden/Mixture/FinalCopy shader");
                return null;
            }

            return new Material(finalCopyShader) {hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector};
        }

        protected override void Disable()
        {
            base.Disable();

            foreach (var output in outputTextureSettings)
            {
                if (graph != null && graph.type != MixtureGraphType.Realtime)
                    CoreUtils.Destroy(output.finalCopyRT);
            }
        }


        protected override bool ProcessNode(CommandBuffer cmd)
        {
            if (graph == null)
                return false;

            if (graph.mainOutputAsset == null)
            {
                Debug.LogError("Output Node can't write to target texture, Graph references a null output texture");
                return false;
            }

            UpdateMessages();

            foreach (var output in outputTextureSettings)
            {
                // Update the renderTexture reference for realtime graph
                if (graph.type == MixtureGraphType.Realtime)
                {
                    var finalCopyRT = graph.FindOutputTexture(output.name, output.isMain) as CustomRenderTexture;
                    if (finalCopyRT != null && output.finalCopyRT != finalCopyRT)
                        onTempRenderTextureUpdated?.Invoke();
                    output.finalCopyRT = finalCopyRT;

                    UpdateTempRenderTexture(ref output.finalCopyRT, output.hasMipMaps, hideAsset: false);
                    output.finalCopyRT.material = null;

                    // Only the main output CRT is marked as realtime because it's processing will automatically
                    // trigger the processing of it's graph, and thus all the outputs in the graph.
                    if (output.isMain)
                        output.finalCopyRT.updateMode = CustomRenderTextureUpdateMode.Realtime;
                    else
                        output.finalCopyRT.updateMode = CustomRenderTextureUpdateMode.OnDemand;

                    // Sync output texture properties:
                    output.finalCopyRT.wrapMode = settings.GetResolvedWrapMode(graph);
                    output.finalCopyRT.filterMode = settings.GetResolvedFilterMode(graph);
                    output.finalCopyRT.hideFlags = HideFlags.None;
                }
                else
                {
                    // Update the renderTexture size and format:
                    if (UpdateTempRenderTexture(ref output.finalCopyRT, output.hasMipMaps))
                        onTempRenderTextureUpdated?.Invoke();
                }

                if (!UpdateFinalCopyMaterial(output))
                    continue;

                bool inputHasMips = output.inputTexture != null && output.inputTexture.mipmapCount > 1;
                CustomTextureManager.crtExecInfo[output.finalCopyRT] = new CustomTextureManager.CustomTextureExecInfo
                {
                    runOnAllMips = inputHasMips
                };

                // The CustomRenderTexture update will be triggered at the begining of the next frame so we wait one frame to generate the mipmaps
                // We need to do this because we can't generate custom mipMaps with CustomRenderTextures

                if (output.hasMipMaps && !inputHasMips)
                {
                    // TODO: check if input has mips and copy them instead of overwritting them.
                    cmd.GenerateMips(output.finalCopyRT);
                }

                if (graph.type == MixtureGraphType.Material)
                {
                    //SetMaterialPropertiesFromEdges(GetAllEdges().ToList(), graph.outputMaterial);
                    SetMaterialPropertiesFromEdges(GetAllEdges().ToList(), graph.previewOutputMaterial);
                    
                }
            }

            return true;
        }

        void UpdateMessages()
        {
            if (inputPorts.All(p => p?.GetEdges()?.Count == 0))
            {
                if (uniqueMessages.Add("OutputNotConnected"))
                    AddMessage("Output node input is not connected", NodeMessageType.Warning);
            }
            else
            {
                uniqueMessages.Clear();
                ClearMessages();
            }
        }

        bool UpdateFinalCopyMaterial(OutputTextureSettings targetOutput)
        {
            if (targetOutput.finalCopyMaterial == null)
            {
                targetOutput.finalCopyMaterial = CreateFinalCopyMaterial();
                if (!graph.IsObjectInGraph(targetOutput.finalCopyMaterial))
                    graph.AddObjectToGraph(targetOutput.finalCopyMaterial);
            }

            // Manually reset all texture inputs
            ResetMaterialPropertyToDefault(targetOutput.finalCopyMaterial, "_Source_2D");
            ResetMaterialPropertyToDefault(targetOutput.finalCopyMaterial, "_Source_3D");
            ResetMaterialPropertyToDefault(targetOutput.finalCopyMaterial, "_Source_Cube");

            var input = targetOutput.inputTexture;
            if (input != null)
            {
                if (input.dimension != settings.GetResolvedTextureDimension(graph))
                {
                    Debug.LogError("Error: Expected texture type input for the OutputNode is " +
                                   settings.GetResolvedTextureDimension(graph) + " but " + input?.dimension +
                                   " was provided");
                    return false;
                }

                MixtureUtils.SetupDimensionKeyword(targetOutput.finalCopyMaterial, input.dimension);

                if (input.dimension == TextureDimension.Tex2D)
                    targetOutput.finalCopyMaterial.SetTexture("_Source_2D", input);
                else if (input.dimension == TextureDimension.Tex3D)
                    targetOutput.finalCopyMaterial.SetTexture("_Source_3D", input);
                else
                    targetOutput.finalCopyMaterial.SetTexture("_Source_Cube", input);

                targetOutput.finalCopyMaterial.SetInt("_IsSRGB", targetOutput.sRGB ? 1 : 0);
            }

            if (targetOutput.finalCopyRT != null)
                targetOutput.finalCopyRT.material = targetOutput.finalCopyMaterial;

            return true;
        }

        [CustomPortInput(nameof(shaderParameters), typeof(object))]
        protected void SetOutputType(List<SerializableEdge> edges)
        {
            foreach (var item in edges)
            {
                var prop = enableParameters.FirstOrDefault(x => x.name == item.inputPortIdentifier);
                if (prop != null)
                {
                    var type = GetTypeFromShaderProperty(prop);
                    if(prop.type == ShaderPropertyType.Color)
                        item.passThroughBuffer = (Color)item.passThroughBuffer;
                    if(prop.type == ShaderPropertyType.Vector)
                        item.passThroughBuffer = (Vector4)item.passThroughBuffer;
                    if(prop.type == ShaderPropertyType.Float)
                        item.passThroughBuffer = (float)item.passThroughBuffer;
                }
            }
        }

        [CustomPortBehavior(nameof(outputTextureSettings))]
        protected IEnumerable<PortData> ChangeOutputPortType(List<SerializableEdge> edges)
        {
            Type displayType = TextureUtils.GetTypeFromDimension(settings.GetResolvedTextureDimension(graph));

            foreach (var output in outputTextureSettings)
            { 
                yield return new PortData
                {
                    displayName = "", // display name is handled by the port settings UI element
                    displayType = displayType,
                    identifier = output.name,
                };
            }
        }

        [CustomPortBehavior(nameof(shaderParameters))]
        protected IEnumerable<PortData> GetShaderParametersPorts(List<SerializableEdge> edges)
        {
            foreach (var item in enableParameters)
            {
                if (item.type == ShaderPropertyType.Texture)
                    continue;

                if (!item.displayInOutput)
                    continue;

                yield return new PortData
                {
                    displayName = item.description + " (" + item.name + ")",
                    displayType = GetTypeFromShaderProperty(item),
                    identifier = item.name
                };
            }
        }
        // TODO : Put it in utils
        public Type GetTypeFromShaderProperty(ShaderPropertyData propertyData)
        {
            if (propertyData.type == ShaderPropertyType.Color)
                return typeof(Color);
            if (propertyData.type == ShaderPropertyType.Float)
                return typeof(float);
            if (propertyData.type == ShaderPropertyType.Vector)
                return typeof(Vector4);
            if (propertyData.type == ShaderPropertyType.Range)
                return typeof(float);
            if (propertyData.type == ShaderPropertyType.Texture)
                return typeof(Texture2D);
            return typeof(object);
        }

        Type TypeFromShaderProperty(ShaderPropertyType type, int index)
        {
            if (type == ShaderPropertyType.Texture)
            {
                var dim = graph.outputMaterial.shader.GetPropertyTextureDimension(index);
                if (dim == TextureDimension.Tex2D)
                    return typeof(Texture2D);
                else if (dim == TextureDimension.Tex3D)
                    return typeof(Texture3D);
                else
                {
                    return typeof(Cubemap);
                }
            }
            else if (type == ShaderPropertyType.Float)
                return typeof(float);
            else if (type == ShaderPropertyType.Color)
                return typeof(Color);
            else if (type == ShaderPropertyType.Vector)
                return typeof(Vector4);

            return typeof(int);
        }

        [CustomPortInput(nameof(outputTextureSettings), typeof(Texture))]
        protected void AssignSubTextures(List<SerializableEdge> edges)
        {
            foreach (var edge in edges)
            {
                // Find the correct output texture:
                var output = outputTextureSettings.Find(o => o.name == edge.inputPort.portData.identifier);

                if (output != null)
                {
                    output.inputTexture = edge.passThroughBuffer as Texture;
                }
            }
        }

        public IEnumerable<CustomRenderTexture> GetCustomRenderTextures()
        {
            foreach (var output in outputTextureSettings)
            {
                yield return output.finalCopyRT;
            }
        }

        public void UpdatePropertyList()
        {
            var newList = new List<ShaderPropertyData>();
            bool update = false;
            var count = graph.outputMaterial.shader.GetPropertyCount();
            if (enableParameters.Count != count)
                update = true;
            else
            {
                for (int i = 0; i < count; i++)
                {
                    if (enableParameters[i].name != graph.outputMaterial.shader.GetPropertyName(i))
                    {
                        update = true;
                        break;
                    }
                }
            }

            if (!update)
                return;
            
            
            for (int i = 0; i < count; i++)
            {
                ShaderPropertyData data = new ShaderPropertyData(graph.outputMaterial.shader, i);
                newList.Add(data);
            }

            foreach (var item in newList)
            {
                var oldData = enableParameters.Where(x => x.name == item.name).FirstOrDefault();
                if (oldData != null)
                {
                    item.displayInOutput = oldData.displayInOutput;
                }
            }

            enableParameters = newList;

        }

        public void SetMaterialPropertiesFromEdges(List<SerializableEdge> edges, Material material)
        {
            UpdatePropertyList();
            foreach (var item in enableParameters)
            {
                if (!item.displayInOutput)
                {
                    if (item.type == ShaderPropertyType.Texture)
                        material.SetTexture(item.name, null);
                    continue;
                }
                //ResetMaterialPropertyToDefault(graph.outputMaterial, item.name);
                if (item.type == ShaderPropertyType.Texture)
                {
                    var defaultValue = material.shader.GetPropertyTextureDefaultName(item.index);
                    material.SetTexture(item.name,
                        defaultValue == "white" ? Texture2D.whiteTexture : Texture2D.blackTexture);
                }

                else if (item.type == ShaderPropertyType.Float)
                {
                    var defaultValue = material.shader.GetPropertyDefaultFloatValue(item.index);
                    material.SetFloat(item.name, defaultValue);
                }
                else if (item.type == ShaderPropertyType.Color || item.type == ShaderPropertyType.Vector)
                {

                    //Debug.Log(item.type);
                    var defaultValue = material.shader.GetPropertyDefaultVectorValue(item.index);
                    if (item.type == ShaderPropertyType.Vector)
                        material.SetVector(item.name, defaultValue);
                    else
                    {
                        material.SetColor(item.name, defaultValue);
                    }
                }
            }


            // Update material settings when processing the graph:
            foreach (var edge in edges)
            {
                // Just in case something bad happened in a node
                if (edge.passThroughBuffer == null)
                    continue;

                string propName = edge.inputPort.portData.identifier;
                int propertyIndex = material.shader.FindPropertyIndex(propName);

                if (propertyIndex == -1)
                    continue;

                switch (material.shader.GetPropertyType(propertyIndex))
                {
                    case ShaderPropertyType.Color:
                        material.SetColor(propName, MixtureConversions.ConvertObjectToColor(edge.passThroughBuffer));
                        break;
                    case ShaderPropertyType.Texture:
                        // TODO: texture scale and offset
                        // Check texture dim before assigning:
                        if (edge.passThroughBuffer is Texture t && t != null)
                        {
                            var tex = graph.FindOutputTexture(edge.inputPortIdentifier, true);
                            if (material.shader.GetPropertyTextureDimension(propertyIndex) == t.dimension)
                                material.SetTexture(propName, tex == null ? t : tex);
                        }

                        break;
                    case ShaderPropertyType.Float:
                    case ShaderPropertyType.Range:
                        switch (edge.passThroughBuffer)
                        {
                            case float f:
                                material.SetFloat(propName, f);
                                break;
                            case Vector2 v:
                                material.SetFloat(propName, v.x);
                                break;
                            case Vector3 v:
                                material.SetFloat(propName, v.x);
                                break;
                            case Vector4 v:
                                material.SetFloat(propName, v.x);
                                break;
                            case int i:
                                material.SetFloat(propName, i);
                                break;
                            default:
                                throw new Exception(
                                    $"Can't assign {edge.passThroughBuffer.GetType()} to material float property");
                        }

                        break;
                    case ShaderPropertyType.Vector:
                        material.SetVector(propName, MixtureConversions.ConvertObjectToVector4(edge.passThroughBuffer));
                        break;
                }
            }
        }
    }
}