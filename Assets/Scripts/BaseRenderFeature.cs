using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public abstract class BaseRenderFeature : ScriptableRendererFeature
{
    protected enum RenderQueueRangeOptions // your custom enumeration
    {
        All, 
        Opaque, 
        Transparent,
    }
    [SerializeField] protected  RenderQueueRangeOptions m_RenderQueueRange = RenderQueueRangeOptions.All;
    [SerializeField] protected LayerMask m_LayerMask;
    [SerializeField] protected  Material m_Material;
    [SerializeField] protected  RenderPassEvent m_RenderPassEvent = RenderPassEvent.AfterRenderingSkybox;
    private ScriptableRenderPass m_ScriptablePass;
    
    public override void Create()
    {
        var renderQueueRange = m_RenderQueueRange switch
        {
            RenderQueueRangeOptions.All => RenderQueueRange.all,
            RenderQueueRangeOptions.Opaque => RenderQueueRange.opaque,
            RenderQueueRangeOptions.Transparent => RenderQueueRange.transparent,
            _ => RenderQueueRange.all
        };

        m_ScriptablePass = InitializeRenderPass(renderQueueRange);
        m_ScriptablePass.renderPassEvent = m_RenderPassEvent;
    }

    protected virtual ScriptableRenderPass InitializeRenderPass(RenderQueueRange renderQueueRange)
    {
        throw new System.NotImplementedException();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
        
    }
}
