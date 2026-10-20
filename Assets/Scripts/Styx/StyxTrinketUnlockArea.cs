using UnityEngine;

/// <summary>
/// Reveal altar for a newly unlocked Styx trinket. Shears are Strings of Fate and the
/// hammer is Gates Beyond. Only the unlocked object is shown, and hovering it describes
/// the effect. Nothing on this altar is selected or highlighted.
/// </summary>
public class StyxTrinketUnlockArea : MonoBehaviour
{
    public Camera AreaCamera;
    public StyxTrinketOption Shears;
    public StyxTrinketOption Hammer;

    void Awake()
    {
        Prepare(Shears);
        Prepare(Hammer);
        if (AreaCamera != null)
            AreaCamera.enabled = false;
    }

    public void Show(Trinket trinket)
    {
        if (AreaCamera != null)
        {
            AreaCamera.gameObject.SetActive(true);
            AreaCamera.enabled = true;
        }

        if (Shears == null)
            Shears = FindOption(StyxTrinketOption.Kind.StringsOfFate);
        if (Hammer == null)
            Hammer = FindOption(StyxTrinketOption.Kind.GatesBeyond);

        bool showShears = trinket is StringsOfFateTrinket;
        bool showHammer = trinket is GatesBeyondTrinket;
        Configure(Shears, showShears ? trinket : null);
        Configure(Hammer, showHammer ? trinket : null);
    }

    public void Hide()
    {
        Configure(Shears, null);
        Configure(Hammer, null);

        if (AreaCamera != null)
            AreaCamera.enabled = false;
    }

    static void Prepare(StyxTrinketOption option)
    {
        if (option == null)
            return;

        option.Selectable = false;
        option.Configure(null);
    }

    static void Configure(StyxTrinketOption option, Trinket trinket)
    {
        if (option == null)
            return;

        option.Selectable = false;
        option.Configure(trinket);
    }

    StyxTrinketOption FindOption(StyxTrinketOption.Kind kind)
    {
        StyxTrinketOption[] options = GetComponentsInChildren<StyxTrinketOption>(true);
        for (int i = 0; i < options.Length; i++)
        {
            if (options[i] != null && options[i].TrinketKind == kind)
                return options[i];
        }

        return null;
    }
}
