#include "includes.cginc"

float lightAbsorptionThroughCloud;
float lightAbsorptionTowardSun;
float darknessThreshold;
float forwardScattering;
float backScattering;
float baseBrightness;
float phaseFactor;
float _densityStepCount;
uint _lightMarchSteps;
float3 _Color;
float3 _ShadowColor;
float3 _noiseWeights;
float _noiseMin;
float  _noiseMax;

static const float DITHER_THRESHOLD_8x8[64] =
{
    0.2627,0.7725,0.3843,0.9529,0.2941,0.8196,0.4118,1.0000,
    0.5020,0.0392,0.6235,0.1412,0.5333,0.0510,0.6549,0.1725,
    0.3216,0.8627,0.2039,0.6824,0.3529,0.9098,0.2314,0.7294,
    0.5647,0.0824,0.4431,0.0078,0.5922,0.1137,0.4745,0.0235,
    0.2784,0.7961,0.4000,0.9765,0.2471,0.7529,0.3686,0.9333,
    0.5176,0.0471,0.6392,0.1569,0.4902,0.0314,0.6078,0.1294,
    0.3373,0.8863,0.2196,0.7059,0.3098,0.8431,0.1882,0.6706,
    0.5804,0.0980,0.4588,0.0157,0.5490,0.0667,0.4275,0.0001
};

float Dither8x8(float2 positionCS)
{
    uint index = (uint(positionCS.x) % 8) * 8 + uint(positionCS.y) % 8;
    return DITHER_THRESHOLD_8x8[index];
}

inline bool _ShouldRaymarchFinish(RaymarchInfo ray)
{
    if (ray.totalLength > ray.maxDistance) return true;
    if (!IsInnerObject(ray.endPos)) return true;

    return false;
}

inline void InitRaymarchFullScreen(out RaymarchInfo ray, float4 projPos)
{
    ZERO_INITIALIZE(RaymarchInfo, ray);
    ray.rayDir = GetCameraDirection(projPos);
    ray.projPos = projPos;
#if defined(USING_STEREO_MATRICES)
    float3 cameraPos = unity_StereoWorldSpaceCameraPos[unity_StereoEyeIndex];
    cameraPos += float3(1., 0, 0) * unity_StereoEyeIndex;
#else
    float3 cameraPos = _WorldSpaceCameraPos;
#endif
    ray.startPos = cameraPos + GetCameraNearClip() * ray.rayDir;
    ray.maxDistance = GetCameraFarClip();
}

inline void InitRaymarchObject(out RaymarchInfo ray, float4 projPos, float3 worldPos)
{
    ZERO_INITIALIZE(RaymarchInfo, ray);
    ray.rayDir = normalize(worldPos - GetCameraPosition());
    ray.projPos = projPos;
    ray.startPos = worldPos;
    ray.maxDistance = GetCameraFarClip();

    float3 cameraNearPlanePos = GetCameraPosition() + GetDistanceFromCameraToNearClipPlane(projPos) * ray.rayDir;
    if (IsInnerObject(cameraNearPlanePos)) {
        ray.startPos = cameraNearPlanePos;
        ray.polyNormal = -ray.rayDir;
    }
}

inline void UseCameraDepthTextureForMaxDistance(inout RaymarchInfo ray, float4 projPos)
{
    float depth = LinearEyeDepth(_CameraDepthTexture.Sample(sampler_CameraDepthTexture, projPos.xy / projPos.w).r, _ZBufferParams);
    float dist = depth / dot(ray.rayDir, GetCameraForward());
    ray.maxDistance = dist;
}

#define INITIALIZE_RAYMARCH_INFO(ray, i) \
    InitRaymarchObject(ray, i.projPos, i.worldPos); \
    UseCameraDepthTextureForMaxDistance(ray, i.projPos);

float hg(float a, float g) {
    float g2 = g*g;
    return (1-g2) / (4*3.1415*pow(1+g2-2*g*(a), 1.5));
}

float phase(float a) {
    float blend = .5;
    float hgBlend = hg(a,forwardScattering) * (1-blend) + hg(a,-backScattering) * blend;
    return baseBrightness + hgBlend*phaseFactor;
}

float beer(float d) {
    float beer = exp(-d);
    return beer;
}

/**
 * \brief 
 * \param pos worldSpace position
 * \return (float density, float SDF distance)
 */
