Shader "Dynamic Smoke System"
{

    Properties
    {
        _Color("Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _ShadowColor("ShadowColor", Color) = (1.0, 1.0, 1.0, 1.0)
        _CellShadingRamp("Cell Shading Ramp", 2D) = "" {}
        
        _NoiseTex("Noise Big", 3D) = "" {}
        _NoiseDetailTex("Noise Detail", 3D) = "" {}
        
        _TimeScale("Timescale", Float) = 5
        _Scale("Noise Scale", Float) = 0.3

        lightAbsorptionThroughCloud("Light Absorption Through Cloud", float) = 1.05
        lightAbsorptionTowardSun("lightAbsorptionTowardSun", float) = 1.6
        darknessThreshold("darknessThreshold", float) = 0.1
        forwardScattering("forwardScattering", float) = 0.73
        backScattering("backScattering", float) = 0.33
        baseBrightness("baseBrightness", float) = 0.75
        phaseFactor("phaseFactor", float) = 0.95
        _densityStepCount("Density Step Count", int) = 100
        _lightMarchSteps("Lightmarch Steps", int) = 8
        _noiseWeights("noiseWeights", Vector) = (6.66, 2.43, 2.85)
        _noiseMin("Noise Min", Float) = 0
        _noiseMax("Noise Max", Float) = 1
        _SdfTexture("SDF Texture", 3D) = "" {}
        _Mask("Mask t", Float) = 0
        _MaskPos("Mask pos", Vector) = (0, 0, 0)
        _BlueNoise("Blue Noise", 2D) = "" {}
        _EdgeScale("Edge Scale", Float) = 1
        _EdgeSpeed("Edge Speed", Float) = 1
        _EdgeIntensity("Edge Intensity", Float) = 1
        _OmissionStrength("Omission Strength", Float) = 0.5
        _DepthFade("Depth Fade Distance", Float) = 1
        _DepthBias("Depth Bias", Float) = 0.25
        _Zwrite("Zwrite", Int) = 0
        _NoiseSize("Noise Size", Float) = 20
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "DisableBatching" = "True"
        }

        Cull Off
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite [_Zwrite]
            ZTest LEqual

            HLSLPROGRAM

            float _Radius;
            Texture3D _NoiseTex;
            SamplerState sampler_NoiseTex;
            Texture3D _NoiseDetailTex;
            SamplerState sampler_NoiseDetailTex;
            float _TimeScale;
            float _Scale;
            SamplerState sampler_SdfTexture;
            Texture3D _SdfTexture;
            float3 _boundsMin;
            float3 _boundsMax;
            float _EdgeIntensity;
            float _EdgeSpeed;
            float _EdgeScale;
            float _Mask;
            sampler2D _BlueNoise;
            float3 _MaskPos;
            float _OmissionStrength;
            float _DepthFade;
            float _DepthBias = 0.2;
            
            #ifdef CELLSHADING_ON
            sampler2D _CellShadingRamp;
            #endif
            
            float invLerp(float from, float to, float value)
            {
                return (value - from) / (to - from);
            }

            float4 invLerp(float4 from, float4 to, float4 value)
            {
                return (value - from) / (to - from);
            }

            float3 invLerp(float3 from, float3 to, float3 value)
            {
                return (value - from) / (to - from);
            }

            float remap(float origFrom, float origTo, float targetFrom, float targetTo, float value)
            {
                float rel = invLerp(origFrom, origTo, value);
                return lerp(targetFrom, targetTo, rel);
            }

            float4 remap(float4 origFrom, float4 origTo, float4 targetFrom, float4 targetTo, float4 value)
            {
                float4 rel = invLerp(origFrom, origTo, value);
                return lerp(targetFrom, targetTo, rel);
            }

            inline float DistanceFunction(float3 wpos)
            {
                float3 uvw = invLerp(_boundsMin, _boundsMax, wpos);

                if (uvw.x > 1 || uvw.y > 1 || uvw.z > 1 || uvw.x < 0 || uvw.y < 0 || uvw.z < 0) return 0.1;

                float4 sample = _SdfTexture.SampleLevel(sampler_SdfTexture, uvw, 0);
                float sdf = sample.r;

                // convert texture sample to real SDF
                if (sdf >= 1) return 0.1;
                sdf = (1 - sdf) * -_Radius;

                // apply omissions channel
                sdf = lerp(sdf, 1, invLerp(0, 1 - clamp(0, 0.95, _OmissionStrength), sample.g));

                return sdf;
            }

            #include "Includes.cginc"
            #include "ForwardBaseUnlit.cginc"
            
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile_fwdbase
            #pragma multi_compile CELLSHADING_ON CELLSHADING_OFF
            #pragma multi_compile ALPHA_DITHER_ON ALPHA_DITHER_OFF
            ENDHLSL
        }

    }

    Fallback Off
    
    CustomEditor "DynamicSmokeSystem.DynamicSmokeEditor"
}