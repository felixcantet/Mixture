﻿#if DEVELOPMENT_BUILD || UNITY_EDITOR
    #define CUSTOM_TEXTURE_PROFILING
#endif

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Linq;
using System;
using UnityEngine.Profiling;
using Mixture;

#if UNITY_EDITOR
[UnityEditor.InitializeOnLoad]
#endif
public static class CustomTextureManager
{
    static CustomTextureManager() => SetupManager();

    public static List<CustomRenderTexture> customRenderTextures {get; private set;} = new List<CustomRenderTexture>();
    static List<CustomRenderTexture> sortedCustomRenderTextures = new List<CustomRenderTexture>();

    static HashSet<CustomRenderTexture> needsInitialization = new HashSet<CustomRenderTexture>();
    static Dictionary<CustomRenderTexture, int> needsUpdate = new Dictionary<CustomRenderTexture, int>();

    static Dictionary<CustomRenderTexture, int> computeOrder = new Dictionary<CustomRenderTexture, int>();
    static Dictionary<CustomRenderTexture, CustomSampler> customRenderTextureSamplers = new Dictionary<CustomRenderTexture, CustomSampler>();

    public static event Action<CommandBuffer, CustomRenderTexture> onBeforeCustomTextureUpdated;

    [RuntimeInitializeOnLoadMethod]
    static void SetupManager()
    {
        CustomRenderTextureManager.textureLoaded -= OnCRTLoaded;
        CustomRenderTextureManager.textureLoaded += OnCRTLoaded;
        CustomRenderTextureManager.textureUnloaded -= OnCRTUnloaded;
        CustomRenderTextureManager.textureUnloaded += OnCRTUnloaded;

        CustomRenderTextureManager.updateTriggered += OnUpdateCalled;
        CustomRenderTextureManager.initializeTriggered += OnInitializeCalled;

#if UNITY_EDITOR
        // In the editor we might not always have a camera to update our custom render textures
        UnityEditor.EditorApplication.update -= UpdateCRTsEditor;
        UnityEditor.EditorApplication.update += UpdateCRTsEditor;
#else
        // Workaround for built-in renderer that don't have the RenderPipelineManager callbacks
        if (GraphicsSettings.renderPipelineAsset == null)
        {
            // Avoid creating the updater twice
            if (Resources.FindObjectsOfTypeAll<BuiltinRealtimeMixtureUpdater>().Length == 0)
            {
                var mixtureUpdateObject = new GameObject("Realtime Mixture Updater");
                GameObject.DontDestroyOnLoad(mixtureUpdateObject);
                var builtinUpdater = mixtureUpdateObject.AddComponent<BuiltinRealtimeMixtureUpdater>();
                builtinUpdater.onPreRender += UpdateCRTsRuntime;
            }
        }
        else
        {
            RenderPipelineManager.beginFrameRendering -= UpdateCRTsRuntime;
            RenderPipelineManager.beginFrameRendering += UpdateCRTsRuntime;
        }
#endif

        GraphicsSettings.disableBuiltinCustomRenderTextureUpdate = true;
        UpdateSRPCustomRenderTextureStatus();
    }

#if UNITY_EDITOR
    static void UpdateCRTsEditor()
    {
        if (!GraphicsSettings.disableBuiltinCustomRenderTextureUpdate)
            return;

        // When run in background is not enabled and unity is in background, we skip the processing
        if (!Application.runInBackground && !UnityEditorInternal.InternalEditorUtility.isApplicationActive)
            return;


        UpdateDependencies();

        Graphics.ExecuteCommandBuffer(MakeCRTCommandBuffer());
    }
#endif

    static void UpdateCRTsRuntime(ScriptableRenderContext context, Camera[] cameras)
    {
        if (!GraphicsSettings.disableBuiltinCustomRenderTextureUpdate)
            return;
        
        UpdateDependencies();

        context.ExecuteCommandBuffer(MakeCRTCommandBuffer());
    }

    static void UpdateCRTsRuntime()
    {
        if (!GraphicsSettings.disableBuiltinCustomRenderTextureUpdate)
            return;

        UpdateDependencies();

        Graphics.ExecuteCommandBuffer(MakeCRTCommandBuffer());
    }

    static CommandBuffer MakeCRTCommandBuffer()
    {
        var cmd = CommandBufferPool.Get("SRP Custom Render Texture");
        foreach (var crt in sortedCustomRenderTextures)
            UpdateCustomRenderTexture(cmd, crt);
        return cmd;
    }

    public static void ForceUpdateNow()
    {
        UpdateDependencies();
        Graphics.ExecuteCommandBuffer(MakeCRTCommandBuffer());
    }

    /// <summary>
    /// Add a CRT that is not yet tracked by the manager because of the frame of delay.
    /// </summary>
    /// <param name="crt"></param>
    public static void RegisterNewCustomRenderTexture(CustomRenderTexture crt)
    {
        OnCRTLoaded(crt);
    }

