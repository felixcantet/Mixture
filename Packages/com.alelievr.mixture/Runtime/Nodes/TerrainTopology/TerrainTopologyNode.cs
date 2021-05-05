using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GraphProcessor;
using System.Linq;
using UnityEngine.Rendering;

namespace Mixture
{
    [System.Serializable, NodeMenuItem("Compute/TerrainTopologyNode")]
    public class TerrainTopologyNode : ComputeShaderNode
    {
        [ShowInInspector] [Input] public Texture HeightMap;
        [Input] public float CellLength;
        [Input] public float TerrainHeight;
        [Input] public Texture Gradiant;
        [Output] public CustomRenderTexture Output;
        public bool visible = false;
        [VisibleIf("visible", true)] public float IsThisShow;
        RenderTexture smoothed;
        Texture2D Gradient;
        Texture2D PosGradient;
        Texture2D NegGradient;
        public override string name => "TerrainTopologyNode";

        // Override this if you want your node to not support certartain dimensions
        // public override List<OutputDimension> supportedDimensions => new List<OutputDimension>() {
        // 	OutputDimension.Texture2D,
        //     OutputDimension.Texture2D,
        //     OutputDimension.Cubemap,
        // };

        protected override string computeShaderResourcePath => "Mixture/TerrainCurvatureComputer";
        public override bool showDefaultInspector => true;
        // In case you want to change the compute
        // protected override string previewKernel => null;
        // public override string previewTexturePropertyName => previewComputeProperty;
        public override Texture previewTexture => Output;

        int smoothKernel;
        int curvaturekernel;
        int aspectKernel;

        public enum VISUALIZE_GRADIENT { WARM, COOL, COOL_WARM, GREY_WHITE, GREY_BLACK, BLACK_WHITE };

        private void CreateGradients(bool colored)
        {
            if (colored)
            {
                Gradient = CreateGradient(VISUALIZE_GRADIENT.COOL_WARM);
                PosGradient = CreateGradient(VISUALIZE_GRADIENT.WARM);
                NegGradient = CreateGradient(VISUALIZE_GRADIENT.COOL);
            }
            else
            {
                Gradient = CreateGradient(VISUALIZE_GRADIENT.BLACK_WHITE);
                PosGradient = CreateGradient(VISUALIZE_GRADIENT.GREY_WHITE);
                NegGradient = CreateGradient(VISUALIZE_GRADIENT.GREY_BLACK);
            }

            Gradient.Apply();
            PosGradient.Apply();
            NegGradient.Apply();
        }


        private Texture2D CreateGradient(VISUALIZE_GRADIENT g)
        {
            switch (g)
            {
                case VISUALIZE_GRADIENT.WARM:
                    return CreateWarmGradient();

                case VISUALIZE_GRADIENT.COOL:
                    return CreateCoolGradient();

                case VISUALIZE_GRADIENT.COOL_WARM:
                    return CreateCoolToWarmGradient();

                case VISUALIZE_GRADIENT.GREY_WHITE:
                    return CreateGreyToWhiteGradient();

                case VISUALIZE_GRADIENT.GREY_BLACK:
                    return CreateGreyToBlackGradient();

                case VISUALIZE_GRADIENT.BLACK_WHITE:
                    return CreateBlackToWhiteGradient();
            }

            return null;
        }

        private Texture2D CreateWarmGradient()
        {
            var gradient = new Texture2D(5, 1, TextureFormat.ARGB32, false, true);
            gradient.SetPixel(0, 0, new Color32(80, 230, 80, 255));
            gradient.SetPixel(1, 0, new Color32(180, 230, 80, 255));
            gradient.SetPixel(2, 0, new Color32(230, 230, 80, 255));
            gradient.SetPixel(3, 0, new Color32(230, 180, 80, 255));
            gradient.SetPixel(4, 0, new Color32(230, 80, 80, 255));
            gradient.wrapMode = TextureWrapMode.Clamp;

            return gradient;
        }

        private Texture2D CreateCoolGradient()
        {
            var gradient = new Texture2D(5, 1, TextureFormat.ARGB32, false, true);
            gradient.SetPixel(0, 0, new Color32(80, 230, 80, 255));
            gradient.SetPixel(1, 0, new Color32(80, 230, 180, 255));
            gradient.SetPixel(2, 0, new Color32(80, 230, 230, 255));
            gradient.SetPixel(3, 0, new Color32(80, 180, 230, 255));
            gradient.SetPixel(4, 0, new Color32(80, 80, 230, 255));
            gradient.wrapMode = TextureWrapMode.Clamp;

            return gradient;
        }

