using UnityEngine;

/// <summary>
/// Tuning for PostFXPulse (asset: Resources/Content/PostFXPulseSettings). Each pulse blends these values over the main
/// post-processing look: at full strength the vignette, fringing, exposure and saturation reach the values below, then
/// fade back. Untick Enabled to turn every pulse off.
/// </summary>
[CreateAssetMenu(menuName = "Pharmakos/Post FX Pulse Settings", fileName = "PostFXPulseSettings")]
public class PostFXPulseSettings : ScriptableObject
{
    [System.Serializable]
    public class Pulse
    {
        [Tooltip("Total length in seconds.")]
        public float Duration = 0.5f;
        [Tooltip("Seconds to reach full strength; the rest of Duration fades out.")]
        public float AttackTime = 0.06f;
        public Color VignetteColor = new Color(0.6f, 0f, 0f);
        [Range(0f, 1f)] public float VignetteIntensity = 0.5f;
        [Range(0.01f, 1f)] public float VignetteSmoothness = 0.6f;
        [Range(0f, 1f)] public float ChromaticAberration = 0.15f;
        [Tooltip("Post exposure in stops at full strength (negative darkens).")]
        public float Exposure = 0f;
        [Tooltip("Saturation offset at full strength (-100..100).")]
        public float Saturation = -15f;
    }

    public bool Enabled = true;

    [Header("Player takes damage")]
    public Pulse DamageTaken = new Pulse();
    [Tooltip("Strength for a 0-damage hit; each point of damage adds DamageStrengthPerPoint (capped at 1).")]
    [Range(0f, 1f)] public float DamageBaseStrength = 0.45f;
    [Range(0f, 0.5f)] public float DamageStrengthPerPoint = 0.12f;

    [Header("Ritual used")]
    public Pulse RitualUsed = new Pulse
    {
        Duration = 1.1f,
        AttackTime = 0.25f,
        VignetteColor = new Color(0.05f, 0.02f, 0f),
        VignetteIntensity = 0.5f,
        VignetteSmoothness = 0.5f,
        ChromaticAberration = 0.1f,
        Exposure = 0f,
        Saturation = 10f,
    };
}
