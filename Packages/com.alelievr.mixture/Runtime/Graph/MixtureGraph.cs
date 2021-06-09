﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GraphProcessor;
using System.Linq;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System;
using Object = UnityEngine.Object;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace Mixture
{
    public enum MixtureGraphType
    {
        Baked,
        Realtime,
        Behaviour,
        Material,
    }

    public enum NodeInheritanceMode
    {
        InheritFromGraph = -1,
        InheritFromParent = -2,
        InheritFromChild = -3,
    }

    [System.Serializable]
    public class MixtureGraph : BaseGraph
    {
        public enum Version
        {
            Initial,
            SettingsRefactor,
        }

        public Version version = MixtureUtils.GetLastEnumValue<Version>();

        public MixtureGraphType type = MixtureGraphType.Baked;

        // Serialized data for the editor:
        public bool realtimePreview;

        // Whether or not the mixture is realtime
        [SerializeField, Obsolete("Use type instead.")]
        bool isRealtime;

        public bool isParameterViewOpen;

        /// <summary>
        /// Add the mixture asset to the built player, note that it only works for static mixtures as realtime ones are always included.
        /// </summary>
        public bool embedInBuild;

        public NodeInheritanceMode defaultNodeInheritanceMode = NodeInheritanceMode.InheritFromParent;

        [System.NonSerialized] OutputNode _outputNode;

        public OutputNode outputNode
        {
            get
            {
// In the editor we don't want to cache the wrong output node.
#if !UNITY_EDITOR
                if (_outputNode == null)
#endif
                _outputNode = nodes.FirstOrDefault(n => n is OutputNode) as OutputNode;

                return _outputNode;
            }
            internal set => _outputNode = value;
        }

        [SerializeField] List<Object> objectReferences = new List<Object>();

        [SerializeField] internal List<MixtureVariant> variants = new List<MixtureVariant>();

        [SerializeField, FormerlySerializedAs("_outputTexture")]
        Object _mainOutputAsset;

        public Object mainOutputAsset
        {
            get
            {
#if UNITY_EDITOR
                if (_mainOutputAsset == null)
                    _mainOutputAsset = AssetDatabase.LoadAssetAtPath<Object>(mainAssetPath);
#endif
                return _mainOutputAsset;
            }
            set
            {
                if (_mainOutputAsset is Texture texture)
                {
                    outputTextures.Remove(texture);
                    outputTextures.Add(value as Texture);
                }
                else
                {
                    _mainOutputAsset = outputMaterial;
                }

                _mainOutputAsset = value;
            }
        }

        [SerializeField] private Material _previewOutputMaterial;

        public Material previewOutputMaterial
        {
            get
            {
                if (_previewOutputMaterial == null)
                {
                    if (GraphicsSettings.renderPipelineAsset == null)
                    {
                        _previewOutputMaterial = new Material(Shader.Find("Custom/SimpleShader"));
                    }
                    else
                    {
                        _previewOutputMaterial = new Material(GraphicsSettings.renderPipelineAsset.defaultMaterial);
                    }
                    //this.AddObjectToGraph(_previewOutputMaterial);
                }

                return _previewOutputMaterial;
            }
        }

        [SerializeField] private Material _outputMaterial;

        public Material outputMaterial
        {
            get
            {
                if (_outputMaterial == null)
                {
                    if (GraphicsSettings.renderPipelineAsset == null)
                    {
                        _outputMaterial = new Material(Shader.Find("Custom/SimpleShader"));
                        mainOutputAsset = _outputMaterial;
                    }
                    else
                    {
                        _outputMaterial = new Material(GraphicsSettings.renderPipelineAsset.defaultMaterial);
                    }
                }

                return _outputMaterial;
            }
        }


        // Important: note that order is not guaranteed 
        [SerializeField] List<Texture> _outputTextures = null;

        public List<Texture> outputTextures
        {
            get
            {
#if UNITY_EDITOR
                if (_outputTextures == null || _outputTextures.Count == 0)
                    _outputTextures = AssetDatabase.LoadAllAssetsAtPath(mainAssetPath).OfType<Texture>().ToList();
#endif
                _outputTextures.RemoveAll(t => t == null);

                return _outputTextures;
            }
        }

        public string mainAssetPath
        {
            get
            {
#if UNITY_EDITOR

                return AssetDatabase.GetAssetPath(this);
#else
                return null;
#endif
            }
        }

        public event Action onOutputTextureUpdated;
        public event Action afterCommandBufferExecuted;

        public MixtureGraph()
        {
            base.onEnabled += Enabled;
        }

        public MixtureSettings settings = new MixtureSettings()
        {
            // Default graph values:
            width = 1024,
            height = 1024,
            depth = 1,
            dimension = OutputDimension.Texture2D,
            outputChannels = OutputChannel.RGBA,
            outputPrecision = OutputPrecision.Half,
        };

        protected override void OnEnable()
        {
            MigrateGraph();
            SanitizeSettings();
            base.OnEnable();
        }

        void Enabled()
        {
            // We should have only one OutputNode per graph
            if (type != MixtureGraphType.Behaviour && outputNode == null)
                outputNode = AddNode(BaseNode.CreateFromType<OutputNode>(Vector2.zero)) as OutputNode;

#if UNITY_EDITOR
            // TODO: check if the asset is in a Resources folder for realtime and put a warning if it's not the case
            // + store the Resources path in a string
            if (type == MixtureGraphType.Realtime)
                RealtimeMixtureReferences.realtimeMixtureCRTs.Add(mainOutputAsset as CustomRenderTexture);

            // Check that object references are really ours (just in case the asset was duplicated)
            objectReferences.RemoveAll(obj => { return AssetDatabase.GetAssetPath(obj) != mainAssetPath; });

            // Cleanup deleted variants
            variants.RemoveAll(v => v == null);
#endif
        }

        void MigrateGraph()
        {
#pragma warning disable CS0618
            if (isRealtime)
            {
                type = MixtureGraphType.Realtime;
                isRealtime = false;
            }
#pragma warning restore CS0618

            if (version == Version.Initial)
            {
                if (outputNode?.settings != null)
                {
                    // migrate output node settings to graph settings:
                    settings = outputNode.settings.Clone();

                    // Patch output node settings to inherit graph settings (old behavior)
                    outputNode.settings.editFlags |= EditFlags.Size;
                    outputNode.settings.sizeMode = OutputSizeMode.InheritFromGraph;
                    outputNode.settings.outputPrecision = OutputPrecision.InheritFromGraph;
                    outputNode.settings.outputChannels = OutputChannel.InheritFromGraph;
                    outputNode.settings.dimension = OutputDimension.InheritFromGraph;
                }

                foreach (var node in nodes)
                {
                    // Migrate node settings
                    if (node is MixtureNode n && n != null)
                    {
                        if (n.settings.outputChannels == 0)
                            n.settings.outputChannels = OutputChannel.InheritFromGraph;
                        if (n.settings.outputPrecision == 0)
                            n.settings.outputPrecision = OutputPrecision.InheritFromGraph;
                        if (n.settings.dimension == 0)
                            n.settings.dimension = OutputDimension.InheritFromGraph;
                        if (n.settings.sizeMode == 0)
                            n.settings.sizeMode = OutputSizeMode.InheritFromGraph;
                        if (n.settings.widthScale == 0)
                            n.settings.widthScale = 1;
                        if (n.settings.heightScale == 0)
                            n.settings.heightScale = 1;
                        if (n.settings.depthScale == 0)
                            n.settings.depthScale = 1;
                    }
                }

                version = Version.SettingsRefactor;
            }

            version = MixtureUtils.GetLastEnumValue<Version>();
        }

        void SanitizeSettings()
        {
            // Avoid undefined values in settings
            if (settings.outputChannels.Inherits())
                settings.outputChannels = OutputChannel.RGBA;
            if (settings.outputPrecision.Inherits())
                settings.outputPrecision = OutputPrecision.Half;
            if (settings.dimension.Inherits())
                settings.dimension = OutputDimension.Texture2D;
            if (settings.wrapMode.Inherits())
                settings.wrapMode = OutputWrapMode.Mirror;
            if (settings.filterMode.Inherits())
                settings.filterMode = OutputFilterMode.Trilinear;
            if (settings.sizeMode.Inherits())
                settings.sizeMode = OutputSizeMode.Absolute;

            settings.editFlags = EditFlags.TargetFormat;
        }

        public List<Object> GetObjectsReferences()
        {
            return objectReferences;
        }

        public void AddObjectToGraph(Object obj)
        {
            objectReferences.Add(obj);

#if UNITY_EDITOR
            AssetDatabase.AddObjectToAsset(obj, mainAssetPath);
            AssetDatabase.SaveAssets();
#endif
        }

        public bool IsObjectInGraph(Object obj) => objectReferences.Contains(obj);

        public bool IsExternalSubAsset(Object obj)
        {
#if UNITY_EDITOR
            return AssetDatabase.GetAssetPath(obj) != mainAssetPath;
#else
            return false;
#endif
        }

        public void RemoveObjectFromGraph(Object obj)
        {
            if (obj == null)
                return;

            objectReferences.Remove(obj);

#if UNITY_EDITOR
            AssetDatabase.RemoveObjectFromAsset(obj);
#endif
        }

        public void ClearObjectReferences() => objectReferences.Clear();

        public Texture FindOutputTexture(string name, bool isMain)
        {
            return outputTextures.Find(t => t != null && (isMain ? t.name == mainOutputAsset.name : t.name == name));
        }

        /// <summary>
        /// Warning: this function will create updated the cached texture and may result in partial writing of texture on the disk (only uncompressed textures will be updated)
        /// </summary>
        public void UpdateOutputTextures()
        {
            foreach (var output in outputNode.outputTextureSettings)
            {
                Debug.Log("Output name : " + output.name);
                // Note that the main texture always uses the name of the asset:
                Texture oldTextureObject = FindOutputTexture(output.name, output.isMain);
                Texture newTexture;

                if (type == MixtureGraphType.Realtime)
                {
                    newTexture = UpdateOutputRealtimeTexture(output);
                    // We don't ever need to the main asset in realtime if it's already a CRT
#if UNITY_EDITOR
                    if (!(oldTextureObject is CustomRenderTexture))
                        RealtimeMixtureReferences.realtimeMixtureCRTs.Add(mainOutputAsset as CustomRenderTexture);
#endif
                }
                else
                    newTexture = UpdateOutputStaticTexture(output);

                if (oldTextureObject != newTexture)
                {
                    if (oldTextureObject != null)
                        outputTextures.Remove(oldTextureObject);
                    outputTextures.Add(newTexture);
                }
            }
        }

#if UNITY_EDITOR

        public Texture FindTextureOnDisk(string name, bool isMain)
        {
            return AssetDatabase.LoadAllAssetsAtPath(mainAssetPath).FirstOrDefault(o =>
                o is Texture t && (isMain ? t.name == mainOutputAsset.name : t.name == name)) as Texture;
        }

        public void FlushTexturesToDisk()
        {
            List<Object> assetsToKeep = new List<Object>();
            if (type == MixtureGraphType.Material &&
                AssetDatabase.LoadAllAssetsAtPath(mainAssetPath)[0] as Material == null)
            {
                AssetDatabase.AddObjectToAsset(outputMaterial, this);
                AssetDatabase.SetMainObject(outputMaterial, mainAssetPath);
            }

            foreach (var output in outputNode.outputTextureSettings)
            {
                // Note that the main texture always uses the name of the asset:
                Texture newTexture = FindOutputTexture(output.name, output.isMain);
                Texture oldTexture = FindTextureOnDisk(output.name, output.isMain);

                // Update the asset on disk if they differ
                if (oldTexture == null || newTexture.GetType() != oldTexture.GetType())
                {
                    UpdateTextureAssetOnDisk(newTexture, output.isMain);
                    assetsToKeep.Add(newTexture);
                }
                // In case the old texture already exists, we can swap it's internal data with the new texture
                // which prevent any reference loss that a Destroy would have caused.
                else if (newTexture != oldTexture)
                {
                    EditorUtility.CopySerialized(newTexture, oldTexture);
                    if (output.isMain)
                    {
                        AssetDatabase.SetMainObject(oldTexture, mainAssetPath);
                        mainOutputAsset = oldTexture;
                    }

                    Object.DestroyImmediate(newTexture);
                    outputTextures.Remove(newTexture);
                    outputTextures.Add(oldTexture);
                    assetsToKeep.Add(oldTexture);
                }
                else
                    assetsToKeep.Add(oldTexture);
            }

            foreach (var tex in AssetDatabase.LoadAllAssetsAtPath(mainAssetPath).OfType<Texture>())
            {
                // When a texture contains the not editable hideflag (for example a prefab capture image) we don't remove it
                // otherwise it would break the graph.
                if (!assetsToKeep.Contains(tex) && (tex.hideFlags & HideFlags.NotEditable) == 0)
                {
                    AssetDatabase.RemoveObjectFromAsset(tex);
                    DestroyImmediate(tex, true);
                }
            }

            // Do not reimport the graph during saving because it can mess up everything if TMP is installed
            // There are post processors that unload the mixture assets in TMP package :(
            AssetDatabase.SaveAssets();
        }

        void UpdateTextureAssetOnDisk(Texture newTexture, bool main = false)
        {
            if (newTexture == null)
                return;

            AssetDatabase.AddObjectToAsset(newTexture, this);
            if (main)
            {
                AssetDatabase.SetMainObject(newTexture, mainAssetPath);
                mainOutputAsset = newTexture;
            }
        }
#endif

        Texture UpdateOutputRealtimeTexture(OutputTextureSettings outputSettings)
        {
            var dimension = outputNode.settings.GetResolvedTextureDimension(this);
            var width = outputNode.settings.GetResolvedWidth(this);
            var height = outputNode.settings.GetResolvedHeight(this);
            var depth = outputNode.settings.GetResolvedDepth(this);
            var graphicsFormat = outputNode.settings.GetGraphicsFormat(this);

            var oldTexture = FindOutputTexture(outputSettings.name, outputSettings.isMain);
            Texture newTexture = oldTexture;

            if (!(oldTexture is CustomRenderTexture))
            {
                newTexture = new CustomRenderTexture(width, height, graphicsFormat)
                    {name = "Realtime Final Copy", enableRandomWrite = true};
            }

            var crt = newTexture as CustomRenderTexture;
            bool needsUpdate = crt.width != width
                               || crt.height != height
                               || crt.useMipMap != outputSettings.hasMipMaps
                               || crt.volumeDepth != depth
                               || crt.graphicsFormat != graphicsFormat
                               || crt.updateMode != CustomRenderTextureUpdateMode.Realtime;

            if (needsUpdate)
            {
                if (crt.IsCreated())
                    crt.Release();
                crt.width = Mathf.Max(1, width);
                crt.height = Mathf.Max(1, height);
                crt.graphicsFormat = graphicsFormat;
                crt.useMipMap = outputSettings.hasMipMaps;
                crt.autoGenerateMips = false;
                crt.updateMode = CustomRenderTextureUpdateMode.Realtime;
                crt.volumeDepth = depth;
                crt.Create();
            }

            if (outputSettings.isMain)
                mainOutputAsset = newTexture;

            newTexture.name = outputSettings.name;

            return newTexture;
        }

        Texture UpdateOutputStaticTexture(OutputTextureSettings outputSettings)
        {
            var dimension = outputNode.settings.GetResolvedTextureDimension(this);
            var width = outputNode.settings.GetResolvedWidth(this);
            var height = outputNode.settings.GetResolvedHeight(this);
            var depth = outputNode.settings.GetResolvedDepth(this);
            var graphicsFormat = outputNode.settings.GetGraphicsFormat(this);
            var creationFlags = outputSettings.hasMipMaps ? TextureCreationFlags.MipChain : TextureCreationFlags.None;

            // Check if we need to re-create the texture:
            var currentTexture = FindOutputTexture(outputSettings.name, outputSettings.isMain);

            if (currentTexture != null)
            {
                bool matchTextureSettings = currentTexture.dimension == dimension
                                            && currentTexture.width == width && currentTexture.height == height
                                            && (currentTexture.mipmapCount > 1) == outputSettings.hasMipMaps
                                            && MatchTextureTypeWithGraphType(currentTexture);

                bool MatchTextureTypeWithGraphType(Texture t)
                {
                    bool realtimeTexture = typeof(RenderTexture).IsAssignableFrom(t.GetType());
                    if (type == MixtureGraphType.Baked)
                        return !realtimeTexture;
                    else
                        return realtimeTexture;
                }

                bool conversionOrCompression =
                    outputSettings.IsCompressionEnabled() || outputSettings.IsConversionEnabled();
                matchTextureSettings &= conversionOrCompression ||
                                        (!conversionOrCompression && currentTexture.graphicsFormat == graphicsFormat);

                // Note that here we don't check the graphic format of the texture, because the current texture
                // can use a compressed format which will be different compared to the one in the graph.
                // This can be a problem because we may end up re-creating render targets when we don't need to.
                if (conversionOrCompression && matchTextureSettings)
                    return currentTexture;
                else if (
                    !conversionOrCompression &&
                    matchTextureSettings) // Otherwise if the format is not compressed, we want to compare the format because it directly affects the data on disk
                {
                    if (currentTexture.graphicsFormat == graphicsFormat)
                        return currentTexture;
                }
            }

            outputTextures.RemoveAll(t =>
                t.name == outputSettings.name || (outputSettings.isMain && t.name == mainOutputAsset.name));

            Texture newTexture = null;

            switch (dimension)
            {
                case TextureDimension.Tex2D:
                    newTexture = new Texture2D(width, height, graphicsFormat, creationFlags);
                    onOutputTextureUpdated?.Invoke();
                    break;
                case TextureDimension.Tex3D:
                    newTexture = new Texture3D(width, height, depth, graphicsFormat, creationFlags);
                    onOutputTextureUpdated?.Invoke();
                    break;
                case TextureDimension.Cube:
                    newTexture = new Cubemap(width, graphicsFormat, creationFlags);
                    onOutputTextureUpdated?.Invoke();
                    break;
                default:
                    Debug.LogError("Texture format " + dimension + " is not supported");
                    return null;
            }

            newTexture.name = (outputSettings.isMain) ? mainOutputAsset.name : outputSettings.name;

            outputTextures.Add(newTexture);

            return newTexture;
        }

#if UNITY_EDITOR
        public void SaveExternalTexture(ExternalOutputNode external, bool saveAs = false)
        {
            try
            {
                Texture outputTexture = null;
                bool isHDR = external.settings.IsHDR(this);

                TextureDimension dimension = external.settings.GetResolvedTextureDimension(this);
                GraphicsFormat format = (GraphicsFormat) external.settings.GetGraphicsFormat(this);
                var rtSettings = external.settings;

                switch (dimension)
                {
                    case TextureDimension.Tex2D:
                        outputTexture = new Texture2D(rtSettings.GetResolvedWidth(this),
                            rtSettings.GetResolvedHeight(this), format, TextureCreationFlags.MipChain);
                        break;
                    case TextureDimension.Cube:
                        outputTexture = new Cubemap(rtSettings.GetResolvedWidth(this), format,
                            TextureCreationFlags.MipChain);
                        break;
                    case TextureDimension.Tex3D:
                        outputTexture = new Texture3D(rtSettings.GetResolvedWidth(this),
                            rtSettings.GetResolvedHeight(this), rtSettings.GetResolvedDepth(this), format,
                            TextureCreationFlags.MipChain);
                        break;
                }

                EditorUtility.DisplayProgressBar("Mixture", "Reading Back Data...", 0.1f);
                var o = external.outputTextureSettings.First();
                ReadBackTexture(external, o.finalCopyRT, false, o.compressionFormat, o.compressionQuality,
                    outputTexture);

                // Check Output Type
                string assetPath;
                if (external.asset != null && !saveAs)
                    assetPath = AssetDatabase.GetAssetPath(external.asset);
                else
                {
                    string extension = "asset";

                    if (dimension == TextureDimension.Tex2D)
                    {
                        if (isHDR)
                            extension = "exr";
                        else
                            extension = "png";
                    }

                    assetPath = EditorUtility.SaveFilePanelInProject("Save Texture", external.name, extension,
                        "Save Texture");

                    if (string.IsNullOrEmpty(assetPath))
                    {
                        EditorUtility.ClearProgressBar();
                        return; // Canceled
                    }
                }

                EditorUtility.DisplayProgressBar("Mixture", $"Writing to {assetPath}...", 0.3f);

                if (dimension == TextureDimension.Tex3D)
                {
                    var volume = AssetDatabase.LoadAssetAtPath<Texture3D>(assetPath);
                    if (volume == null)
                    {
                        volume = new Texture3D(external.settings.width, external.settings.height,
                            external.settings.depth, (TextureFormat) external.external3DFormat, true);
                        AssetDatabase.CreateAsset(volume, assetPath);
                    }

                    // TODO: check resolution
                    if (volume.format != (TextureFormat) external.external3DFormat)
                    {
                        var newTexture = new Texture3D(external.settings.width, external.settings.height,
                            external.settings.depth, (TextureFormat) external.external3DFormat, true);
                        EditorUtility.CopySerialized(newTexture, volume);
                        Object.DestroyImmediate(newTexture);
                    }

                    volume.SetPixels((outputTexture as Texture3D).GetPixels());
                    volume.Apply();
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    external.asset = volume;
                }
                else if (dimension == TextureDimension.Tex2D)
                {
                    byte[] contents = null;

                    if (isHDR)
                        contents = ImageConversion.EncodeToEXR(outputTexture as Texture2D);
                    else
                    {
                        var colors = (outputTexture as Texture2D).GetPixels();

                        // We only do the conversion for whe the graph uses SRGB images
                        if (external.mainOutput.sRGB)
                        {
                            for (int i = 0; i < colors.Length; i++)
                                colors[i] = colors[i].linear;
                        }

                        (outputTexture as Texture2D).SetPixels(colors);

                        contents = ImageConversion.EncodeToPNG(outputTexture as Texture2D);
                    }

                    System.IO.File.WriteAllBytes(
                        System.IO.Path.GetDirectoryName(Application.dataPath) + "/" + assetPath, contents);

                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    TextureImporter importer = (TextureImporter) AssetImporter.GetAtPath(assetPath);
                    switch (external.external2DOoutputType)
                    {
                        case ExternalOutputNode.External2DOutputType.Color:
                            importer.textureType = TextureImporterType.Default;
                            importer.sRGBTexture = true;
                            break;
                        case ExternalOutputNode.External2DOutputType.Linear:
                            importer.textureType = TextureImporterType.Default;
                            importer.sRGBTexture = false;
                            break;
                        case ExternalOutputNode.External2DOutputType.Normal:
                            importer.textureType = TextureImporterType.NormalMap;
                            break;
                        case ExternalOutputNode.External2DOutputType.LatLonCubemap:
                            importer.textureShape = TextureImporterShape.TextureCube;
                            importer.generateCubemap = TextureImporterGenerateCubemap.Cylindrical;
                            break;
                    }

                    importer.SaveAndReimport();

                    if (external.external2DOoutputType == ExternalOutputNode.External2DOutputType.LatLonCubemap)
                        external.asset = AssetDatabase.LoadAssetAtPath<Cubemap>(assetPath);
                    else
                        external.asset = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                }
                else if (dimension == TextureDimension.Cube)
                {
                    var cube = AssetDatabase.LoadAssetAtPath<Cubemap>(assetPath);
                    if (cube == null)
                    {
                        cube = new Cubemap(external.settings.width, (TextureFormat) external.external3DFormat, true);
                        AssetDatabase.CreateAsset(cube, assetPath);
                    }

                    // TODO: check resolution
                    if (cube.format != (TextureFormat) external.external3DFormat)
                    {
                        var newTexture = new Cubemap(external.settings.width, (TextureFormat) external.external3DFormat,
                            true);
                        EditorUtility.CopySerialized(newTexture, cube);
                        Object.DestroyImmediate(newTexture);
                    }

                    for (int i = 0; i < 6; i++)
                    {
                        CubemapFace face = (CubemapFace) i;
                        cube.SetPixels((outputTexture as Cubemap).GetPixels(face), face);
                    }

                    cube.Apply();
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    external.asset = cube;
                }

                EditorUtility.DisplayProgressBar("Mixture", $"Importing {assetPath}...", 1.0f);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                if (saveAs)
                {
                    var tex = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);
                    EditorGUIUtility.PingObject(tex);
                    Selection.activeObject = tex;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
#endif

        public void ReadbackMainTexture(Texture target)
        {
            if (type == MixtureGraphType.Realtime)
            {
                Debug.LogError("Can't save runtime texture to a specified path.");
                return;
            }

            var o = outputNode.outputTextureSettings.First();
            ReadBackTexture(outputNode, o.finalCopyRT, o.IsCompressionEnabled(), o.compressionFormat,
                o.compressionQuality, target);
        }

#if UNITY_EDITOR
        public void SaveAll(bool pingObject = true)
        {
            if (type == MixtureGraphType.Realtime)
                return;

            UpdateOutputTextures();
            foreach (var item in outputTextures)
            {
                if (item == null)
                    Debug.Log("Null Output");
                else
                {
                    Debug.Log("Regular Output");
                }
            }

            Debug.Log("Output Settings Count : " + outputNode.outputTextureSettings.Count);
            foreach (var output in outputNode.outputTextureSettings)
            {
                // We only need to update the main asset texture because the outputTexture should
                // always be correctly setup when we arrive here.
                Debug.Log("Output Name : " + output.name);
                var currentTexture = FindOutputTexture(output.name, output.isMain);
                //Debug.Log("Current Texture : " + currentTexture);
                // The main texture is always the first one
                var format = output.enableConversion
                    ? (TextureFormat) output.conversionFormat
                    : output.compressionFormat;
                ReadBackTexture(this.outputNode, output.finalCopyRT,
                    output.IsCompressionEnabled() || output.IsConversionEnabled(), format, output.compressionQuality,
                    currentTexture);
            }

            FlushTexturesToDisk();

            AssetDatabase.Refresh();

            if (pingObject)
                EditorGUIUtility.PingObject(mainOutputAsset);
        }

        public void UpdateRealtimeAssetsOnDisk()
        {
            if (type != MixtureGraphType.Realtime)
                return;

            UpdateOutputTextures();
            FlushTexturesToDisk();
            AssetDatabase.SaveAssets();
        }

        public void UpdateLinkedVariants()
        {
            // Sadly we can't display a progress bar for updaing mixture variants because it makes the
            // EdutorUtility.CompressTexture() function throw erors :( 
            // EditorUtility.DisplayCancelableProgressBar("Updating Mixture Variants", "Startup...", 0);

            try
            {
                int index = 0;
                foreach (var variant in variants)
                {
                    if (variant == null)
                        continue;

                    variant.CopyTexturesFromGraph(false);
                    variant.UpdateAllVariantTextures();
                    index++;
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally // always cleanup the progress bar
            {
                // EditorUtility.ClearProgressBar();
            }
        }
#endif

        public struct ReadbackData
        {
            public OutputNode node;
            public Texture targetTexture;
            public RenderTexture source;
            public int mipLevel;
        }

        // Write the rendertexture value to the graph main texture asset, or to an external Texture
        public void ReadBackTexture(OutputNode node, RenderTexture source, bool enableCompression = false,
            TextureFormat compressionFormat = TextureFormat.DXT5,
            MixtureCompressionQuality compressionQuality = MixtureCompressionQuality.Best,
            Texture externalTexture = null)
        {
            var outputFormat = node.settings.GetGraphicsFormat(this);
            var target = externalTexture == null ? mainOutputAsset as Texture : externalTexture;
            string name = target.name;

            // When we use Texture2D, we can compress them. In that case we use a temporary target for readback before compressing / converting it.
#if UNITY_EDITOR

            bool useTempTarget = enableCompression || outputFormat != target.graphicsFormat;
#else
            // We can't compress the texture in real-time
            bool useTempTarget = false;
#endif

            if (useTempTarget)
            {
                var textureFlags = source.useMipMap ? TextureCreationFlags.MipChain : TextureCreationFlags.None;
                switch (source.dimension)
                {
                    case TextureDimension.Tex2D:
                        target = new Texture2D(target.width, target.height, outputFormat, textureFlags);
                        break;
                    case TextureDimension.Tex3D:
                        target = new Texture3D(target.width, target.height, source.volumeDepth, outputFormat,
                            textureFlags);
                        break;
                    case TextureDimension.Cube:
                        target = new Cubemap(target.width, outputFormat, textureFlags);
                        break;
                }

                target.name = target.name;
            }

            var readbackRequests = new List<AsyncGPUReadbackRequest>();
            for (int mipLevel = 0; mipLevel < source.mipmapCount; mipLevel++)
            {
                int width = source.width / (1 << mipLevel);
                int height = source.height / (1 << mipLevel);
                int depth = source.dimension == TextureDimension.Cube
                    ? 6
                    : (source.dimension == TextureDimension.Tex2D
                        ? 1
                        : Mathf.Max(source.volumeDepth / (1 << mipLevel), 1));
                var data = new ReadbackData
                {
                    node = node,
                    targetTexture = target,
                    mipLevel = mipLevel,
                    source = source,
                };
                var request = AsyncGPUReadback.Request(source, mipLevel, 0, width, 0, height, 0, depth,
                    (r) => { WriteRequestResult(r, data); });

                request.Update();
                readbackRequests.Add(request);
            }

            // TODO: async code
            foreach (var r in readbackRequests)
                r.WaitForCompletion();

            if (useTempTarget)
            {
                var dst = externalTexture == null ? mainOutputAsset as Texture : externalTexture;
                //Debug.Log(dst);
                //var d = dst as CustomRenderTexture;
                bool isCompressedFormat =
                    GraphicsFormatUtility.IsCompressedFormat(
                        GraphicsFormatUtility.GetGraphicsFormat(compressionFormat, false));
                if (enableCompression && isCompressedFormat)
                    CompressTexture(target, dst, compressionFormat, compressionQuality);
                // We need a special case for 3D textures because the function doesn't handle them
                else if (source.dimension == TextureDimension.Tex3D)
                    ConvertOutput3DTexture(target as Texture3D, dst as Texture3D, compressionFormat);
                else
                {
                    if (!Graphics.ConvertTexture(target, dst))
                    {
                        Debug.LogError("Failed to convert " + target.graphicsFormat + " into " + dst.graphicsFormat +
                                       " | from: " + compressionFormat);
                    }
                }

                dst.name = name;
            }
        }

        protected void WriteRequestResult(AsyncGPUReadbackRequest request, ReadbackData data)
        {
            var outputPrecision = data.node.settings.GetResolvedPrecision(this);
            var outputChannels = data.node.settings.GetResolvedChannels(this);

            if (request.hasError)
            {
                Debug.LogError("Can't readback the texture from GPU");
                return;
            }
            else
            {
                Debug.Log("No Error in request !");
                Debug.Log(data.targetTexture);
            }

            switch (data.targetTexture)
            {
                case Texture2D t:
                    t.SetPixelData(request.GetData<float>(0), data.mipLevel);
                    t.Apply(false);
                    break;
                case Texture3D t:
                    List<float> rawData = new List<float>();

                    int sliceCount = Mathf.Max(data.source.volumeDepth / (1 << data.mipLevel), 1);
                    for (int i = 0; i < sliceCount; i++)
                        rawData.AddRange(request.GetData<float>(i).ToList());

                    t.SetPixelData(rawData.ToArray(), data.mipLevel);
                    t.Apply(false);
                    break;
                case Cubemap t:
                    for (int i = 0; i < 6; i++)
                        t.SetPixelData(request.GetData<float>(i), data.mipLevel, (CubemapFace) i);
                    t.Apply(false);
                    break;
                default:
                    Debug.LogError(data.targetTexture + " is not a supported type for saving");
                    return;
            }
        }

        /// <summary>
        /// Graphics.ConvertTexture doesn't work with 3D textures :(
        /// </summary>
        unsafe void ConvertOutput3DTexture(Texture3D source, Texture3D destination, TextureFormat compressionFormat)
        {
#if UNITY_EDITOR
            OutputPrecision inputPrecision = outputNode.settings.outputPrecision;
            OutputChannel inputChannels = outputNode.settings.outputChannels;

            // We allocate the final texture in the correct format, that we'll they swap with the destination texture.
            var finalCompressedTexture = new Texture3D(source.width, source.height, source.depth, compressionFormat,
                destination.mipmapCount);
            for (int mipLevel = 0; mipLevel < source.mipmapCount; mipLevel++)
            {
                var pixels = source.GetPixels(mipLevel);

                finalCompressedTexture.SetPixels(pixels, mipLevel);
            }

            EditorUtility.CopySerialized(finalCompressedTexture, destination);
            Object.DestroyImmediate(finalCompressedTexture);
            Object.DestroyImmediate(source);
#endif
        }

        /// <summary>
        /// This only works for Texture2D
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        void CompressTexture(Texture source, Texture destination, TextureFormat format,
            MixtureCompressionQuality quality)
        {
#if UNITY_EDITOR
            // Copy the readback texture into the compressed one (replace it)
            EditorUtility.CopySerialized(source, destination);
            Object.DestroyImmediate(source);

            if (destination.dimension == TextureDimension.Tex2D)
                EditorUtility.CompressTexture(destination as Texture2D, (TextureFormat) format,
                    (UnityEditor.TextureCompressionQuality) quality);
            else if (destination.dimension == TextureDimension.Cube)
                EditorUtility.CompressCubemapTexture(destination as Cubemap, (TextureFormat) format,
                    (UnityEditor.TextureCompressionQuality) quality);
            else
                Debug.LogError("Unsupported texture dimension for compression");
#endif
        }

        internal void InvokeCommandBufferExecuted() => afterCommandBufferExecuted?.Invoke();

        public void UpdateNodeInheritanceMode()
        {
            foreach (var node in nodes)
            {
                if (node is MixtureNode n)
                    n.settings.SyncInheritanceMode(defaultNodeInheritanceMode);
            }
        }
    }
}