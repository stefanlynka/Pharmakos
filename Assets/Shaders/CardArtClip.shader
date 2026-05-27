Shader "Pharmakos/CardArtClip"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _MinVividness ("Min Vividness", Range(0, 1)) = 0.1
        _ArtClipRect ("Art Clip Rect (minX, minY, maxX, maxY)", Vector) = (0, 0, 1, 1)
        _CornerRadius ("Corner Radius (UV space)", Range(0, 0.5)) = 0
        _CornerCutouts0 ("Corner Cutouts BL + BR (width, height each)", Vector) = (0, 0, 0, 0)
        _CornerCutouts1 ("Corner Cutouts TL + TR (width, height each)", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                half fogFactor : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _MinVividness;
                float4 _ArtClipRect;
                half _CornerRadius;
                float4 _CornerCutouts0;
                float4 _CornerCutouts1;
            CBUFFER_END

            float RoundedBoxSDF(float2 p, float2 halfSize, half radius)
            {
                float2 q = abs(p) - halfSize + radius;
                return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - radius;
            }

            half ClampCornerRadius(half radius, float2 halfSize)
            {
                return min(radius, min(halfSize.x, halfSize.y));
            }

            // Polynomial smooth max (Inigo Quilez). Blends where abs(a-b) < k; ~max elsewhere.
            float SmoothedMax(float a, float b, float k)
            {
                k = max(k, 1e-7);
                float h = saturate(0.5 + 0.5 * (a - b) / k);
                return lerp(b, a, h) + k * h * (1.0 - h);
            }

            float CutoutRoundedSdf(float2 uv, float2 cutoutCenter, float2 cutoutSize, half radiusForClamp)
            {
                float2 halfCut = cutoutSize * 0.5;
                half rr = ClampCornerRadius(radiusForClamp, halfCut);
                return RoundedBoxSDF(uv - cutoutCenter, halfCut, rr);
            }

            half ArtClipMask(float2 uv, float4 rect, half radius, float4 cutouts0, float4 cutouts1)
            {
                float2 center = (rect.xy + rect.zw) * 0.5;
                float2 halfSize = (rect.zw - rect.xy) * 0.5;
                half outerRadius = ClampCornerRadius(radius, halfSize);
                float sdfOuter = RoundedBoxSDF(uv - center, halfSize, outerRadius);

                // Union of corner cutouts (SDF min). Inactive corners use a huge distance so they do not cut.
                const float kFarCutout = 1e5;
                float dCuts = kFarCutout;

                if (cutouts0.x > 1e-5 && cutouts0.y > 1e-5)
                {
                    float2 c = float2(rect.x + cutouts0.x * 0.5, rect.y + cutouts0.y * 0.5);
                    dCuts = min(dCuts, CutoutRoundedSdf(uv, c, cutouts0.xy, outerRadius));
                }

                if (cutouts0.z > 1e-5 && cutouts0.w > 1e-5)
                {
                    float2 c = float2(rect.z - cutouts0.z * 0.5, rect.y + cutouts0.w * 0.5);
                    dCuts = min(dCuts, CutoutRoundedSdf(uv, c, cutouts0.zw, outerRadius));
                }

                if (cutouts1.x > 1e-5 && cutouts1.y > 1e-5)
                {
                    float2 c = float2(rect.x + cutouts1.x * 0.5, rect.w - cutouts1.y * 0.5);
                    dCuts = min(dCuts, CutoutRoundedSdf(uv, c, cutouts1.xy, outerRadius));
                }

                if (cutouts1.z > 1e-5 && cutouts1.w > 1e-5)
                {
                    float2 c = float2(rect.z - cutouts1.z * 0.5, rect.w - cutouts1.w * 0.5);
                    dCuts = min(dCuts, CutoutRoundedSdf(uv, c, cutouts1.zw, outerRadius));
                }

                // Smooth subtraction: clipped rect minus union of cutouts — no separate fillet discs.
                float kBlend = max(outerRadius * 2.0, 1e-6);
                float sdf = SmoothedMax(sdfOuter, -dCuts, kBlend);

                return smoothstep(0.0, fwidth(sdf) * 1.5, -sdf);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _Color;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                half clipMask = ArtClipMask(input.uv, _ArtClipRect, _CornerRadius, _CornerCutouts0, _CornerCutouts1);
                clip(texColor.a * clipMask - 0.001);

                Light mainLight = GetMainLight();
                half lightStrength = saturate(dot(mainLight.color, half3(0.299h, 0.587h, 0.114h)));
                half3 litColor = texColor.rgb * mainLight.color;
                half3 minLitColor = texColor.rgb * _MinVividness;
                half3 finalColor = lerp(minLitColor, litColor, lightStrength);

                half4 color = half4(finalColor, texColor.a * clipMask);
                color.rgb = MixFog(color.rgb, input.fogFactor);
                return color;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