inline float2 sampleDensity(float3 pos, float3 centerPos, float maskDistance)
{
    float time = _Time.x * _TimeScale * 0.4;
    float noiseDetail = _NoiseDetailTex.SampleLevel(sampler_NoiseDetailTex, (pos - time * _EdgeSpeed) * 0.2 *_EdgeScale, 0);

    float distance = DistanceFunction(pos);
    float3 uvw = pos;
    float sdfAsFraction = 0;
    
    if (distance > 0)
    {
        sdfAsFraction = 0;
    }
    else
    {
        sdfAsFraction = -(distance / _Radius);
    }
    
    float noiseEdge = noiseDetail * _EdgeIntensity * saturate(sdfAsFraction + 0.5);
    
    uvw += float3(time, time*0.1, time*0.2);
    float3 noise = 1;
    noise = _NoiseTex.SampleLevel(sampler_NoiseTex, uvw * _Scale, 0);
    noise *= _noiseWeights;
    float noiseResult = (noise.r - noise.g - noise.b);
    noiseResult = remap(0, 1, _noiseMin, _noiseMax * _Mask, noiseResult);

    float maskTime = _Time.x * 0.3;
    //float3 noise2 = _NoiseDetailTex.SampleLevel(sampler_NoiseDetailTex, pos * 0.1 - maskTime, 0);
    float3 noise2 = (float3) noiseDetail;
    float distanceFromCenterNoised = length(pos + noise2.z - centerPos);
    float maskFalloff = clamp(0, 1, 1 - invLerp(0.8, 1, clamp(0, 1, distanceFromCenterNoised / maskDistance)));
    
    return float2(sdfAsFraction * maskFalloff * noiseResult - noiseEdge, distance);
}

// Returns (dstToBox, dstInsideBox). If ray misses box, dstInsideBox will be zero
float2 rayBoxDst(float3 boundsMin, float3 boundsMax, float3 rayOrigin, float3 invRaydir) {
    // Adapted from: http://jcgt.org/published/0007/03/04/
    float3 t0 = (boundsMin - rayOrigin) * invRaydir;
    float3 t1 = (boundsMax - rayOrigin) * invRaydir;
    float3 tmin = min(t0, t1);
    float3 tmax = max(t0, t1);

    float dstA = max(max(tmin.x, tmin.y), tmin.z);
    float dstB = min(tmax.x, min(tmax.y, tmax.z));

    // CASE 1: ray intersects box from outside (0 <= dstA <= dstB)
    // dstA is dst to nearest intersection, dstB dst to far intersection

    // CASE 2: ray intersects box from inside (dstA < 0 < dstB)
    // dstA is the dst to intersection behind the ray, dstB is dst to forward intersection

    // CASE 3: ray misses box (dstA > dstB)

    float dstToBox = max(0, dstA);
    float dstInsideBox = max(0, dstB - dstToBox);
    return float2(dstToBox, dstInsideBox);
}

float lightmarch(float3 pos, float distanceToTravel, float3 centerPos, float maskDistance) {
    float3 dirToLight = _MainLightPosition.xyz;
    float stepSize = distanceToTravel / _lightMarchSteps;
    float transmittance = 1;
    float3 lightMarchPos = pos;
    lightMarchPos += dirToLight * stepSize;
    float totalDensity = 0;

    for (uint step = 0; step < _lightMarchSteps; step++) {
        float density = sampleDensity(lightMarchPos, centerPos, maskDistance).x;
        totalDensity += max(0, density);
        lightMarchPos += dirToLight * stepSize;
    }
    
    transmittance = beer(totalDensity * lightAbsorptionTowardSun);
    float clampedTransmittance = darknessThreshold + transmittance * (1-darknessThreshold);
    return clampedTransmittance;
}

