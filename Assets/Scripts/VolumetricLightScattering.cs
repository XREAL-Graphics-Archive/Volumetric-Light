using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
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
    class CustomRenderPass : ScriptableRenderPass
    {
        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit,
        // the render pipeline will call it at specific points in the pipeline.
        // Called every frame to run the rendering logic
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            
        }

        // Before you execute the render pass to configure render targets,
        // you can call this function instead, it executes right after OnCameraSetup()
        // public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        // {
        //     
        // }

        // This function is called once after rendering the last camera in the camera stack.
        // You can use this to clean up any allocated resources once all cameras in the stack have finished rendering.
        // public override void OnFinishCameraStackRendering(CommandBuffer cmd)
        // {
        //     
        // }
    }

    CustomRenderPass m_ScriptablePass;

    public VolumetricLightScatteringSettings settings = new VolumetricLightScatteringSettings();

    // Called when the feature first loads.
    // Use it to create and configure all ScriptableRenderPass instances.
    public override void Create()
    {
        m_ScriptablePass = new CustomRenderPass();

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    // Called every frame, once per camera.
    // Use it to inject your ScriptableRenderPass instances into the ScriptableRenderer.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }
}


