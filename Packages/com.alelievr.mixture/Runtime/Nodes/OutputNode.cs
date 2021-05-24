﻿using System.Collections.Generic;
using UnityEngine;
using GraphProcessor;
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Profiling;
using UnityEngine.Rendering.HighDefinition;

namespace Mixture
{
    [System.Serializable]
    public class OutputNode : MixtureNode, IUseCustomRenderTextureProcessing
    {
        [Input, SerializeField]
        public List<OutputTextureSettings> outputTextureSettings = new List<OutputTextureSettings>();

        public OutputTextureSettings mainOutput => outputTextureSettings[0];

        public event Action onTempRenderTextureUpdated;

		public override string		name => "Output Texture Asset";
		public override Texture 	previewTexture => graph?.type == MixtureGraphType.Realtime ? graph.mainOutputAsset as Texture : outputTextureSettings.Count > 0 ? outputTextureSettings[0].finalCopyRT : null;
		public override float		nodeWidth => 350;

        public override Texture previewTexture
        {
            get
            {
                if (graph.type != MixtureGraphType.Material)
                {
                    return graph.type == MixtureGraphType.Realtime
                        ? graph.mainOutputAsset as Texture
                        : outputTextureSettings.Count > 0
                            ? outputTextureSettings[0].finalCopyRT
                            : null;
                }
                else
                {
#if UNITY_EDITOR
                    return AssetPreview.GetAssetPreview(graph.outputMaterial);
                    
#else
                    return graph.type == MixtureGraphType.Realtime
                        ? graph.mainOutputAsset as Texture
                        : outputTextureSettings.Count > 0
                            ? outputTextureSettings[0].finalCopyRT
                            : null;
#endif
                }
            }
        }

		protected override MixtureSettings defaultSettings
        {
            get => new MixtureSettings()
            {
                sizeMode = OutputSizeMode.Absolute,
				outputChannels = OutputChannel.RGBA,
				outputPrecision = OutputPrecision.Half,
				potSize = POTSize._1024,
                editFlags = EditFlags.POTSize | EditFlags.Width | EditFlags.Height | EditFlags.Depth | EditFlags.Dimension | EditFlags.TargetFormat | EditFlags.SizeMode
            };
        }

        protected override void Enable()
        {
            base.Enable();
			// Checks that the output have always at least one element:
			if (outputTextureSettings.Count == 0)
				AddTextureOutput(OutputTextureSettings.Preset.Color);

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
            this.outputTextureSettings = outputTextureSettings.OrderBy(x =>
                enableParameters.IndexOf(enableParameters.Find(y => y.name == x.name))
            ).ToList();

            this.outputTextureSettings.ForEach(x =>
                Debug.Log($"Index of : {x.name} = {outputTextureSettings.IndexOf(x)}"));


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

			if (graph.mainOutputAsset == null)
		protected override bool ProcessNode(CommandBuffer cmd)
		{
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

                    if (output.finalCopyRT.dimension != rtSettings.GetTextureDimension(graph))
                    {
                        output.finalCopyRT.Release();
                        output.finalCopyRT.depth = 0;
                        output.finalCopyRT.dimension = rtSettings.GetTextureDimension(graph);
                        output.finalCopyRT.Create();
                    }
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
                    SetMaterialPropertiesFromEdges(GetAllEdges().ToList(), graph.outputMaterial);
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

			var input = targetOutput.inputTexture;
			if (input != null)
			{
				if (input.dimension != settings.GetResolvedTextureDimension(graph))
				{
					Debug.LogError("Error: Expected texture type input for the OutputNode is " + settings.GetResolvedTextureDimension(graph) + " but " + input?.dimension + " was provided");
					return false;
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
            //Debug.Log($"Target Output  for {targetOutput.name} : " + targetOutput.inputTexture);
            var input = targetOutput.inputTexture;
            if (input != null)
            {
                if (input.dimension != (TextureDimension) rtSettings.dimension)
                {
                    Debug.LogError("Error: Expected texture type input for the OutputNode is " + rtSettings.dimension +
                                   " but " + input?.dimension + " was provided");
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
		[CustomPortBehavior(nameof(outputTextureSettings))]
		protected IEnumerable< PortData > ChangeOutputPortType(List< SerializableEdge > edges)
		{
			Type displayType = TextureUtils.GetTypeFromDimension(settings.GetResolvedTextureDimension(graph));

        [CustomPortBehavior(nameof(outputTextureSettings))]
        protected IEnumerable<PortData> ChangeOutputPortType(List<SerializableEdge> edges)
        {
            TextureDimension dim = (GetType() == typeof(ExternalOutputNode))
                ? rtSettings.GetTextureDimension(graph)
                : (TextureDimension) rtSettings.dimension;
            Type displayType = TextureUtils.GetTypeFromDimension(dim);
            if (graph.type != MixtureGraphType.Material)
            {
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
            else
            {
                foreach (var output in outputTextureSettings)
                {
                    Debug.Log("Output name = : " + output.name);
                    yield return new PortData
                    {
                        displayName = "", // display name is handled by the port settings UI element
                        displayType = displayType,
                        identifier = output.name,
                    };
                }

                // var mat = graph.outputMaterial;
                // int propCount = mat.shader.GetPropertyCount();
                // for (int i = 0; i < propCount; i++)
                // {
                //     yield return new PortData
                //     {
                //         displayName = mat.shader.GetPropertyName(i),
                //         displayType = TypeFromShaderProperty(mat.shader.GetPropertyType(i), i),
                //         identifier = mat.shader.GetPropertyName(i)
                //     };
                // }
            }
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
                    if (output.inputTexture == null)
                        Debug.Log("Null Texture Input");
                }
                else
                {
                    Debug.Log("Null output : " + output);
                }

                ;
            }
        }

        public IEnumerable<CustomRenderTexture> GetCustomRenderTextures()
        {
            foreach (var output in outputTextureSettings)
            {
                yield return output.finalCopyRT;
            }
        }

        protected void SetMaterialPropertiesFromEdges(List<SerializableEdge> edges, Material material)
        {

            foreach (var item in enableParameters)
            {
                
                if (item.type == ShaderPropertyType.Texture)
                {
                    var defaultValue = material.shader.GetPropertyTextureDefaultName(item.index);
                    material.SetTexture(item.name, defaultValue == "white" ? Texture2D.whiteTexture : Texture2D.blackTexture);
                }

                else if (item.type == ShaderPropertyType.Float)
                {
                    var defaultValue = material.shader.GetPropertyDefaultFloatValue(item.index);
                    material.SetFloat(item.name, defaultValue);
                }
                else
                {
                    var defaultValue = material.shader.GetPropertyDefaultVectorValue(item.index);
                    if(item.type == ShaderPropertyType.Vector)
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