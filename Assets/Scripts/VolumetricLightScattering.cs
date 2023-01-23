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
    class LightScatteringPass : ScriptableRenderPass
    {
        // create RenderTargetHandle to create a texture
        private readonly RenderTargetHandle occluders = RenderTargetHandle.CameraTarget;
        // defined in settings
        private readonly float resolutionScale; // resolution scale
        private readonly float intensity; // effect intensity
        private readonly float blurWidth; // radial blur width

        // declare a constructor to initialize render pass variables
        // inject feature class settings instance
        public LightScatteringPass(VolumetricLightScatteringSettings settings)
        {
            occluders.Init("_OccludersMap");
            resolutionScale = settings.resolutionScale;
            intensity = settings.intensity;
            blurWidth = settings.blurWidth;
        }

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // create an off-screen texture to store the silhouettes of all the objects that occlude the light source.
            
            // get a copy of the current camera's RenderTextureDescriptor.
            // This descriptor contains all the information needed to create a new texture.
            RenderTextureDescriptor cameraTextureDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            
            // Disable depth buffer. (Not used)
            cameraTextureDescriptor.depthBufferBits = 0;
            
            // scale texture dimensions by resolutionScale
            cameraTextureDescriptor.width = Mathf.RoundToInt(cameraTextureDescriptor.width * resolutionScale);
            cameraTextureDescriptor.height = Mathf.RoundToInt(cameraTextureDescriptor.height * resolutionScale);
            
            // create a new texture.
            // The first parameter is the ID of occluders.
            // The second parameter is the texture configuration from the descriptor created and
            // the third is the texture filtering mode.
            // https://docs.unity3d.com/ScriptReference/Rendering.CommandBuffer.GetTemporaryRT.html
            cmd.GetTemporaryRT(occluders.id, cameraTextureDescriptor, FilterMode.Bilinear);
            
            // finish configuration with RenderTargetIdentifier
            ConfigureTarget(occluders.Identifier());
            
            // *** NOTE ***
            // It’s important to understand that you issue all rendering commands through a CommandBuffer.
            // You set up the commands you want to execute and then hand them over to the scriptable render pipeline
            // to actually run them. You should NEVER call CommandBuffer.SetRenderTarget().
            // Instead, call ConfigureTarget() and ConfigureClear().
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
        renderer.EnqueuePass(m_ScriptablePass);
    }
}


