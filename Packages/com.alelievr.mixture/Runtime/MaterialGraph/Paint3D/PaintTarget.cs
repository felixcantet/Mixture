using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class PaintTarget : MonoBehaviour
{
    private const int TEXTURE_SIZE = 1024;

    public List<Material> materialsPalette = new List<Material>();
    
    public float extendsIslandOffset = 1.0f;

    private RenderTexture extendIslandsRenderTexture;
    private RenderTexture uvIslandsRenderTexture;
    private RenderTexture maskRenderTexture;
    private RenderTexture supportTexture;

    private Renderer rd;
    
    int maskTextureID = Shader.PropertyToID("_MaskTexture");

    public RenderTexture getMask() => maskRenderTexture;
    public RenderTexture getUVIslands() => uvIslandsRenderTexture;
    public RenderTexture getExtend() => extendIslandsRenderTexture;
    public RenderTexture getSupport() => supportTexture;
    public Renderer getRenderer() => rd;

    private void Start()
    {
        Debug.Log($"Paint Target {gameObject.name} Start()");
        
        maskRenderTexture = new RenderTexture(TEXTURE_SIZE, TEXTURE_SIZE, 0);
        maskRenderTexture.filterMode = FilterMode.Bilinear;

        extendIslandsRenderTexture = new RenderTexture(TEXTURE_SIZE, TEXTURE_SIZE, 0);
        extendIslandsRenderTexture.filterMode = FilterMode.Bilinear;

        uvIslandsRenderTexture = new RenderTexture(TEXTURE_SIZE, TEXTURE_SIZE, 0);
        uvIslandsRenderTexture.filterMode = FilterMode.Bilinear;

        supportTexture = new RenderTexture(TEXTURE_SIZE, TEXTURE_SIZE, 0);
        supportTexture.filterMode =  FilterMode.Bilinear;

        rd = GetComponent<Renderer>();
        rd.sharedMaterial.SetTexture(maskTextureID, extendIslandsRenderTexture);
    }
    
    void OnDestroy()
    {
        Debug.Log($"Paint Target {gameObject.name} OnDestroy()");

        maskRenderTexture.Release();
        uvIslandsRenderTexture.Release();
        extendIslandsRenderTexture.Release();
        supportTexture.Release();
    }
    
}