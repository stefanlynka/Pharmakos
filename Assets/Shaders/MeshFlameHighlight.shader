Shader "Pharmakos/MeshFlameHighlight"
{
    Properties
    {
        _GlowColor ("Glow Color", Color) = (0.15, 0.95, 0.28, 1)
        _HotColor ("Hot Color", Color) = (1, 1, 0.82, 1)
        _OuterColor ("Outer Color", Color) = (0.02, 0.45, 0.08, 1)
        _FlameExtent ("Flame Extent", Range(0, 0.2)) = 0.018
        _FlameExtentMin ("Flame Extent Min", Range(0, 0.1)) = 0.001
        _NoiseScale ("Noise Scale", Range(1, 32)) = 11
        _NoiseSpeed ("Noise Speed", Range(0, 10)) = 2.2
        _NoiseStrength ("Noise Strength", Range(0, 2)) = 1.15
        _EdgePower ("Edge Power", Range(0.5, 12)) = 3.8
        _EdgeMin ("Edge Min", Range(0, 1)) = 0.35
        _EdgeMode ("Edge Mode", Float) = 0
        _ObjectCenterOS ("Object Center OS", Vector) = (0, 0, 0, 0)
        _ObjectHalfExtents ("Object Half Extents", Vector) = (0.5, 0.5, 0.5, 0)
        _GeomEdgeWidth ("Geom Edge Width", Range(0, 1)) = 0.08
        _GeomEdgeStrength ("Geom Edge Strength", Range(0, 2)) = 1
        _HybridBlend ("Hybrid Blend", Range(0, 1)) = 0.65
        _TopFaceSuppress ("Top Face Suppress", Range(0, 1)) = 0
        _RiseStrength ("Rise Strength", Range(0, 3)) = 1.1
        _TongueThreshold ("Tongue Threshold", Range(0, 1)) = 0.46
        _TongueSharpness ("Tongue Sharpness", Range(0.01, 0.5)) = 0.11
        _TipFalloff ("Tip Falloff", Range(0.2, 4)) = 1.35
        _FlickerSpeed ("Flicker Speed", Range(0, 16)) = 4.5
        _FlickerStrength ("Flicker Strength", Range(0, 1)) = 0.38
        _Intensity ("Intensity", Range(0, 6)) = 2.2
        _EmissionBoost ("Emission Boost", Range(0, 8)) = 3.2
        _FlowBias ("Flow Bias", Range(0, 3)) = 1.35
        _ShellLayer ("Shell Layer", Float) = 0
        _Seed ("Seed", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent+100"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Cull Back
        Lighting Off
        ZWrite Off
        Blend One One

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float flameFactor : TEXCOORD2;
                float shapeNoise : TEXCOORD3;
                float3 positionOS : TEXCOORD4;
                float3 normalOS : TEXCOORD5;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _GlowColor;
                half4 _HotColor;
                half4 _OuterColor;
                half _FlameExtent;
                half _FlameExtentMin;
                half _NoiseScale;
                half _NoiseSpeed;
                half _NoiseStrength;
                half _EdgePower;
                half _EdgeMin;
                half _EdgeMode;
                float4 _ObjectCenterOS;
                float4 _ObjectHalfExtents;
                half _GeomEdgeWidth;
                half _GeomEdgeStrength;
                half _HybridBlend;
                half _TopFaceSuppress;
                half _RiseStrength;
                half _TongueThreshold;
                half _TongueSharpness;
                half _TipFalloff;
                half _FlickerSpeed;
                half _FlickerStrength;
                half _Intensity;
                half _EmissionBoost;
                half _FlowBias;
                half _ShellLayer;
                float _Seed;
            CBUFFER_END

            float Hash31(float3 p)
            {
                p = frac(p * float3(443.897, 441.423, 437.195));
                p += dot(p, p.yzx + 19.19);
                return frac((p.x + p.y) * p.z);
            }

            float ValueNoise3(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                float3 u = f * f * (3.0 - 2.0 * f);

                float n000 = Hash31(i + float3(0, 0, 0));
                float n100 = Hash31(i + float3(1, 0, 0));
                float n010 = Hash31(i + float3(0, 1, 0));
                float n110 = Hash31(i + float3(1, 1, 0));
                float n001 = Hash31(i + float3(0, 0, 1));
                float n101 = Hash31(i + float3(1, 0, 1));
                float n011 = Hash31(i + float3(0, 1, 1));
                float n111 = Hash31(i + float3(1, 1, 1));

                float n00 = lerp(n000, n100, u.x);
                float n10 = lerp(n010, n110, u.x);
                float n01 = lerp(n001, n101, u.x);
                float n11 = lerp(n011, n111, u.x);
                float n0 = lerp(n00, n10, u.y);
                float n1 = lerp(n01, n11, u.y);
                return lerp(n0, n1, u.z);
            }

            float FractalNoise3(float3 p)
            {
                float amplitude = 0.55;
                float frequency = 1.0;
                float total = 0.0;
                float normalization = 0.0;

                [unroll]
                for (int i = 0; i < 4; i++)
                {
                    total += ValueNoise3(p * frequency) * amplitude;
                    normalization += amplitude;
                    frequency *= 2.17;
                    amplitude *= 0.52;
                    p += float3(17.3, 31.7, 11.9);
                }

                return total / max(normalization, 0.0001);
            }

            float3 GetFlameFlow(float time)
            {
                return float3(0.12, 1.0, 0.08) * (time * _FlowBias);
            }

            float SampleFlameNoise(float3 positionWS, float time, float scaleMul)
            {
                float3 flow = GetFlameFlow(time);
                float3 samplePos = float3(
                    positionWS.x * _NoiseScale * scaleMul,
                    positionWS.y * _NoiseScale * 0.65 * scaleMul + time * _NoiseSpeed * 0.55,
                    positionWS.z * _NoiseScale * scaleMul) + flow + float3(_Seed, _Seed * 1.73, _Seed * 2.41);
                return FractalNoise3(samplePos);
            }

            float ComputeGeometryEdgeGate(float3 positionOS)
            {
                float3 localPos = positionOS - _ObjectCenterOS.xyz;
                float3 distToFace = _ObjectHalfExtents.xyz - abs(localPos);
                // Use the second-smallest face distance so entire faces don't glow — only true box edges/corners do.
                float nearestFace = min(distToFace.x, min(distToFace.y, distToFace.z));
                float farthestFace = max(distToFace.x, max(distToFace.y, distToFace.z));
                float distToEdge = distToFace.x + distToFace.y + distToFace.z - nearestFace - farthestFace;
                return (1.0 - smoothstep(0.0, _GeomEdgeWidth, distToEdge)) * _GeomEdgeStrength;
            }

            float ComputeEdgeGate(float3 positionOS, float3 normalWS, float3 viewDirWS)
            {
                float rim = pow(saturate(1.0 - saturate(dot(normalWS, viewDirWS))), _EdgePower);
                float fresnelGate = smoothstep(_EdgeMin, 1.0, rim);
                float geomGate = ComputeGeometryEdgeGate(positionOS);

                if (_EdgeMode < 0.5)
                    return fresnelGate;

                if (_EdgeMode < 1.5)
                    return geomGate;

                return lerp(geomGate, max(fresnelGate, geomGate), _HybridBlend);
            }

            float ComputeTopFaceMask(float3 normalOS)
            {
                if (_TopFaceSuppress <= 0.001)
                    return 1.0;

                return 1.0 - smoothstep(_TopFaceSuppress - 0.08, _TopFaceSuppress, normalOS.y);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionOS = input.positionOS.xyz;
                float3 normalOS = input.normalOS;
                float3 positionWS = TransformObjectToWorld(positionOS);
                float3 normalWS = normalize(TransformObjectToWorldNormal(normalOS));

                float time = _Time.y * _NoiseSpeed;
                float shapeNoise = SampleFlameNoise(positionWS, time, 1.0);
                float detailNoise = SampleFlameNoise(positionWS + normalWS * 0.22, time * 1.45, 1.85);
                float combinedNoise = saturate(shapeNoise * 0.62 + detailNoise * 0.38);

                float extentRange = max(_FlameExtent - _FlameExtentMin, 0.0001);
                float noiseMix = lerp(0.25, 1.0, _NoiseStrength);
                float extrusion = _FlameExtentMin + extentRange * saturate(lerp(0.35, combinedNoise, noiseMix));

                float outerLayerBoost = lerp(1.0, 1.45, saturate(_ShellLayer));
                extrusion *= lerp(0.55 + combinedNoise * 0.85, 0.35 + combinedNoise * 0.95, saturate(_ShellLayer)) * outerLayerBoost;

                float horizontalMask = 1.0 - saturate(abs(normalOS.y));
                float3 riseDir = normalize(float3(normalWS.x * 0.18, 1.0, normalWS.z * 0.18));
                float rise = extrusion * _RiseStrength * combinedNoise * horizontalMask;
                float3 displacedWS = positionWS + normalWS * extrusion + riseDir * rise;

                output.positionCS = TransformWorldToHClip(displacedWS);
                output.positionWS = displacedWS;
                output.normalWS = normalWS;
                output.positionOS = positionOS;
                output.normalOS = normalOS;
                output.flameFactor = saturate(extrusion / max(_FlameExtent * outerLayerBoost, 0.0001));
                output.shapeNoise = combinedNoise;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(GetWorldSpaceViewDir(input.positionWS));
                float edgeGate = ComputeEdgeGate(input.positionOS, normalWS, viewDirWS);
                edgeGate *= ComputeTopFaceMask(input.normalOS);

                float time = _Time.y;
                float nPrimary = SampleFlameNoise(input.positionWS, time * 1.1, 1.0);
                float nSecondary = SampleFlameNoise(input.positionWS + float3(0, time * 0.35, 0), time * 1.65, 2.35);
                float nWarp = SampleFlameNoise(input.positionWS * 1.15 + normalWS * 0.4, time * 0.95, 1.55);
                float flameNoise = saturate(nPrimary * 0.55 + nSecondary * 0.3 + nWarp * 0.15);

                float threshold = _TongueThreshold + _ShellLayer * 0.12;
                float sharpness = max(_TongueSharpness * lerp(1.0, 1.35, _ShellLayer), 0.01);
                float tongueA = smoothstep(threshold, threshold + sharpness, flameNoise);
                float tongueB = smoothstep(threshold + 0.08, threshold + sharpness + 0.06, nSecondary);
                float tongue = tongueA * lerp(1.0, tongueB, 0.65);

                if (_ShellLayer > 0.5)
                    tongue *= smoothstep(0.48, 0.92, nPrimary);

                float tipFade = pow(saturate(1.0 - input.flameFactor), _TipFalloff);
                float baseBurn = pow(saturate(1.0 - input.flameFactor * 0.72), 2.2);
                float flame = edgeGate * tongue * tipFade * baseBurn;
                flame *= lerp(0.85, 0.55, _ShellLayer);

                float flickerNoise = ValueNoise3(float3(time * _FlickerSpeed + _Seed * 2.1, _Seed * 7.3, time * 0.35));
                float flicker = 1.0 + ((flickerNoise - 0.5) * 2.0) * _FlickerStrength;

                half3 color = lerp(_HotColor.rgb, _GlowColor.rgb, saturate(input.flameFactor * 1.8 + (1.0 - tongue) * 0.25));
                color = lerp(color, _OuterColor.rgb, saturate(input.flameFactor * input.flameFactor * 1.35));
                color *= (0.25 + _EmissionBoost * (0.35 + flameNoise * 0.45)) * _Intensity * flicker;

                return half4(color * flame * _GlowColor.a, flame);
            }
            ENDHLSL
        }
    }
}
