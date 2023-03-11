using UnityEngine;
using UnityEngine.Rendering.Universal;

[System.Serializable] // Make the attributes editable through the inspector.
public class VolumetricLightScatteringSettings
{
    [Header("Properties")]
    
    [Range(0.1f, 1f)] // Configures the size of the off-screen texture.
    public float resolutionScale = 0.5f;
    
    [Range(0.0f, 1.0f)] // Manages the brightness of the light rays generated.
    public float intensity = 1.0f;

    [Range(0.0f, 1.0f)] // The radius of the blur used when combining the pixel colors.
    public float blurWidth = 0.85f;
}

public class VolumetricLightScattering : ScriptableRendererFeature
{
    LightScatteringPass m_ScriptablePass;

    public VolumetricLightScatteringSettings settings = new VolumetricLightScatteringSettings();

    // Called when the feature first loads.
    // Use it to create and configure all ScriptableRenderPass instances.
    public override void Create()
    {
        // pass constructor, inject settings as argument
        m_ScriptablePass = new LightScatteringPass(settings);

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    // Called every frame, once per camera.
    // Use it to inject your ScriptableRenderPass instances into the ScriptableRenderer.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // pass camera color target to render pass, required by Blit()
        m_ScriptablePass.SetCameraColorTarget(renderer.cameraColorTarget);
        
        renderer.EnqueuePass(m_ScriptablePass);
    }
}
