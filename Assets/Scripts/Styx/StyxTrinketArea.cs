using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The altar where the player chooses starting Styx trinkets. Shears are Strings of Fate
/// and the hammer is Gates Beyond. The select screen turns the camera on while it is open.
/// </summary>
public class StyxTrinketArea : MonoBehaviour
{
    public Camera AreaCamera;
    public StyxTrinketOption Shears;
    public StyxTrinketOption Hammer;

    public void ShowUnlocked()
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

        Configure(Shears, StyxUnlocks.StringsOfFateUnlocked ? new StringsOfFateTrinket() : null);
        Configure(Hammer, StyxUnlocks.GatesBeyondUnlocked ? new GatesBeyondTrinket() : null);
    }

    public void Hide()
    {
        if (Shears != null)
            Shears.Hide();
        if (Hammer != null)
            Hammer.Hide();

        if (AreaCamera != null)
            AreaCamera.enabled = false;
    }

    public void CollectSelected(List<Trinket> into)
    {
        AddIfSelected(Shears, into);
        AddIfSelected(Hammer, into);
    }

    static void Configure(StyxTrinketOption option, Trinket trinket)
    {
        if (option != null)
            option.Configure(trinket);
    }

    static void AddIfSelected(StyxTrinketOption option, List<Trinket> into)
    {
        if (option != null && option.Selected && option.Trinket != null)
            into.Add(option.Trinket);
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
