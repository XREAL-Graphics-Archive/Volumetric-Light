// execute this shader on the occluders texture map and 
// overlay the resulting color on top of the main camera color texture

Shader "Hidden/RadialBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurWidth("Blur Width", Range(0,1)) = 0.85
        _Intensity("Intensity", Range(0,1)) = 1
        
        // screen space coord of sun, origin point of radial blur
        _Center("Center", Vector) = (0.5, 0.5, 0, 0)
    }
    SubShader
    {
        Blend One One // additive blending, adds images' values to the color channels and clamps to max 1.

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (Attributes v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.uv = v.uv;
                return o;
            }

            Texture2D _MainTex;
            SamplerState sampler_MainTex;

            #define NUM_SAMPLES 100 // number of samples to take to blur image

            float _BlurWidth;
            float _Intensity;
            float4 _Center;

            half4 frag (v2f i) : SV_Target
            {
                // declare default color as black
                half4 color = half4(0, 0, 0, 1);

                // calculate ray that goes from center point towards the current pixel UV coordinates
                float2 ray = i.uv - _Center.xy;

                // sample texture along ray and accumulate fragment color
                for(int k = 0 ; k < NUM_SAMPLES ; k++)
                {
                    float scale = 1.0f - _BlurWidth * (float(k) / float(NUM_SAMPLES - 1));
                    color.xyz +=
                        _MainTex.Sample(
                            sampler_MainTex,
                            ray * scale + _Center.xy
                        ).xyz / float(NUM_SAMPLES);
                }

                // multiply color by intensity
                return color * _Intensity;
            }
            ENDHLSL
        }
    }
}