    static void UpdateSRPCustomRenderTextureStatus()
    {
        if (!GraphicsSettings.disableBuiltinCustomRenderTextureUpdate)
        {
            // SRP custom textures have been disabled so we clear our list
            customRenderTextures.Clear();
        }
        else
        {
            // Gather the list of all running custom render textures and call the loaded callback
            CustomRenderTextureManager.GetAllCustomRenderTextures(customRenderTextures);
            
            foreach (var crt in customRenderTextures)
                InitializeCustomRenderTexture(crt);
        }
    }

    // CustomRenderTexture.Update have been called by the user
    static void OnUpdateCalled(CustomRenderTexture crt, int count)
    {
        if (needsUpdate.ContainsKey(crt))
            needsUpdate[crt] += count;
        else
            needsUpdate[crt] = count;
    }

    // CustomRenderTexture.Initialize have been called by the user
    static void OnInitializeCalled(CustomRenderTexture crt) => needsInitialization.Add(crt);

    static void OnCRTLoaded(CustomRenderTexture crt)
    {
        if (!customRenderTextures.Contains(crt))
        {
            customRenderTextures.Add(crt);
            InitializeCustomRenderTexture(crt);
        }
    }

    static void InitializeCustomRenderTexture(CustomRenderTexture crt)
    {
        // Debug.Log("Load: " + crt + " | " + crt.updateMode);
    }

    static void OnCRTUnloaded(CustomRenderTexture crt) => customRenderTextures.Remove(crt);

    static void UpdateDependencies()
    {
        computeOrder.Clear();
        sortedCustomRenderTextures.Clear();

        // Check if the CRTs are valid first:
        foreach (var crt in customRenderTextures)
            if (!IsValid(crt))
                computeOrder[crt] = -1;

        foreach (var crt in customRenderTextures)
            UpdateComputeOrder(crt, 0);
        
        foreach (var crt in customRenderTextures)
        {
            if (computeOrder.ContainsKey(crt) && computeOrder[crt] != -1)
                sortedCustomRenderTextures.Add(crt);
        }

        sortedCustomRenderTextures.Sort((c1, c2) => {
            if (!computeOrder.TryGetValue(c1, out int i1))
                i1 = -1;
            if (!computeOrder.TryGetValue(c2, out int i2))
                i2 = -1;

            return i1.CompareTo(i2);
        });
    }

    static int UpdateComputeOrder(CustomRenderTexture crt, int depth)
    {
        int crtComputeOrder = 0;

        if (depth > 500)
        {
            Debug.LogError("Recursion error while updating compute order");
            return -1;
        }

        if (crt?.material?.shader == null)
            return -1;

        if (computeOrder.TryGetValue(crt, out crtComputeOrder))
            return crtComputeOrder;

        var shader = crt.material.shader;
        int propertyCount = shader.GetPropertyCount();
        for (int i = 0; i < propertyCount; i++)
        {
            if (shader.GetPropertyType(i) != ShaderPropertyType.Texture)
                continue;

            var texture = crt.material.GetTexture(shader.GetPropertyNameId(i));

            if (texture is CustomRenderTexture dep)
            {
                int c = UpdateComputeOrder(dep, depth + 1);

                if (c == -1)
                {
                    crtComputeOrder = -1;
                    break;
                }

                crtComputeOrder += c;
            }
        }

        if (crtComputeOrder != -1)
            crtComputeOrder++;
        
        computeOrder[crt] = crtComputeOrder;

        return crtComputeOrder;
    }

    static bool IsValid(CustomRenderTexture crt)
    {
        if (crt.material == null || crt.material.shader == null)
            return false;
        
        if (crt.material.passCount == 0)
            return false;

#if UNITY_EDITOR
        // If the shader have errors
        int messageCount = UnityEditor.ShaderUtil.GetShaderMessageCount(crt.material.shader);
        if (messageCount > 0)
        {
            var compilationMessages = UnityEditor.ShaderUtil.GetShaderMessages(crt.material.shader);
            if (compilationMessages.Any(m => m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error))
                return false;
        }
#endif

        return true;
    }

    static int kUpdateDataCenters              = Shader.PropertyToID("CustomRenderTextureCenters");
    static int kUpdateDataSizesAndRotation     = Shader.PropertyToID("CustomRenderTextureSizesAndRotations");
    static int kUpdateDataPrimitiveIDs         = Shader.PropertyToID("CustomRenderTexturePrimitiveIDs");
    static int kCustomRenderTextureParameters  = Shader.PropertyToID("CustomRenderTextureParameters");
    static int kCustomRenderTextureInfo        = Shader.PropertyToID("_CustomRenderTextureInfo");
    static int kSelf2D                         = Shader.PropertyToID("_SelfTexture2D");
    static int kSelf3D                         = Shader.PropertyToID("_SelfTexture3D");
    static int kSelfCube                       = Shader.PropertyToID("_SelfTextureCube");
    static int kMipLevel                       = Shader.PropertyToID("_CustomRenderTextureMipLevel");

