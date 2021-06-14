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

    public RenderTexture extendIslandsRenderTexture;
    public RenderTexture uvIslandsRenderTexture;
    public RenderTexture maskRenderTexture;
    public RenderTexture supportTexture;

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
        
        

        rd = GetComponent<Renderer>();
       // rd.sharedMaterial.SetTexture(maskTextureID, extendIslandsRenderTexture);
    }
    
    void OnDestroy()
    {
        Debug.Log($"Paint Target {gameObject.name} OnDestroy()");

        //maskRenderTexture.Release();
        //uvIslandsRenderTexture.Release();
        //extendIslandsRenderTexture.Release();
        //supportTexture.Release();
    }


}