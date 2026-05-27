using System;
using UnityEngine;

[Serializable]
public struct CardArtCornerCutout
{
    [Tooltip("How far the cutout extends inward along the horizontal edge from this corner (UV space).")]
    [Range(0f, 0.5f)]
    public float Width;

    [Tooltip("How far the cutout extends inward along the vertical edge from this corner (UV space).")]
    [Range(0f, 0.5f)]
    public float Height;

    public bool IsActive => Width > 0f && Height > 0f;

    public static CardArtCornerCutout None => default;
}

[Serializable]
public struct CardArtClipRect
{
    [Tooltip("Left edge of visible art, 0 = left of sprite, 1 = right.")]
    [Range(0f, 1f)]
    public float MinX;

    [Tooltip("Bottom edge of visible art, 0 = bottom of sprite, 1 = top.")]
    [Range(0f, 1f)]
    public float MinY;

    [Tooltip("Right edge of visible art, 0 = left of sprite, 1 = right.")]
    [Range(0f, 1f)]
    public float MaxX;

    [Tooltip("Top edge of visible art, 0 = bottom of sprite, 1 = top.")]
    [Range(0f, 1f)]
    public float MaxY;

    [Tooltip("Corner radius in normalized sprite UV space. 0.04 is a subtle round on a typical card.")]
    [Range(0f, 0.5f)]
    public float CornerRadius;

    public CardArtCornerCutout BottomLeftCutout;
    public CardArtCornerCutout BottomRightCutout;
    public CardArtCornerCutout TopLeftCutout;
    public CardArtCornerCutout TopRightCutout;

    public Vector4 ToShaderVector()
    {
        float minX = Mathf.Min(MinX, MaxX);
        float maxX = Mathf.Max(MinX, MaxX);
        float minY = Mathf.Min(MinY, MaxY);
        float maxY = Mathf.Max(MinY, MaxY);
        return new Vector4(minX, minY, maxX, maxY);
    }

    public Vector4 ToCornerCutouts0Vector()
    {
        return new Vector4(
            BottomLeftCutout.Width,
            BottomLeftCutout.Height,
            BottomRightCutout.Width,
            BottomRightCutout.Height);
    }

    public Vector4 ToCornerCutouts1Vector()
    {
        return new Vector4(
            TopLeftCutout.Width,
            TopLeftCutout.Height,
            TopRightCutout.Width,
            TopRightCutout.Height);
    }

    public static CardArtClipRect Full => new CardArtClipRect
    {
        MinX = 0f,
        MinY = 0f,
        MaxX = 1f,
        MaxY = 1f,
        CornerRadius = 0f,
    };

    public static CardArtClipRect TopHalf(float bottom = 0.5f, float cornerRadius = 0f) => new CardArtClipRect
    {
        MinX = 0f,
        MinY = bottom,
        MaxX = 1f,
        MaxY = 1f,
        CornerRadius = cornerRadius,
    };

    public static CardArtClipRect BottomHalf(float top = 0.5f, float cornerRadius = 0f) => new CardArtClipRect
    {
        MinX = 0f,
        MinY = 0f,
        MaxX = 1f,
        MaxY = top,
        CornerRadius = cornerRadius,
    };

    public static CardArtClipRect TrimTop(float top = 0.88f, float cornerRadius = 0f) => new CardArtClipRect
    {
        MinX = 0f,
        MinY = 0f,
        MaxX = 1f,
        MaxY = top,
        CornerRadius = cornerRadius,
    };

    public static CardArtClipRect Rounded(float cornerRadius = 0.04f) => new CardArtClipRect
    {
        MinX = 0f,
        MinY = 0f,
        MaxX = 1f,
        MaxY = 1f,
        CornerRadius = cornerRadius,
    };

    public static CardArtClipRect RoundedWithBottomLeftCutout(
        float cutoutWidth,
        float cutoutHeight,
        float cornerRadius = 0.04f) => new CardArtClipRect
    {
        MinX = 0f,
        MinY = 0f,
        MaxX = 1f,
        MaxY = 1f,
        CornerRadius = cornerRadius,
        BottomLeftCutout = new CardArtCornerCutout
        {
            Width = cutoutWidth,
            Height = cutoutHeight,
        },
    };
}

public static class CardArtClipUtility
{
    public static readonly int ArtClipRectId = Shader.PropertyToID("_ArtClipRect");
    public static readonly int CornerRadiusId = Shader.PropertyToID("_CornerRadius");
    public static readonly int CornerCutouts0Id = Shader.PropertyToID("_CornerCutouts0");
    public static readonly int CornerCutouts1Id = Shader.PropertyToID("_CornerCutouts1");

    public static void Apply(SpriteRenderer renderer, CardArtClipRect layout, MaterialPropertyBlock block)
    {
        if (renderer == null)
        {
            return;
        }

        renderer.GetPropertyBlock(block);
        block.SetVector(ArtClipRectId, layout.ToShaderVector());
        block.SetFloat(CornerRadiusId, layout.CornerRadius);
        block.SetVector(CornerCutouts0Id, layout.ToCornerCutouts0Vector());
        block.SetVector(CornerCutouts1Id, layout.ToCornerCutouts1Vector());
        renderer.SetPropertyBlock(block);
    }
}