    // Returns user facing texture info
    static Vector4 GetTextureInfos(CustomRenderTexture crt, int sliceIndex, int mipLevel)
    {
        var info = new Vector4((float)crt.width, (float)crt.height, crt.volumeDepth, (float)sliceIndex);

        // Adjust texture size using mip level:
        if (mipLevel > 0 && mipLevel < crt.mipmapCount)
        {
            info.x = Mathf.Max((int)info.x >> mipLevel, 1);
            info.y = Mathf.Max((int)info.y >> mipLevel, 1);
            info.z = Mathf.Max((int)info.z >> mipLevel, 1);
        }

        return info;
    }

    // Returns internal parameters for rendering
    static Vector4 GetTextureParameters(CustomRenderTexture crt, int sliceIndex, int mipLevel)
    {
        int depth = GetSliceCount(crt, mipLevel);
        return new Vector4(
            (crt.updateZoneSpace == CustomRenderTextureUpdateZoneSpace.Pixel) ? 1.0f : 0.0f,
            // Important: textureparam.y is used for the z coordinate in the CRT and in case of 2D, we use 0.5 because most of the 3D compatible effects will use a neutral value 0.5
            crt.dimension == TextureDimension.Tex2D ? 0.5f : (float)sliceIndex / depth,
            // 0 => 2D, 1 => 3D, 2 => Cube
            crt.dimension == TextureDimension.Tex3D ? 1.0f : (crt.dimension == TextureDimension.Cube ? 2.0f : 0.0f),
            0.0f
            );
    }

    // Update one custom render texture.
    public static void UpdateCustomRenderTexture(CommandBuffer cmd, CustomRenderTexture crt)
    {
        bool firstPass = crt.updateCount == 0;

        // Handle initialization here too:
        if (crt.initializationMode == CustomRenderTextureUpdateMode.Realtime || needsInitialization.Contains(crt) || (firstPass && crt.initializationMode == CustomRenderTextureUpdateMode.OnLoad))
        {
            switch (crt.initializationSource)
            {
                case CustomRenderTextureInitializationSource.Material:
                    // TODO
                    break;
                case CustomRenderTextureInitializationSource.TextureAndColor:
                    // TODO
                    break;
            }
            needsInitialization.Remove(crt);
        }

        needsUpdate.TryGetValue(crt, out int updateCount);

        if (crt.material != null && (crt.updateMode == CustomRenderTextureUpdateMode.Realtime || updateCount > 0 || (firstPass && crt.updateMode == CustomRenderTextureUpdateMode.OnLoad)))
        {
            onBeforeCustomTextureUpdated?.Invoke(cmd, crt);

#if CUSTOM_TEXTURE_PROFILING
            customRenderTextureSamplers.TryGetValue(crt, out var sampler);
            if (sampler == null)
            {
                sampler = customRenderTextureSamplers[crt] = CustomSampler.Create($"{crt.name} - {crt.GetInstanceID()}", true);
                sampler.GetRecorder().enabled = true;
            }
            cmd.BeginSample(sampler);
#endif

            // using (new ProfilingScope(cmd, new ProfilingSampler($"Update {crt.name}")))
            {
                updateCount = Mathf.Max(updateCount, 1);
                crtExecInfo.TryGetValue(crt, out var execInfo);
                if (execInfo != null && execInfo.runOnAllMips)
                    for (int mipLevel = 0; mipLevel < crt.mipmapCount; mipLevel++)
                        UpdateCustomRenderTexture(cmd, crt, updateCount, mipLevel: mipLevel);
                else
                    UpdateCustomRenderTexture(cmd, crt, updateCount);

                needsUpdate.Remove(crt);
            }

#if CUSTOM_TEXTURE_PROFILING
            cmd.EndSample(sampler);
#endif
            crt.IncrementUpdateCount();
        }
    }

    // Temporary stuff that will be removed when moving to RenderGraph
    public class CustomTextureExecInfo
    {
        public bool runOnAllMips = false;
    }

    public static Dictionary<CustomRenderTexture, CustomTextureExecInfo> crtExecInfo = new Dictionary<CustomRenderTexture, CustomTextureExecInfo>();

