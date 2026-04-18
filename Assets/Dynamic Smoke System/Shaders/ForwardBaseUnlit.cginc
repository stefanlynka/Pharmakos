#ifndef VERT_FRAG_FORWARD_OBJECT_SIMPLE_H
#define VERT_FRAG_FORWARD_OBJECT_SIMPLE_H

#include "Includes.cginc"

struct appdata
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
};

struct Vert2Frag
{
    float4 pos : POSITION;
    float4 projPos : TEXCOORD0;
    float3 worldPos : TEXCOORD2;
    float4 shadowCoord : TEXCOORD3;
    float fogCoord : TEXCOORD4;
    UNITY_VERTEX_OUTPUT_STEREO
};

struct FragOutput
{
    float4 color : SV_Target;
    float depth : SV_Depth;
};

Vert2Frag Vert(appdata v)
{
    Vert2Frag o;
    ZERO_INITIALIZE(Vert2Frag, o);

    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);


    o.pos = TransformObjectToHClip(v.vertex);
    o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

    o.projPos = ComputeScreenPos(o.pos);
    //COMPUTE_EYEDEPTH(o.projPos.z);

    //UNITY_TRANSFER_SHADOW(o, v.texcoord1.xy);
    o.fogCoord = ComputeFogFactor(o.pos.z);
    return o;
}

FragOutput Frag(Vert2Frag i)
{
    //UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    //UNITY_SETUP_INSTANCE_ID(i);

    RaymarchInfo ray;
    INITIALIZE_RAYMARCH_INFO(ray, i);
    Raymarch(ray);

    FragOutput o;
    ZERO_INITIALIZE(FragOutput, o);
    o.color = ray.color;
    o.depth = ray.depth;
    
//#if (defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2))
    //i.fogCoord = mul(UNITY_MATRIX_VP, float4(ray.endPos, 1.0)).z;
//#endif
    
    o.color.rgb = MixFog(o.color, i.fogCoord);

    return o;
}

#endif