        private Texture2D CreateCoolToWarmGradient()
        {
            var gradient = new Texture2D(9, 1, TextureFormat.ARGB32, false, true);
            gradient.SetPixel(0, 0, new Color32(80, 80, 230, 255));
            gradient.SetPixel(1, 0, new Color32(80, 180, 230, 255));
            gradient.SetPixel(2, 0, new Color32(80, 230, 230, 255));
            gradient.SetPixel(3, 0, new Color32(80, 230, 180, 255));
            gradient.SetPixel(4, 0, new Color32(80, 230, 80, 255));
            gradient.SetPixel(5, 0, new Color32(180, 230, 80, 255));
            gradient.SetPixel(6, 0, new Color32(230, 230, 80, 255));
            gradient.SetPixel(7, 0, new Color32(230, 180, 80, 255));
            gradient.SetPixel(8, 0, new Color32(230, 80, 80, 255));
            gradient.wrapMode = TextureWrapMode.Clamp;

            return gradient;
        }

        private Texture2D CreateGreyToWhiteGradient()
        {
            var gradient = new Texture2D(3, 1, TextureFormat.ARGB32, false, true);
            gradient.SetPixel(0, 0, new Color32(128, 128, 128, 255));
            gradient.SetPixel(1, 0, new Color32(192, 192, 192, 255));
            gradient.SetPixel(2, 0, new Color32(255, 255, 255, 255));
            gradient.wrapMode = TextureWrapMode.Clamp;

            return gradient;
        }

        private Texture2D CreateGreyToBlackGradient()
        {
            var gradient = new Texture2D(3, 1, TextureFormat.ARGB32, false, true);
            gradient.SetPixel(0, 0, new Color32(128, 128, 128, 255));
            gradient.SetPixel(1, 0, new Color32(64, 64, 64, 255));
            gradient.SetPixel(2, 0, new Color32(0, 0, 0, 255));
            gradient.wrapMode = TextureWrapMode.Clamp;

            return gradient;
        }

        private Texture2D CreateBlackToWhiteGradient()
        {
            var gradient = new Texture2D(5, 1, TextureFormat.ARGB32, false, true);
            gradient.SetPixel(0, 0, new Color32(0, 0, 0, 255));
            gradient.SetPixel(1, 0, new Color32(64, 64, 64, 255));
            gradient.SetPixel(2, 0, new Color32(128, 128, 128, 255));
            gradient.SetPixel(3, 0, new Color32(192, 192, 192, 255));
            gradient.SetPixel(4, 0, new Color32(255, 255, 255, 255));
            gradient.wrapMode = TextureWrapMode.Clamp;

            return gradient;
        }

        protected override void Enable()
        {
            base.Enable();
            
            CreateGradients(true);

            rtSettings.outputChannels = OutputChannel.RGBA;

            rtSettings.outputPrecision = OutputPrecision.Full;
            rtSettings.editFlags = EditFlags.Dimension | EditFlags.Size;


            UpdateTempRenderTexture(ref Output);

            smoothKernel = computeShader.FindKernel("SmoothHeight");
            curvaturekernel = computeShader.FindKernel("CurvatureComputer");
            aspectKernel = computeShader.FindKernel("AspectComputer");
        }

        protected override bool ProcessNode(CommandBuffer cmd)
        {
            if (!base.ProcessNode(cmd) || HeightMap == null)
                return false;

            UpdateTempRenderTexture(ref Output);
            cmd.SetComputeFloatParam(computeShader, "cellLength", this.CellLength);
            cmd.SetComputeFloatParam(computeShader, "terrainHeight", this.TerrainHeight);

            //MixtureUtils.SetupComputeTextureDimension(cmd, computeShader, HeightMap.dimension);
            if (smoothed == null || !smoothed.IsCreated())
            {
                smoothed = new RenderTexture(HeightMap.width, HeightMap.height, 0);
                smoothed.enableRandomWrite = true;
                smoothed.Create();
            }
            else
            {
                cmd.SetRenderTarget(smoothed);
                cmd.ClearRenderTarget(true, true, Color.white);
            }

            cmd.SetComputeTextureParam(computeShader, smoothKernel, "_HeightMap", HeightMap);
            cmd.SetComputeTextureParam(computeShader, smoothKernel, "_SmoothedHeightMap", smoothed);
            DispatchCompute(cmd, smoothKernel, Output.width, Output.height);

            cmd.SetComputeTextureParam(computeShader, curvaturekernel, "_HeightMap", smoothed);
            cmd.SetComputeTextureParam(computeShader, curvaturekernel, "_Output", this.Output);
            cmd.SetComputeTextureParam(computeShader, curvaturekernel, "_Gradient", this.Gradient);
            cmd.SetComputeTextureParam(computeShader, curvaturekernel, "_PosGradient", this.PosGradient);
            cmd.SetComputeTextureParam(computeShader, curvaturekernel, "_NegGradient", this.NegGradient);
            //MixtureUtils.SetTextureWithDimension(cmd, computeShader, curvaturekernel, "_HeightMap", this.HeightMap);
            //MixtureUtils.SetTextureWithDimension(cmd, computeShader, curvaturekernel, "_Output", this.Output);
            DispatchCompute(cmd, curvaturekernel, Output.width, Output.height);
            //smooth.Release();
            return true;
        }

        protected override void Destroy()
        {
            base.Destroy();
            smoothed.Release();
        }
        protected override void Disable()
        {
            base.Disable();
            smoothed.Release();
        }
    }
}