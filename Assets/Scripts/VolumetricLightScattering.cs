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
        // reference to camera color target
        private RenderTargetIdentifier cameraColorTargetIdent;
        
        // filtering settings: indicates which render queue range is allowed.
        // opaque, transparent or all
        private FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
        
        // list of shader tag ids to keep track of
        private readonly List<ShaderTagId> shaderTagIdList = new List<ShaderTagId>();

        private static readonly int _centerID = Shader.PropertyToID("_Center");
        private static readonly int _intensityID = Shader.PropertyToID("_Intensity");
        private static readonly int _blurWidthID = Shader.PropertyToID("_BlurWidth");
        
        // create RenderTargetHandle to create a texture
        private readonly RenderTargetHandle occluders = RenderTargetHandle.CameraTarget;
        
        // properties defined in settings
        private readonly float resolutionScale; // resolution scale
        private readonly float intensity; // effect intensity
        private readonly float blurWidth; // radial blur width
        
        // material references
        private readonly Material occludersMaterial; // occluders material for override
        private readonly Material radialBlurMaterial; // radial blur material instance
        
        // declare a constructor to initialize render pass variables
        // inject feature class settings instance
        public LightScatteringPass(VolumetricLightScatteringSettings settings)
        {
            occluders.Init("_OccludersMap");
            resolutionScale = settings.resolutionScale;
            intensity = settings.intensity;
            blurWidth = settings.blurWidth;

            occludersMaterial = new Material(Shader.Find("Hidden/UnlitColor"));
            radialBlurMaterial = new Material(Shader.Find("Hidden/RadialBlur"));
            
            // occluder shaders that can potentially block the light source
            shaderTagIdList.Add(new ShaderTagId("UniversalForward"));
            shaderTagIdList.Add(new ShaderTagId("UniversalForwardOnly"));
            shaderTagIdList.Add(new ShaderTagId("LightweightForward"));
            shaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));
        }

        public void SetCameraColorTarget(RenderTargetIdentifier cameraColorTargetIdentifier)
        {
            this.cameraColorTargetIdent = cameraColorTargetIdentifier;
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
            // Stop rendering pass if material is missing
            if (!occludersMaterial || !radialBlurMaterial) return;

            // Issue graphic commands via CommandBuffer
            // CommandBufferPool is just a collection of pre-created command buffers that are ready to use.
            // You can request one using Get().
            CommandBuffer cmd = CommandBufferPool.Get();

            // Wrap the graphic commands inside a ProfilingScope,
            // which ensures that FrameDebugger can profile the code.
            using (new ProfilingScope(cmd, 
                       new ProfilingSampler("VolumetricLightScattering")))
            {
                // prepare the CommandBuffer so commands can be added
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                // RenderingData provides information about the scene.
                Camera camera = renderingData.cameraData.camera; // get camera reference from RenderingData
                context.DrawSkybox(camera); // DrawSkybox needs a reference to the camera.

                // Describe how to sort objects and which shader passes are allowed.
                DrawingSettings drawSettings = 
                    CreateDrawingSettings(
                        shaderTagIdList, // list of shader passes
                        ref renderingData, // reference to RenderingData
                        SortingCriteria.CommonOpaque // Sorting criteria for visible objects
                    );

                // Replace the object's materials with occludersMaterial
                drawSettings.overrideMaterial = occludersMaterial;
                
                // DrawRenderers handles the acutal draw call.
                // It needs to know which objects are currently visible with culling results.
                // Drawing settings and filtering settings must also be supplied.
                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filteringSettings);

                // get a reference to sun from RenderSettings
                Vector3 sunDirectionWorldSpace = RenderSettings.sun.transform.forward;
                // get camera position
                Vector3 cameraPositionWorldSpace = camera.transform.position;
                // unit vector that goes from camera towards the sun.
                // use this for sun position
                Vector3 sunPositionWorldSpace = cameraPositionWorldSpace + sunDirectionWorldSpace;
                // shader expects a viewport space position.
                // convert world space positions to viewport space.
                Vector3 sunPositionViewportSpace = camera.WorldToViewportPoint(sunPositionWorldSpace);
                
                // pass data to shader
                // only x and y components of sunPositionViewportSpace are needed
                // since it represents pixel position of screen
                radialBlurMaterial.SetVector(_centerID, 
                    new Vector4(sunPositionViewportSpace.x, sunPositionViewportSpace.y, 0, 0));
                radialBlurMaterial.SetFloat(_intensityID, intensity);
                radialBlurMaterial.SetFloat(_blurWidthID, blurWidth);

                // blur the occluders map.
                // Blit() copies a source texture into a destination texture using a shader.
                // Executes shader with occluders as source texture, then stores output in camera color target.
                Blit(cmd, occluders.Identifier(), cameraColorTargetIdent, radialBlurMaterial);
            }
            
            // After adding all commands to the CommandBuffer, schedule it for execution and release it.
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        // Clean up any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            // Clean up resources allocated when executing this render pass.
            cmd.ReleaseTemporaryRT(occluders.id);
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
        
        // pass camera color target to render pass, required by Blit()
        m_ScriptablePass.SetCameraColorTarget(renderer.cameraColorTarget);
    }
}
