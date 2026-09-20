using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace Rubber.Gameplay.Interaction.Rendering
{
    public sealed class InteractionOutlineRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] private Shader outlineShader;
        [SerializeField, ColorUsage(true, true)] private Color outlineColor = new(1f, 0.78f, 0.25f, 0.85f);
        [SerializeField, Range(1f, 8f)] private float outlineWidthPixels = 3f;

        private Material outlineMaterial;
        private InteractionOutlinePass outlinePass;

        public override void Create()
        {
            if (!outlineShader)
                outlineShader = Shader.Find("Hidden/Rubber/Interaction Outline Mask");

            CoreUtils.Destroy(outlineMaterial);
            if (outlineShader)
                outlineMaterial = CoreUtils.CreateEngineMaterial(outlineShader);

            outlinePass = new InteractionOutlinePass
            {
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents,
                requiresIntermediateTexture = true
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!outlineMaterial || !InteractionOutlineSelection.HasSelection ||
                renderingData.cameraData.cameraType != CameraType.Game)
                return;

            outlineMaterial.SetColor(ShaderIds.OutlineColor, outlineColor);
            outlineMaterial.SetFloat(ShaderIds.OutlineWidthPixels, outlineWidthPixels);
            outlinePass.Setup(outlineMaterial, InteractionOutlineSelection.SelectedRenderers);
            renderer.EnqueuePass(outlinePass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(outlineMaterial);
            outlineMaterial = null;
        }

        private static class ShaderIds
        {
            public static readonly int MaskTexture = Shader.PropertyToID("_InteractionOutlineMask");
            public static readonly int OutlineColor = Shader.PropertyToID("_OutlineColor");
            public static readonly int OutlineWidthPixels = Shader.PropertyToID("_OutlineWidthPixels");
        }

        private sealed class InteractionOutlinePass : ScriptableRenderPass
        {
            private sealed class MaskPassData
            {
                public Material material;
                public Renderer[] renderers;
            }

            private Material material;
            private Renderer[] renderers;

            public void Setup(Material passMaterial, Renderer[] selectedRenderers)
            {
                material = passMaterial;
                renderers = selectedRenderers;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (!material || renderers == null || renderers.Length == 0)
                    return;

                UniversalResourceData resources = frameData.Get<UniversalResourceData>();
                if (resources.isActiveTargetBackBuffer)
                    return;

                TextureHandle source = resources.activeColorTexture;
                TextureDesc maskDescriptor = renderGraph.GetTextureDesc(source);
                maskDescriptor.name = "_InteractionOutlineMask";
                maskDescriptor.colorFormat = GraphicsFormat.R8_UNorm;
                maskDescriptor.depthBufferBits = DepthBits.None;
                maskDescriptor.msaaSamples = MSAASamples.None;
                maskDescriptor.filterMode = FilterMode.Point;
                maskDescriptor.clearBuffer = true;
                maskDescriptor.clearColor = Color.black;
                TextureHandle mask = renderGraph.CreateTexture(maskDescriptor);

                using (IRasterRenderGraphBuilder builder =
                       renderGraph.AddRasterRenderPass<MaskPassData>("Interaction Outline Mask", out MaskPassData passData))
                {
                    passData.material = material;
                    passData.renderers = renderers;
                    builder.SetRenderAttachment(mask, 0, AccessFlags.Write);
                    builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.Read);
                    builder.SetGlobalTextureAfterPass(mask, ShaderIds.MaskTexture);
                    builder.SetRenderFunc(static (MaskPassData data, RasterGraphContext context) =>
                    {
                        foreach (Renderer targetRenderer in data.renderers)
                        {
                            if (!targetRenderer || !targetRenderer.enabled || !targetRenderer.gameObject.activeInHierarchy)
                                continue;

                            int subMeshCount = Mathf.Max(1, targetRenderer.sharedMaterials.Length);
                            for (int subMesh = 0; subMesh < subMeshCount; subMesh++)
                                context.cmd.DrawRenderer(targetRenderer, data.material, subMesh, 0);
                        }
                    });
                }

                TextureDesc destinationDescriptor = renderGraph.GetTextureDesc(source);
                destinationDescriptor.name = "Interaction Outline Composite";
                destinationDescriptor.clearBuffer = false;
                TextureHandle destination = renderGraph.CreateTexture(destinationDescriptor);
                var parameters = new RenderGraphUtils.BlitMaterialParameters(source, destination, material, 1);
                renderGraph.AddBlitPass(parameters, "Interaction Outline Composite");
                resources.cameraColor = destination;
            }
        }
    }
}
