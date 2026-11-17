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
/// The single source of highlight colours. Lives at Assets/Resources/HighlightPalette.asset.
/// Card glow colours are applied from here at runtime, so per-prefab glow colour fields don't matter.
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

    [Tooltip("Choice glow for large framed picks (starter bundles). Same colours as Choice; its own pulse so it can be tuned separately.")]
    public MeshHighlightSettings ChoiceLarge;

    [Tooltip("Target glow for 3D mesh highlights (player portraits). Same colours as Target, mesh-shaped.")]
    public MeshHighlightSettings TargetMesh;

    [Tooltip("Intensity multiplier for the Selected state.")]
    public float SelectedIntensity = 1.45f;

    static HighlightPalette instance;
    static bool attemptedLoad;

    public static HighlightPalette Instance
    {
        get
        {
            if (instance == null && !attemptedLoad)
            {
                attemptedLoad = true;
                instance = Resources.Load<HighlightPalette>("HighlightPalette");
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
