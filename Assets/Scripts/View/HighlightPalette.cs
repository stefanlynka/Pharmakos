using UnityEngine;

/// <summary>
/// What a highlight means. Colour comes from the state (and, for Ready/Selected, from the card's kind):
/// see Docs/StyleGuide.md, "Interaction states".
/// </summary>
public enum HighlightState
{
    /// <summary>Can act now: playable card, follower that can attack. Glows in its offering colour (follower = Crop, spell = Scroll).</summary>
    Ready,
    /// <summary>Currently picked (e.g. the attacking follower). Its Ready colour, brighter.</summary>
    Selected,
    /// <summary>Valid target while aiming an attack, spell or ritual. Pulsing ember, whatever the kind.</summary>
    Target,
    /// <summary>A pick outside combat: rewards, shop, temple, starter bundles. Steady ember.</summary>
    Choice,
}

/// <summary>
/// Which preset a card's glow plays for each highlight state. Lives at Assets/Resources/Content/HighlightSettings/HighlightPalette.asset.
/// Only cards switch preset at runtime (their state changes); everything else plays the preset assigned in its inspector.
/// </summary>
[CreateAssetMenu(fileName = "HighlightPalette", menuName = "Hecatomb/Highlight Palette")]
public class HighlightPalette : ScriptableObject
{
    [Tooltip("Ready glow for followers: Crop green, because followers produce Crops.")]
    public MeshHighlightSettings Follower;

    [Tooltip("Ready glow for spells: Scroll blue, because spells produce Scrolls.")]
    public MeshHighlightSettings Spell;

    [Tooltip("Valid target while aiming. Pulsing ember; deliberately not an offering colour.")]
    public MeshHighlightSettings Target;

    [Tooltip("Pick-me glow outside combat (rewards, shop, temple, starter bundles).")]
    public MeshHighlightSettings Choice;

    [Tooltip("Brief flash when a card's triggered ability fires (On Death, On Attack...). Votive gold; not an interaction state, so it never stays lit.")]
    public MeshHighlightSettings Triggered;

    [Tooltip("Intensity multiplier for the Selected state.")]
    public float SelectedIntensity = 1.45f;

    /// <summary>Asset lives at Assets/Resources/Content/HighlightSettings/HighlightPalette.asset, next to its presets.</summary>
    const string ResourcePath = "Content/HighlightSettings/HighlightPalette";

    static HighlightPalette instance;
    static bool attemptedLoad;

    public static HighlightPalette Instance
    {
        get
        {
            if (instance == null && !attemptedLoad)
            {
                attemptedLoad = true;
                instance = Resources.Load<HighlightPalette>(ResourcePath);
            }
            return instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        instance = null;
        attemptedLoad = false;
    }

    /// <summary>The preset for a state. For Ready/Selected, pass whether the thing is a follower (else spell).</summary>
    public MeshHighlightSettings For(HighlightState state, bool isFollower)
    {
        switch (state)
        {
            case HighlightState.Target: return Target;
            case HighlightState.Choice: return Choice;
            default: return isFollower ? Follower : Spell;
        }
    }
}
