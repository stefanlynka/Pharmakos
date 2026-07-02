using UnityEngine;

[CreateAssetMenu(menuName = "Pharmakos/Mesh Highlight Settings", fileName = "MeshHighlightSettings")]
public class MeshHighlightSettings : ScriptableObject
{
    [Header("Colors")]
    public Color glowColor = new Color(0.15f, 0.95f, 0.28f, 1f);
    public Color hotColor = new Color(1f, 1f, 0.82f, 1f);
    public Color outerColor = new Color(0.02f, 0.45f, 0.08f, 1f);
    public Color outerShellGlowColor = new Color(0.1f, 0.78f, 0.18f, 0.9f);
    public Color outerShellOuterColor = new Color(0.01f, 0.28f, 0.05f, 0.75f);

    [Header("Extent")]
    public float coreFlameExtent = 0.012f;
    public float coreFlameExtentMin = 0.001f;
    public float outerFlameExtent = 0.032f;
    public float outerFlameExtentMin = 0.004f;

    [Header("Outline")]
    public MeshHighlightEdgeMode edgeMode = MeshHighlightEdgeMode.Fresnel;
    [Tooltip("Width of the geometry-based outline band in mesh-local units.")]
    public float geomEdgeWidth = 0.08f;
    [Tooltip("Strength multiplier for the geometry-based outline.")]
    public float geomEdgeStrength = 1f;
    [Tooltip("Hybrid mode: 0 = geometry only, 1 = max(geometry, fresnel).")]
    [Range(0f, 1f)]
    public float hybridBlend = 0.65f;
    [Tooltip("Suppress glow on mesh-local upward-facing surfaces (normalOS.y) above this threshold. 0 = disabled.")]
    [Range(0f, 1f)]
    public float topFaceSuppress = 0f;

    [Header("Shape")]
    public float edgePower = 3.8f;
    public float outerEdgePower = 2.6f;
    public float edgeMin = 0.35f;
    public float riseStrength = 1.1f;
    public float coreTongueThreshold = 0.46f;
    public float outerTongueThreshold = 0.58f;
    public float tongueSharpness = 0.11f;
    public float tipFalloff = 1.35f;
    public float outerTipFalloff = 1.05f;

    [Header("Noise")]
    public float noiseScale = 11f;
    public float noiseSpeed = 2.2f;
    public float coreNoiseStrength = 1.15f;
    public float outerNoiseStrength = 1.35f;
    public float flowBias = 1.35f;

    [Header("Intensity")]
    public float intensity = 2.2f;
    public float coreIntensityMultiplier = 1f;
    public float outerIntensityMultiplier = 0.55f;
    public float emissionBoost = 3.2f;
    public float outerEmissionBoost = 2.1f;
    public float flickerSpeed = 4.5f;
    public float flickerStrength = 0.38f;

    [Header("Pulse")]
    public float pulseSpeed = 1.2f;
    public float pulseMin = 0.88f;
    public float pulseMax = 1f;
}
