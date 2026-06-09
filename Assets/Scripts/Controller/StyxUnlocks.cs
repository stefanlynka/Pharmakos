using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Permanent (cross-run) unlocks for the two Styx trinkets, earned by defeating the
/// Fates and Gate bosses for the first time. Persisted via PlayerPrefs.
/// </summary>
public static class StyxUnlocks
{
    const string StringsOfFateKey = "StyxUnlock_StringsOfFate";
    const string GatesBeyondKey = "StyxUnlock_GatesBeyond";

    public static bool StringsOfFateUnlocked => PlayerPrefs.GetInt(StringsOfFateKey, 0) == 1;
    public static bool GatesBeyondUnlocked => PlayerPrefs.GetInt(GatesBeyondKey, 0) == 1;
    public static bool AnyUnlocked => StringsOfFateUnlocked || GatesBeyondUnlocked;

    public static void UnlockStringsOfFate()
    {
        PlayerPrefs.SetInt(StringsOfFateKey, 1);
        PlayerPrefs.Save();
    }

    public static void UnlockGatesBeyond()
    {
        PlayerPrefs.SetInt(GatesBeyondKey, 1);
        PlayerPrefs.Save();
    }

    public static void SetStringsOfFateUnlocked(bool unlocked)
    {
        if (unlocked) PlayerPrefs.SetInt(StringsOfFateKey, 1);
        else PlayerPrefs.DeleteKey(StringsOfFateKey);
        PlayerPrefs.Save();
    }

    public static void SetGatesBeyondUnlocked(bool unlocked)
    {
        if (unlocked) PlayerPrefs.SetInt(GatesBeyondKey, 1);
        else PlayerPrefs.DeleteKey(GatesBeyondKey);
        PlayerPrefs.Save();
    }

    public static List<Trinket> GetUnlockedTrinkets()
    {
        List<Trinket> trinkets = new List<Trinket>();
        if (StringsOfFateUnlocked) trinkets.Add(new StringsOfFateTrinket());
        if (GatesBeyondUnlocked) trinkets.Add(new GatesBeyondTrinket());
        return trinkets;
    }
}
