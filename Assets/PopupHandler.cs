using UnityEngine;

public class PopupHandler : MonoBehaviour
{
    public static PopupHandler Instance;

    public PopupCapture Popup1;
    public PopupCapture Popup2;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static void ShowCard(PopupSlot slot, Card card) => Instance?.ShowCardInternal(slot, card);
    public static void ShowRitual(PopupSlot slot, Ritual ritual) => Instance?.ShowRitualInternal(slot, ritual);
    public static void ShowTrinket(PopupSlot slot, Trinket trinket) => Instance?.ShowTrinketInternal(slot, trinket);
    public static void ShowBuff(PopupSlot slot, StaticPlayerEffect buff, int amount = 1) =>
        Instance?.ShowBuffInternal(slot, buff, null, amount);
    public static void ShowBuff(PopupSlot slot, StaticPlayerEffect buff, PlayerEffectDescriptionData descriptionData, int amount = 1) =>
        Instance?.ShowBuffInternal(slot, buff, descriptionData, amount);
    public static void ShowHeartstring(PopupSlot slot) => Instance?.ShowHeartstringInternal(slot);
    public static void Clear(PopupSlot slot) => Instance?.ClearInternal(slot);
    public static void ClearAll()
    {
        Clear(PopupSlot.One);
        Clear(PopupSlot.Two);
    }

    void ShowCardInternal(PopupSlot slot, Card card) => GetCapture(slot)?.LoadCard(card);
    void ShowRitualInternal(PopupSlot slot, Ritual ritual) => GetCapture(slot)?.LoadRitual(ritual);
    void ShowTrinketInternal(PopupSlot slot, Trinket trinket) => GetCapture(slot)?.LoadTrinket(trinket);
    void ShowBuffInternal(PopupSlot slot, StaticPlayerEffect buff, PlayerEffectDescriptionData descriptionData, int amount) =>
        GetCapture(slot)?.LoadBuff(buff, descriptionData, amount);
    void ShowHeartstringInternal(PopupSlot slot) => GetCapture(slot)?.LoadHeartstring();
    void ClearInternal(PopupSlot slot) => GetCapture(slot)?.Clear();

    PopupCapture GetCapture(PopupSlot slot)
    {
        if (slot == PopupSlot.One)
            return Popup1;
        if (slot == PopupSlot.Two)
            return Popup2;

        Debug.LogWarning($"PopupHandler: unknown slot {slot}");
        return null;
    }
}

public enum PopupSlot
{
    One,
    Two
}

public enum PopupType
{
    None,
    Follower,
    Spell,
    Ritual,
    Trinket,
    Heartstring
}