    public static void UpdateCustomRenderTexture(CommandBuffer cmd, CustomRenderTexture crt, int updateCount, int mipLevel = 0, MaterialPropertyBlock block = null)
    {
        // Prepare "self" texture for reading in the shader for double buffered custom textures
        RenderTexture textureSelf2D = null;
        RenderTexture textureSelf3D = null;
        RenderTexture textureSelfCube = null;
        if (crt.doubleBuffered)
        {
            if (crt.dimension == TextureDimension.Tex2D)
                textureSelf2D = crt;
            if (crt.dimension == TextureDimension.Cube)
                textureSelfCube = crt;
            if (crt.dimension == TextureDimension.Tex3D)
                textureSelf3D = crt;
        }

        if (crt.doubleBuffered)
        {
            // Update the internal double buffered render texture (resize / alloc / ect.)
            crt.EnsureDoubleBufferConsistency();
        }

        if (block == null)
            block = new MaterialPropertyBlock();

        // If the user didn't called the update on CRT, we still process it because it's realtime
        for (int i = 0; i < updateCount; i++)
        {
            // TODO: cache everything
            List<CustomRenderTextureUpdateZone> updateZones = new List<CustomRenderTextureUpdateZone>();
            crt.GetUpdateZones(updateZones);

            if (updateZones.Count == 0)
                updateZones.Add(new CustomRenderTextureUpdateZone{ needSwap = false, updateZoneCenter = new Vector3(0.5f, 0.5f, 0.5f), updateZoneSize = Vector3.one, rotation = 0, passIndex = 0});

            foreach (var zone in updateZones)
            {
                var zoneCenters = updateZones.Select(z => new Vector4(z.updateZoneCenter.x, z.updateZoneCenter.y, z.updateZoneCenter.z, 0)).ToList();
                var zoneSizesAndRotation = updateZones.Select(z => new Vector4(z.updateZoneSize.x, z.updateZoneSize.y, z.updateZoneSize.z, z.rotation)).ToList();
                var zonePrimitiveIDs = Enumerable.Range(0, updateZones.Count).Select(j => (float)j).ToList();// updateZones.Select(z => 0.0f).ToList();
                int sliceCount = GetSliceCount(crt, mipLevel);

                // Copy all the slices in case the texture is double buffered
                if (zone.needSwap)
                {
                    var doubleBuffer = crt.GetDoubleBufferRenderTexture();
                    if (doubleBuffer != null)
                    {
                        // For now, it's just a copy, once we actually do the swap of pointer, be careful to reset the Active Render Texture
                        for (int sliceIndex = 0; sliceIndex < sliceCount; sliceIndex++)
                            cmd.CopyTexture(doubleBuffer, sliceIndex, crt, sliceIndex);
                    }
                }

                for (int slice = 0; slice < sliceCount; slice++)
                {
                    RenderTexture renderTexture = crt.doubleBuffered ? crt.GetDoubleBufferRenderTexture() : crt;
                    cmd.SetRenderTarget(renderTexture, mipLevel, (crt.dimension == TextureDimension.Cube) ? (CubemapFace)slice : CubemapFace.Unknown, (crt.dimension == TextureDimension.Tex3D) ? slice : 0);
                    cmd.SetViewport(new Rect(0, 0, Mathf.Max(1, crt.width >> mipLevel), Mathf.Max(1, crt.height >> mipLevel)));
                    block.SetVector(kCustomRenderTextureInfo, GetTextureInfos(crt, slice, mipLevel));
                    block.SetVector(kCustomRenderTextureParameters, GetTextureParameters(crt, slice, mipLevel));
                    block.SetFloat(kMipLevel, mipLevel);
                    if (textureSelf2D != null)
                        block.SetTexture(kSelf2D, textureSelf2D);
                    if (textureSelf3D != null)
                        block.SetTexture(kSelf3D, textureSelf3D);
                    if (textureSelfCube != null)
                        block.SetTexture(kSelfCube, textureSelfCube);

                    int passIndex = zone.passIndex == -1 ? 0 : zone.passIndex;

                    block.SetVectorArray(kUpdateDataCenters, zoneCenters);
                    block.SetVectorArray(kUpdateDataSizesAndRotation, zoneSizesAndRotation);
                    block.SetFloatArray(kUpdateDataPrimitiveIDs, zonePrimitiveIDs);

                    cmd.DrawProcedural(Matrix4x4.identity, crt.material, passIndex, MeshTopology.Triangles, 6 * updateZones.Count, 1, block);
                }
            }
        }
    }

    public static CustomSampler GetCustomTextureProfilingSampler(CustomRenderTexture crt)
    {
        customRenderTextureSamplers.TryGetValue(crt, out var sampler);
        return sampler;
    }

    static int GetSliceCount(CustomRenderTexture crt, int mipLevel)
    {
        switch (crt.dimension)
        {
            case TextureDimension.Cube:
                return 6;
            case TextureDimension.Tex3D:
                return Mathf.Max(1, crt.volumeDepth >> mipLevel);
            default:
            case TextureDimension.Tex2D:
                return 1;
        }
    }
}
