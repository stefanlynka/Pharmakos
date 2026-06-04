#ifndef PHARMAKOS_LIT_TERRAIN_MAPPING_INCLUDED
#define PHARMAKOS_LIT_TERRAIN_MAPPING_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

// Separate cbuffer — LitInput.hlsl already owns UnityPerMaterial.
CBUFFER_START(LitTerrainPerMaterial)
    half _POMSteps;
    half _POMRefineSteps;
    half _DirectOcclusionStrength;
    half _HeightMapChannel;
CBUFFER_END

// Matches URP Lit height sampling (.g). Set _HeightMapChannel to 0 for .r-only height maps.
half SampleHeightMap(float2 uv)
{
    half4 heightSample = SAMPLE_TEXTURE2D(_ParallaxMap, sampler_ParallaxMap, uv);
    return lerp(heightSample.g, heightSample.r, saturate(_HeightMapChannel));
}

// Parallax Occlusion Mapping with optional binary refinement.
float2 ParallaxOcclusionMapping(float2 uv, half3 viewDirTS, half parallaxScale, int stepCount, int refineCount)
{
    viewDirTS = normalize(viewDirTS);
    viewDirTS.xy *= parallaxScale;
    viewDirTS.z = max(viewDirTS.z, 1e-4h);

    const half layerDepth = 1.0h / stepCount;
    half currentLayerDepth = 0.0h;

    float2 currentUV = uv;
    half currentHeight = SampleHeightMap(currentUV);
    float2 deltaUV = viewDirTS.xy / viewDirTS.z;

    UNITY_LOOP
    for (int i = 0; i < stepCount; i++)
    {
        if (currentLayerDepth >= currentHeight)
            break;

        currentUV -= deltaUV * layerDepth;
        currentHeight = SampleHeightMap(currentUV);
        currentLayerDepth += layerDepth;
    }

    float2 prevUV = currentUV + deltaUV * layerDepth;
    half prevLayerDepth = currentLayerDepth - layerDepth;
    half prevHeight = SampleHeightMap(prevUV);
    half afterHeight = currentHeight - currentLayerDepth;
    half beforeHeight = prevHeight - prevLayerDepth;
    half weight = afterHeight / (afterHeight - beforeHeight + 1e-5h);
    float2 parallaxUV = lerp(currentUV, prevUV, saturate(weight));

    currentUV = parallaxUV;
    currentHeight = SampleHeightMap(currentUV);
    currentLayerDepth = weight * layerDepth + prevLayerDepth;

    UNITY_LOOP
    for (int j = 0; j < refineCount; j++)
    {
        float2 midUV = lerp(prevUV, currentUV, 0.5);
        half midHeight = SampleHeightMap(midUV);
        half midDepth = lerp(prevLayerDepth, currentLayerDepth, 0.5);

        if (midHeight < midDepth)
        {
            currentUV = midUV;
            currentHeight = midHeight;
            currentLayerDepth = midDepth;
        }
        else
        {
            prevUV = midUV;
            prevHeight = midHeight;
            prevLayerDepth = midDepth;
        }
    }

    return currentUV;
}

half LitTerrainSampleOcclusion(float2 uv)
{
#ifdef _OCCLUSIONMAP
    half occ = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uv).g;
    return LerpWhiteTo(occ, _OcclusionStrength);
#else
    return 1.0h;
#endif
}

#endif
