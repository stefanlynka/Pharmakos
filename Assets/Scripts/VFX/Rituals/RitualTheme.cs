using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// How one god answers a ritual (see RitualInvocationVfx): the colour of its light, its sigil, and the particles its
/// boon releases. Themes live at Resources/Content/RitualThemes/&lt;God&gt;.asset and are picked by <see cref="Ritual.God"/>,
/// falling back to Default, exactly like altar styles. Major and Minor rituals of a god share a theme; the Minor plays it
/// smaller and quicker. Each ritual's sound is its own clip (Audio/SFX/Rituals/Invocations/&lt;RitualClass&gt;).
/// Tools → Rituals → Create Missing Ritual Themes rebuilds any theme that was deleted, with its starting values.
/// </summary>
[CreateAssetMenu(menuName = "Hecatomb/Ritual Theme", fileName = "RitualTheme")]
public class RitualTheme : ScriptableObject
{
    const string Folder = "Content/RitualThemes/";
    const string DefaultName = "Default";

    [Header("Light")]
    [Tooltip("The god's light: sigil, ground seal, light shaft, flash and the coloured spotlight.")]
    public Color Glow = new Color(1f, 0.78f, 0.45f);
    [Tooltip("Second colour: the hot centre of the flash and the shockwave.")]
    public Color Accent = new Color(1f, 0.95f, 0.85f);
    [Tooltip("Brightness of the sigil, seal and shaft. Above 1 they catch the bloom.")]
    [Range(0.5f, 6f)] public float GlowIntensity = 2.2f;
    [Tooltip("How far the board spotlight is tinted toward Glow (0 = white light).")]
    [Range(0f, 1f)] public float SpotlightTint = 0.6f;

    [Header("Timing")]
    [Tooltip("Speed of the whole invocation, relative to RitualInvocationVfx's beats: 0.75 (every god today) plays it in " +
             "75% of that time, 1 at the original, slower length. The god's sound clips must be rendered at the same pace " +
             "(DEFAULT_TEMPO / TEMPO in Tools/Audio/build_ritual_sounds.py), or the notes drift off the beats.")]
    [Range(0.5f, 1.25f)] public float Pace = 0.75f;

    [Header("Sigil")]
    [Tooltip("The god's emblem, white on transparent (Images/RitualFX/Sigils, built by Tools/RitualFX/build_ritual_textures.py).")]
    public Texture2D Sigil;
    [Range(0.5f, 2f)] public float SigilScale = 1f;

    [Header("Boon")]
    [Tooltip("Released from the target on the final note.")]
    public RitualParticleLayer Burst = new RitualParticleLayer();
    [Tooltip("Lingers after the boon: rising embers, falling petals, drifting ash. Starts on the final note.")]
    public RitualParticleLayer Drift = new RitualParticleLayer { Count = 24, Duration = 1.2f, Speed = new Vector2(0.5f, 2f), Gravity = -0.08f };
    [Tooltip("An extra layer, e.g. dark smoke under Hades' boon (untick Additive for smoke and shadow).")]
    public RitualParticleLayer Extra = new RitualParticleLayer { Enabled = false };

    [Header("Impact")]
    [Tooltip("Scales the screen shake on the final note.")]
    [Range(0f, 3f)] public float ShakeStrength = 1f;
    [Tooltip("Zeus: a lightning bolt strikes the target on the final note.")]
    public bool LightningStrike;

    static readonly Dictionary<string, RitualTheme> cache = new Dictionary<string, RitualTheme>();

    /// <summary>The theme for a ritual's god, falling back to Default. Null only if no Default theme exists.</summary>
    public static RitualTheme For(Ritual ritual)
    {
        if (ritual != null)
        {
            RitualTheme theme = Load(ritual.God);
            if (theme != null)
                return theme;
        }
        return Load(DefaultName);
    }

    static RitualTheme Load(string god)
    {
        if (string.IsNullOrEmpty(god))
            return null;
        if (!cache.TryGetValue(god, out RitualTheme theme) || theme == null)
        {
            theme = Resources.Load<RitualTheme>(Folder + god);
            cache[god] = theme;
        }
        return theme;
    }
}

public enum RitualParticleShape
{
    [Tooltip("Outward across the board from the target.")] Radial,
    [Tooltip("Outward in every direction, including toward the camera.")] Sphere,
    [Tooltip("Upward in a fan, like sparks or embers.")] Rise,
    [Tooltip("From above the target, falling onto it.")] Fall,
}

/// <summary>One particle layer of a ritual's boon. Colours are absolute (not tinted by the theme's Glow).</summary>
[Serializable]
public class RitualParticleLayer
{
    public bool Enabled = true;
    [Tooltip("White on transparent; Images/RitualFX holds Glow, Ember, Mote, Petal, Leaf, Feather, Droplet, Shard, Smoke.")]
    public Texture2D Texture;
    [Tooltip("Additive light. Untick for opaque-ish particles such as dark smoke, petals or leaves.")]
    public bool Additive = true;
    public RitualParticleShape Shape = RitualParticleShape.Radial;
    [Tooltip("Particles for a Major ritual. Minors use 60%.")]
    public int Count = 40;
    [Tooltip("0 = all at once. Otherwise they are released evenly over this many seconds.")]
    public float Duration = 0f;
    [Tooltip("Seconds after the final note before this layer starts.")]
    public float Delay = 0f;
    public Vector2 Speed = new Vector2(6f, 16f);
    public Vector2 Lifetime = new Vector2(0.6f, 1.4f);
    public Vector2 Size = new Vector2(0.5f, 1.4f);
    [Tooltip("Positive falls toward the bottom of the screen, negative rises.")]
    public float Gravity = 0f;
    [Tooltip("Slows particles down over their life.")]
    public float Drag = 2f;
    [Tooltip("Degrees per second, random in this range.")]
    public Vector2 Spin = new Vector2(-90f, 90f);
    [Tooltip("Stretch along the direction of travel (sparks, streaks).")]
    public bool Stretch;
    public Color ColorA = Color.white;
    public Color ColorB = Color.white;
    [Tooltip("Brightness multiplier for additive layers (above 1 blooms).")]
    [Range(0f, 6f)] public float Intensity = 1.5f;
}