inline bool _Raymarch(inout RaymarchInfo ray)
{
    if (_Mask <= 0) return false;
    
    ray.endPos = ray.startPos;
    ray.lastDistance = 0.0;
    
    
    float depth = LinearEyeDepth(_CameraDepthTexture.Sample(sampler_CameraDepthTexture, ray.projPos.xy / ray.projPos.w).r, _ZBufferParams);
    depth = depth / dot(ray.rayDir, GetCameraForward());
    
    float4 clipPos = mul(UNITY_MATRIX_VP, float4(ray.endPos, 1));
    float4 screenPos = ComputeScreenPos(clipPos);
    screenPos.xyz /= screenPos.w;
    float blueNoise = tex2D(_BlueNoise, screenPos.xy * 20);

    float3 centerPos = _MaskPos;
    float3 objectSize = (_boundsMax - _boundsMin);
    float maxObjectSize = abs(max(objectSize.z, max(objectSize.x, objectSize.y)));
    float maskDistance = maxObjectSize * _Mask;
    
    float3 startPos = ray.startPos + ray.rayDir * blueNoise;

    ray.totalLength = length(startPos - GetCameraPosition());

    float depthBias = _DepthBias;
    
    // determine max raymarching distance. It's either the end of the box, or
    // an object that blocks view before it (for which the depth is used to determine this)
    float2 rayBoxIntersection = rayBoxDst(_boundsMin, _boundsMax, startPos, 1/ray.rayDir);
    float distanceToTravel = min(depth - ray.totalLength - depthBias, rayBoxIntersection.y);
    
    if (depth < ray.totalLength + depthBias)
    {
        return false;
    }

    float densityStepSize = maxObjectSize / _densityStepCount;
    
    // since we know the max depth, offset the start position so the raymarching will
    // exactly hit the surface.
    // This prevents banding
    //float densityMarchLoopRest = densityMarchLoops % 1;
    //startPos -= ray.rayDir * (1 - densityMarchLoopRest) * _densityStepSize;

    float transmittance = 1;
    float lightEnergy = 0;
    float3 dirToLight = _MainLightPosition.xyz;
    bool isBeginFound = false;
    bool isEndFound = false;
    float3 beginDepth;
    
    // Phase makes clouds brighter around sun
    float cosAngle = dot(ray.rayDir, _MainLightPosition.xyz);
    float phaseVal = phase(cosAngle);
    
    for (int di = 0; di < _densityStepCount; di++)
    {
        float currentDistance = densityStepSize * di;
        float3 densityMarchPos = startPos + ray.rayDir * currentDistance;
        float lightMarchDistanceToTravel = rayBoxDst(_boundsMin, _boundsMax, densityMarchPos, 1/dirToLight).y;

        if (currentDistance >= distanceToTravel)
        {
            break;
        }
        
        float2 densitySample = sampleDensity(densityMarchPos, centerPos, maskDistance);
        float density = densitySample.x;
        float sdfDistance = densitySample.y;
        
        if (density <= 0 || sdfDistance > 0)
        {
            continue;
        }

        if (!isBeginFound)
        {
            isBeginFound = true;
            beginDepth = currentDistance;
        }
        
        if (!isEndFound)
        {
            isEndFound = true;
            ray.endPos = densityMarchPos;
        }
        
         float currentTransmittence = lightmarch(densityMarchPos, lightMarchDistanceToTravel, centerPos, maskDistance);
         lightEnergy += density * transmittance * currentTransmittence * phaseVal;
         transmittance *= exp(-density * lightAbsorptionThroughCloud);
        
        // Early exit
        if (transmittance < 0.01) {
            break;
        }
    }

    float shadowLightLerp = lightEnergy;

#ifdef CELLSHADING_ON
    shadowLightLerp = saturate(shadowLightLerp);
    shadowLightLerp = tex2D(_CellShadingRamp, float2(shadowLightLerp, 0));
#endif

    float a = 1 - transmittance;
    a *= saturate(invLerp(0, _DepthFade, depth - ray.totalLength - beginDepth));
    
#ifdef ALPHA_DITHER_ON
    if (a <= 0.9)
    {
        float2 ditherUV = screenPos.xy * _ScreenParams.xy;
        half cutoffValue = Dither8x8(ditherUV.xy);
        clip(a - cutoffValue);

        // if not clipped, always return alpha = 1
        a = 1;
    }
#endif
    
    float4 col = float4(lerp(_ShadowColor, _Color, shadowLightLerp), a);
    col = saturate(col);
    
    ray.color = col;
    
    return true;
}
    
void Raymarch(inout RaymarchInfo ray)
{
    if (!_Raymarch(ray)) discard;


    if (IsInnerObject(GetCameraPosition()) && ray.totalLength < GetCameraNearClip()) {
        ray.normal = EncodeNormal(-ray.rayDir);
        ray.depth = EncodeDepth(ray.startPos);
        return;
    }

    float initLength = length(ray.startPos - GetCameraPosition());
    ray.depth = EncodeDepth(ray.endPos);
}
