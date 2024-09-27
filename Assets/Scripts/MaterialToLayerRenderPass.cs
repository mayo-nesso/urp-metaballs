using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

// This class defines a custom render pass for Unity's Universal Render Pipeline (URP)
// It's designed to render objects from specific layers using a custom material,
// and then blit the result back to the main render target
public class MaterialToLayerRenderPass : ScriptableRenderPass
{
    // Scale and bias for the blit operation, set to (1,1,0,0) for a straight copy
    // X: scaleX = 1: No horizontal scaling, Y: scaleY = 1: No vertical scaling
    // Z: biasX = 0: No horizontal offset, W: biasY = 0: No vertical offset
    private static readonly Vector4 ScaleBias = new(1f, 1f, 0f, 0f);
    
    // The material to be applied in this render pass
    private readonly Material _material = default;
        
    // List of shader tags used to build the renderer list
    // This ensures compatibility with different shader types in URP
    private readonly List<ShaderTagId> _shaderTagIdList = new();
    
    // Descriptor for the temporary render texture used in this pass
    private RenderTextureDescriptor _tempTextureDescriptor = default;
    
    // Settings to filter which renderers should be drawn
    private readonly FilteringSettings _filterSettings = default; 
    
    public MaterialToLayerRenderPass(LayerMask layerMask, Material material, RenderQueueRange renderQueueRange)
    {
        // Set up filtering settings based on the provided layer mask and render queue range
        _filterSettings = new FilteringSettings(renderQueueRange, layerMask);
        _material = material;
        
        // Define shader tags for forward rendering passes
        // This ensures compatibility with both URP-specific and legacy shaders
        var forwardOnlyShaderTagIds = new[]
        {
            new ShaderTagId("UniversalForwardOnly"),
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("SRPDefaultUnlit"), // Legacy shaders (do not have a gbuffer pass) are considered forward-only for backward compatibility
            new ShaderTagId("LightweightForward") // Legacy shaders (do not have a gbuffer pass) are considered forward-only for backward compatibility
        };
        _shaderTagIdList.Clear();
        foreach (var sid in forwardOnlyShaderTagIds)
        {
            _shaderTagIdList.Add(sid);
        }
        
        // Initialize _tempTextureDescriptor with screen dimensions,
        // will be updated with camera dimensions later
        _tempTextureDescriptor = new RenderTextureDescriptor(Screen.width, Screen.height);
    }
    
    // Data structure for the layer render pass (First pass)
    private class LayerRenderPassData
    {
        public RendererListHandle RendererListHandle = default;
    }
    
    // Data structure for the blit pass (Second pass)
    private class BlitPassData
    {
        public Material Material = default;
        public TextureHandle Source = default;
    }
    
    private static void ExecuteLayerRenderPass(LayerRenderPassData data, RasterGraphContext context)
    {
        // Draw all renderers in the list
        context.cmd.DrawRendererList(data.RendererListHandle);
    }
    
    private static void ExecuteBlitPass(BlitPassData data, RasterGraphContext context)
    {
        // Blit the source texture to the current render target using the specified material
        Blitter.BlitTexture(context.cmd, data.Source, ScaleBias, data.Material, 0);
    }
    
    private void InitRendererLists(ContextContainer frameData, ref LayerRenderPassData layerRenderPassData, RenderGraph renderGraph)
    {
        // Access the relevant frame data from the Universal Render Pipeline
        var universalRenderingData = frameData.Get<UniversalRenderingData>();
        var cameraData = frameData.Get<UniversalCameraData>();
        var lightData = frameData.Get<UniversalLightData>();
        var sortingCriteria = cameraData.defaultOpaqueSortFlags;
        
        // Create drawing settings based on the shader tags and frame data
        var drawSettings = RenderingUtils.CreateDrawingSettings(_shaderTagIdList, universalRenderingData, cameraData, lightData, sortingCriteria);
        
        // Create renderer list parameters,
        // here we are using _filterSettings, and that is where we specified the layerMask to use
        var param = new RendererListParams(universalRenderingData.cullResults, drawSettings, _filterSettings);
        
        // Finally create a RenderListHandle 
        layerRenderPassData.RendererListHandle = renderGraph.CreateRendererList(param);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var resourceData = frameData.Get<UniversalResourceData>();
        // The following line ensures that the render pass doesn't blit
        // from the back buffer.
        if (resourceData.isActiveTargetBackBuffer)
        {
            return;
        }
        
        // Get handles for the active color and depth textures
        var srcCamColor = resourceData.activeColorTexture;
        var srcCamDepth = resourceData.activeDepthTexture;
        
        // This check is to avoid an error from the material preview in the scene
        if (!srcCamColor.IsValid() || !srcCamDepth.IsValid()) 
        {
#if UNITY_EDITOR
            Debug.LogWarning("MaterialToLayerRenderPass: Invalid source color or depth texture.");
#endif
            return;
        }
        
        // Update _tempTextureDescriptor width & height according camera info from frame data 
        var cameraData = frameData.Get<UniversalCameraData>();
        _tempTextureDescriptor.width = cameraData.cameraTargetDescriptor.width;
        _tempTextureDescriptor.height = cameraData.cameraTargetDescriptor.height;
        
        // Create a temporary render texture handle for this pass
        var temporaryHandle = UniversalRenderer.CreateRenderGraphTexture(renderGraph,
            _tempTextureDescriptor, "Mat2Layer: k_TempTexture", false);
        
        // This check is to avoid an error from the material preview in the scene
        if (!temporaryHandle.IsValid())
        {
#if UNITY_EDITOR
            Debug.LogWarning("MaterialToLayerRenderPass: destination texture is invalid.");
#endif
            return;
        }
        
        // Set up the layer render pass
        const string layerRenderPassName = "Mat2Layer: Layer Render 1/2";
        using (var builder = renderGraph.AddRasterRenderPass<LayerRenderPassData>(layerRenderPassName, out var passData))
        {
            InitRendererLists(frameData, ref passData, renderGraph);
            // Here we instruct to build the graph using our passData.RendererListHandle
            // that was created with our layer filter setting,
            // ie; what we are going to retrieve to 'draw' in this pass will be only 
            // what is on that specific layer...
            builder.UseRendererList(passData.RendererListHandle);
            
            // Set up texture dependencies
            // We are not really using 'srcCamColor' on this pass,
            // but we are going to keep the next line for clarity and documentation... 
            builder.UseTexture(srcCamColor); 
            builder.SetRenderAttachment(temporaryHandle, 0);
            builder.SetRenderAttachmentDepth(srcCamDepth);
            
            builder.SetRenderFunc((LayerRenderPassData data, RasterGraphContext context) => ExecuteLayerRenderPass(data, context));
        }
        
        // Set up the blit pass
        const string blitPassName = "Mat2Layer: Blit Pass 2/2";
        using (var builder = renderGraph.AddRasterRenderPass<BlitPassData>(blitPassName, out var passData))
        {
            // Configure pass data
            passData.Material = _material;
            // Use the output of the previous pass as the input
            passData.Source = temporaryHandle;
            builder.UseTexture(passData.Source);
            
            // Set the render target to the original color buffer
            builder.SetRenderAttachment(srcCamColor, 0);
            builder.SetRenderAttachmentDepth(srcCamDepth);
            
            builder.SetRenderFunc((BlitPassData data, RasterGraphContext context) => ExecuteBlitPass(data, context));
        }
    }
}
