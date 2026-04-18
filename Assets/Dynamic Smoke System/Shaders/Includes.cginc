#ifndef INCLUDES
#define INCLUDES
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" 
//#include "UnityPBSLighting.cginc"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

#include "./Structs.cginc"
#include "./Utils.cginc"
#include "./Camera.cginc"
#include "./Math.cginc"
#include "./Raymarching.cginc"
#endif