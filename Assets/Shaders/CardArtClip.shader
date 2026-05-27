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

            // Negative inside the axis-aligned box, positive outside (standard SDF convention).
            float SharpBoxSDF(float2 p, float2 boxMin, float2 boxMax)
            {
                float2 d = max(boxMin - p, p - boxMax);
                return max(d.x, d.y);
            }

            // Rounds a 90-degree convex corner using local edge coordinates.
            // axisU/axisV point along the two visible edges away from the corner (unit vectors).
            float ConvexCornerEarTrim(float2 p, float2 corner, float2 axisU, float2 axisV, half radius)
            {
                if (radius <= 1e-5)
                {
                    return 0.0;
                }

                float u = dot(p - corner, axisU);
                float v = dot(p - corner, axisV);
                if (u < 0.0 || v < 0.0 || u > radius || v > radius)
                {
                    return 0.0;
                }

                float dist = length(float2(u, v) - radius);
                if (dist < radius)
                {
                    return 0.0;
                }

                return dist - radius;
            }

            float ApplyConvexCornerRound(
                float sdf,
                float2 p,
                float2 corner,
                float2 axisU,
                float2 axisV,
                half radius)
            {
                float trim = ConvexCornerEarTrim(p, corner, axisU, axisV, radius);
                if (trim > 0.0)
                {
                    sdf = max(sdf, trim);
                }

                return sdf;
            }

            half CutoutFilletRadius(half outerRadius, float2 cutoutSize)
            {
                if (outerRadius <= 1e-5)
                {
                    return 0.0;
                }

                return min(outerRadius, min(cutoutSize.x, cutoutSize.y) * 0.499);
            }

            float ApplyBottomLeftCutout(float sdf, float2 uv, float4 rect, float2 size, half outerRadius)
            {
                if (size.x <= 1e-5 || size.y <= 1e-5)
                {
                    return sdf;
                }

                float2 cutMin = rect.xy;
                float2 cutMax = rect.xy + size;
                half r = CutoutFilletRadius(outerRadius, size);
                const float2 axisRight = float2(1.0, 0.0);
                const float2 axisLeft = float2(-1.0, 0.0);
                const float2 axisUp = float2(0.0, 1.0);
                const float2 axisDown = float2(0.0, -1.0);

                sdf = max(sdf, -SharpBoxSDF(uv, cutMin, cutMax));
                // Left edge meets notch top, inner notch corner, notch side meets bottom.
                sdf = ApplyConvexCornerRound(sdf, uv, float2(cutMin.x, cutMax.y), axisUp, axisLeft, r);
                sdf = ApplyConvexCornerRound(sdf, uv, cutMax, axisRight, axisUp, r);
                sdf = ApplyConvexCornerRound(sdf, uv, float2(cutMax.x, cutMin.y), axisRight, axisDown, r);
                return sdf;
            }

            float ApplyBottomRightCutout(float sdf, float2 uv, float4 rect, float2 size, half outerRadius)
            {
                if (size.x <= 1e-5 || size.y <= 1e-5)
                {
                    return sdf;
                }

                float2 cutMin = float2(rect.z - size.x, rect.y);
                float2 cutMax = float2(rect.z, rect.y + size.y);
                half r = CutoutFilletRadius(outerRadius, size);
                const float2 axisRight = float2(1.0, 0.0);
                const float2 axisLeft = float2(-1.0, 0.0);
                const float2 axisUp = float2(0.0, 1.0);
                const float2 axisDown = float2(0.0, -1.0);

                sdf = max(sdf, -SharpBoxSDF(uv, cutMin, cutMax));
                // Bottom meets cutout side, inner notch corner, notch top meets card right edge.
                sdf = ApplyConvexCornerRound(sdf, uv, cutMin, axisLeft, axisDown, r);
                sdf = ApplyConvexCornerRound(sdf, uv, float2(cutMin.x, cutMax.y), axisLeft, axisUp, r);
                sdf = ApplyConvexCornerRound(sdf, uv, cutMax, axisUp, axisRight, r);
                return sdf;
            }

            float ApplyTopLeftCutout(float sdf, float2 uv, float4 rect, float2 size, half outerRadius)
            {
                if (size.x <= 1e-5 || size.y <= 1e-5)
                {
                    return sdf;
                }

                float2 cutMin = float2(rect.x, rect.w - size.y);
                float2 cutMax = float2(rect.x + size.x, rect.w);
                half r = CutoutFilletRadius(outerRadius, size);
                const float2 axisRight = float2(1.0, 0.0);
                const float2 axisLeft = float2(-1.0, 0.0);
                const float2 axisUp = float2(0.0, 1.0);
                const float2 axisDown = float2(0.0, -1.0);

                sdf = max(sdf, -SharpBoxSDF(uv, cutMin, cutMax));
                // Left edge meets notch bottom, inner notch corner, notch side meets top edge.
                sdf = ApplyConvexCornerRound(sdf, uv, cutMin, axisLeft, axisDown, r);
                sdf = ApplyConvexCornerRound(sdf, uv, float2(cutMax.x, cutMin.y), axisRight, axisDown, r);
                sdf = ApplyConvexCornerRound(sdf, uv, cutMax, axisRight, axisUp, r);
                return sdf;
            }

            float ApplyTopRightCutout(float sdf, float2 uv, float4 rect, float2 size, half outerRadius)
            {
                if (size.x <= 1e-5 || size.y <= 1e-5)
                {
                    return sdf;
                }

                float2 cutMin = float2(rect.z - size.x, rect.w - size.y);
                float2 cutMax = rect.zw;
                half r = CutoutFilletRadius(outerRadius, size);
                const float2 axisRight = float2(1.0, 0.0);
                const float2 axisLeft = float2(-1.0, 0.0);
                const float2 axisUp = float2(0.0, 1.0);
                const float2 axisDown = float2(0.0, -1.0);

                sdf = max(sdf, -SharpBoxSDF(uv, cutMin, cutMax));
                // Left side meets notch top, left side meets notch bottom, inner corner, card top meets cutout right.
                sdf = ApplyConvexCornerRound(sdf, uv, float2(cutMin.x, cutMax.y), axisDown, axisRight, r);
                sdf = ApplyConvexCornerRound(sdf, uv, cutMin, axisRight, axisUp, r);
                sdf = ApplyConvexCornerRound(sdf, uv, float2(cutMax.x, cutMin.y), axisLeft, axisUp, r);
                sdf = ApplyConvexCornerRound(sdf, uv, cutMax, axisLeft, axisDown, r);
                return sdf;
            }

            half ArtClipMask(float2 uv, float4 rect, half radius, float4 cutouts0, float4 cutouts1)
            {
                float2 center = (rect.xy + rect.zw) * 0.5;
                float2 halfSize = (rect.zw - rect.xy) * 0.5;
                half outerRadius = ClampCornerRadius(radius, halfSize);
                float sdf = RoundedBoxSDF(uv - center, halfSize, outerRadius);

                sdf = ApplyBottomLeftCutout(sdf, uv, rect, cutouts0.xy, outerRadius);
                sdf = ApplyBottomRightCutout(sdf, uv, rect, cutouts0.zw, outerRadius);
                sdf = ApplyTopLeftCutout(sdf, uv, rect, cutouts1.xy, outerRadius);
                sdf = ApplyTopRightCutout(sdf, uv, rect, cutouts1.zw, outerRadius);

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
