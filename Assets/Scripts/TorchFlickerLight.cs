using UnityEngine;

public class TorchFlickerLight : MonoBehaviour
{
    [Tooltip("Light to drive. If empty, uses Light on this GameObject.")]
    [SerializeField] private Light targetLight;

    [Header("Intensity")]
    [Tooltip("Use Custom Base Intensity instead of the light's intensity when this component enables.")]
    [SerializeField] private bool useCustomBaseIntensity;

    [Tooltip("Baseline intensity in URP units when Use Custom Base Intensity is enabled.")]
    [SerializeField] private float customBaseIntensity = 8f;

    [Tooltip("How far the multiplier swings below and above 1 (e.g. 0.25 → roughly 0.75–1.25).")]
    [SerializeField] [Range(0f, 1f)] private float flickerAmount = 0.22f;

    [Tooltip("Higher values follow the target flicker faster (URP: tune with your intensity scale).")]
    [SerializeField] private float smoothing = 14f;

    [Header("Noise")]
    [Tooltip("Offsets the Perlin samples so multiple torches do not sync.")]
    [SerializeField] private float noiseSeed = 1.73f;

    [Tooltip("Slow drift of the flame.")]
    [SerializeField] private float slowNoiseFrequency = 0.35f;

    [Tooltip("Mid-frequency flutter.")]
    [SerializeField] private float midNoiseFrequency = 2.1f;

    [Tooltip("Low-frequency channel that occasionally deepens dips (gusts / uneven fuel).")]
    [SerializeField] private float dipNoiseFrequency = 0.55f;

    [Tooltip("Extra dimming when the dip channel is low (0 = none, 1 = strong).")]
    [SerializeField] [Range(0f, 1f)] private float dipStrength = 0.45f;

    [Header("Optional: range")]
    [SerializeField] private bool enableRangeWobble;

    [Tooltip("Fraction of base range to vary (keep small for URP).")]
    [SerializeField] [Range(0f, 0.2f)] private float rangeWobbleAmount = 0.04f;

    [Header("Optional: color")]
    [SerializeField] private bool enableColorShift;

    [Tooltip("Warmer tint when the flame is relatively dim.")]
    [SerializeField] private Color warmTintColor = new Color(1f, 0.55f, 0.2f, 1f);

    [Tooltip("How much the warm tint blends in at the dimmest part of the flicker.")]
    [SerializeField] [Range(0f, 1f)] private float colorShiftStrength = 0.35f;

    [Header("Optional: shadow movement")]
    [SerializeField] private bool enablePositionJitter;

    [Tooltip("Max local-space offset in meters (very small values read best).")]
    [SerializeField] private float positionJitterRadius = 0.02f;

    [Tooltip("Spatial frequency of the jitter noise.")]
    [SerializeField] private float positionJitterFrequency = 3.5f;

    private float _baseIntensity;
    private float _baseRange;
    private Color _baseColor;
    private Vector3 _baseLocalPosition;
    private float _smoothedMultiplier = 1f;
    private float _smoothedRangeMultiplier = 1f;
    private Vector3 _smoothedPositionOffset;

    private void OnEnable()
    {
        if (targetLight == null)
            targetLight = GetComponent<Light>();

        if (targetLight == null)
        {
            enabled = false;
            return;
        }

        _baseIntensity = useCustomBaseIntensity ? customBaseIntensity : targetLight.intensity;
        _baseRange = targetLight.range;
        _baseColor = targetLight.color;
        _baseLocalPosition = targetLight.transform.localPosition;
        _smoothedMultiplier = 1f;
        _smoothedRangeMultiplier = 1f;
        _smoothedPositionOffset = Vector3.zero;
    }

    /// <summary>
    /// Re-reads baseline intensity from Custom Base Intensity (or the light) and resets smoothed flicker state.
    /// Use after changing custom base intensity while playing, or click Recalculate baseline in the Inspector.
    /// </summary>
    [ContextMenu("Recalculate Baseline")]
    public void RecalculateBaseline()
    {
        if (targetLight == null)
            targetLight = GetComponent<Light>();

        if (targetLight == null)
            return;

        _baseIntensity = useCustomBaseIntensity ? customBaseIntensity : targetLight.intensity;
        _baseRange = targetLight.range;
        _baseColor = targetLight.color;
        _baseLocalPosition = targetLight.transform.localPosition;
        _smoothedMultiplier = 1f;
        _smoothedRangeMultiplier = 1f;
        _smoothedPositionOffset = Vector3.zero;
    }

    private void LateUpdate()
    {
        if (targetLight == null || !targetLight.enabled)
            return;

        float t = Time.time;
        float s = noiseSeed;

        float nSlow = Mathf.PerlinNoise(t * slowNoiseFrequency, s * 0.13f);
        float nMid = Mathf.PerlinNoise(t * midNoiseFrequency + 19.4f, s * 0.29f);
        float nDip = Mathf.PerlinNoise(t * dipNoiseFrequency + 101f, s * 0.41f + 2.7f);

        float combined = nSlow * 0.45f + nMid * 0.4f + (nSlow * nMid) * 0.15f;
        combined = Mathf.Clamp01(combined);

        float dipAtten = Mathf.Lerp(1f - dipStrength, 1f, Mathf.Clamp01(nDip * 1.15f));
        float targetMultiplier = Mathf.Lerp(1f - flickerAmount, 1f + flickerAmount, combined) * dipAtten;

        float k = smoothing;
        float lerpT = 1f - Mathf.Exp(-k * Time.deltaTime);
        _smoothedMultiplier = Mathf.Lerp(_smoothedMultiplier, targetMultiplier, lerpT);

        float intensity = _baseIntensity * _smoothedMultiplier;
        targetLight.intensity = intensity;

        if (enableRangeWobble)
        {
            float rNoise = Mathf.PerlinNoise(t * slowNoiseFrequency * 1.6f + 7f, s * 0.19f);
            float rTarget = Mathf.Lerp(1f - rangeWobbleAmount, 1f + rangeWobbleAmount, rNoise);
            _smoothedRangeMultiplier = Mathf.Lerp(_smoothedRangeMultiplier, rTarget, lerpT);
            targetLight.range = _baseRange * _smoothedRangeMultiplier;
        }

        if (enableColorShift)
        {
            float tWarm = Mathf.InverseLerp(1f + flickerAmount, 1f - flickerAmount, _smoothedMultiplier);
            tWarm = Mathf.Clamp01(tWarm) * colorShiftStrength;
            targetLight.color = Color.Lerp(_baseColor, warmTintColor, tWarm);
        }

        if (enablePositionJitter)
        {
            float f = positionJitterFrequency;
            float ox = Mathf.PerlinNoise(t * f, s) - 0.5f;
            float oy = Mathf.PerlinNoise(t * f + 13f, s + 5f) - 0.5f;
            float oz = Mathf.PerlinNoise(t * f + 29f, s + 11f) - 0.5f;
            Vector3 targetOffset = new Vector3(ox, oy, oz) * (2f * positionJitterRadius);
            _smoothedPositionOffset = Vector3.Lerp(_smoothedPositionOffset, targetOffset, lerpT);
            targetLight.transform.localPosition = _baseLocalPosition + _smoothedPositionOffset;
        }
    }

    private void OnDisable()
    {
        if (targetLight == null)
            return;

        targetLight.intensity = _baseIntensity;
        targetLight.range = _baseRange;
        targetLight.color = _baseColor;
        targetLight.transform.localPosition = _baseLocalPosition;
    }
}
