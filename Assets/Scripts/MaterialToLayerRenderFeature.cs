using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class MaterialToLayerRenderFeature : BaseRenderFeature
{
    protected override ScriptableRenderPass InitializeRenderPass(RenderQueueRange renderQueueRange)
    {
        return new MaterialToLayerRenderPass(m_LayerMask, m_Material, renderQueueRange);
    }
}
