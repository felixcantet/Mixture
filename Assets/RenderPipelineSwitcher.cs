using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class RenderPipelineSwitcher : MonoBehaviour
{
    public RenderPipelineAsset renderPipeline;

    void Update()
    {
        if (GraphicsSettings.renderPipelineAsset != renderPipeline)
            GraphicsSettings.renderPipelineAsset = renderPipeline;
    }
}
