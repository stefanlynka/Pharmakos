using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Short full-screen post-processing pulses for game moments (taking damage, using a ritual): a coloured vignette,
/// lens fringing, a saturation dip and an exposure kick that swell in and fade out over the main look. Plays on its own
/// runtime global Volume at high priority, so the scene's Global Post Processing Volume (the authored look) is never
/// touched. Tune the pulses in Resources/Content/PostFXPulseSettings; untick Enabled there to turn them all off.
/// Call PostFXPulse.Play(...) from view code only (never from game logic / AI simulation).
/// </summary>
public class PostFXPulse : MonoBehaviour
{
    const string SettingsPath = "Content/PostFXPulseSettings";

    static PostFXPulse instance;

    PostFXPulseSettings settings;
    Volume volume;
    VolumeProfile profile;
    Vignette vignette;
    ChromaticAberration chroma;
    ColorAdjustments adjust;

    PostFXPulseSettings.Pulse current;
    float strength;
    float elapsed;
    bool playing;

    public static void Damage(int amount)
    {
        PostFXPulseSettings s = LoadSettings();
        if (s == null)
            return;
        float strength = Mathf.Clamp01(s.DamageBaseStrength + amount * s.DamageStrengthPerPoint);
        Play(s.DamageTaken, strength);
    }

    public static void Ritual()
    {
        PostFXPulseSettings s = LoadSettings();
        if (s != null)
            Play(s.RitualUsed, 1f);
    }

    public static void Play(PostFXPulseSettings.Pulse pulse, float strength)
    {
        PostFXPulseSettings s = LoadSettings();
        if (s == null || !s.Enabled || pulse == null || pulse.Duration <= 0f || !Application.isPlaying)
            return;

        if (instance == null)
        {
            GameObject go = new GameObject("PostFXPulse");
            instance = go.AddComponent<PostFXPulse>();
            instance.Setup(s);
        }
        instance.Begin(pulse, strength);
    }

    static PostFXPulseSettings cachedSettings;
    static PostFXPulseSettings LoadSettings()
    {
        if (cachedSettings == null)
            cachedSettings = Resources.Load<PostFXPulseSettings>(SettingsPath);
        return cachedSettings;
    }

    void Setup(PostFXPulseSettings s)
    {
        settings = s;
        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        vignette = profile.Add<Vignette>(false);
        chroma = profile.Add<ChromaticAberration>(false);
        adjust = profile.Add<ColorAdjustments>(false);

        volume = gameObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 100f;
        volume.weight = 0f;
        volume.sharedProfile = profile;
    }

    void Begin(PostFXPulseSettings.Pulse pulse, float pulseStrength)
    {
        // a weaker pulse never cuts a stronger one short
        if (playing && Envelope() * strength > pulseStrength)
            return;

        current = pulse;
        strength = pulseStrength;
        elapsed = 0f;
        playing = true;

        vignette.color.Override(pulse.VignetteColor);
        vignette.intensity.Override(pulse.VignetteIntensity);
        vignette.smoothness.Override(pulse.VignetteSmoothness);
        vignette.active = pulse.VignetteIntensity > 0f;
        chroma.intensity.Override(pulse.ChromaticAberration);
        chroma.active = pulse.ChromaticAberration > 0f;
        adjust.postExposure.Override(pulse.Exposure);
        adjust.saturation.Override(pulse.Saturation);
        adjust.active = pulse.Exposure != 0f || pulse.Saturation != 0f;
    }

    // 0..1: a quick rise over AttackTime, then the rest of Duration easing back out
    float Envelope()
    {
        if (current == null)
            return 0f;
        float attack = Mathf.Clamp(current.AttackTime, 0.001f, current.Duration);
        if (elapsed < attack)
            return Mathf.SmoothStep(0f, 1f, elapsed / attack);
        float t = Mathf.Clamp01((elapsed - attack) / Mathf.Max(current.Duration - attack, 0.001f));
        return 1f - t * t * (3f - 2f * t);
    }

    void Update()
    {
        if (!playing)
            return;

        elapsed += Time.unscaledDeltaTime;
        volume.weight = Envelope() * strength;
        if (elapsed >= current.Duration)
        {
            playing = false;
            volume.weight = 0f;
        }
    }

    void OnDestroy()
    {
        if (profile != null)
            Destroy(profile);
        if (instance == this)
            instance = null;
    }
}
