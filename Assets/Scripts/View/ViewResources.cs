using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ViewResources : MonoBehaviour
{
    public TextMeshPro GoldText;
    public TextMeshPro BloodText;
    public TextMeshPro BonesText;
    public TextMeshPro CropsText;
    public TextMeshPro ScrollsText;

    public OfferingLabel GoldLabel;
    public OfferingLabel BloodLabel;
    public OfferingLabel BonesLabel;
    public OfferingLabel CropsLabel;
    public OfferingLabel ScrollsLabel;


    private Player player;
    private readonly Dictionary<OfferingType, int> pendingCollectReveals = new Dictionary<OfferingType, int>();
    private readonly Dictionary<OfferingType, int> displayedAmounts = new Dictionary<OfferingType, int>();
    private readonly Dictionary<OfferingType, int> reservedCollectCounts = new Dictionary<OfferingType, int>();

    private static readonly OfferingType[] AllOfferingTypes =
    {
        OfferingType.Gold,
        OfferingType.Blood,
        OfferingType.Bone,
        OfferingType.Crop,
        OfferingType.Scroll
    };

    public void Init(Player player)
    {
        this.player = player;

        GoldLabel.SetSummaryText("Used to play cards. You have " + player.GoldPerTurn + " gold to spend each turn");
        BloodLabel.SetSummaryText("When a Player's life changes on your turn, gain that much Blood");
        BonesLabel.SetSummaryText("When a Follower dies on your turn, gain one Bone");
        CropsLabel.SetSummaryText("When a Follower is summoned on your turn, gain one Crop");
        ScrollsLabel.SetSummaryText("When you play a Spell on your turn, gain one Scroll");

        GoldLabel.OfferingName.text = "Gold";
        BloodLabel.OfferingName.text = "Blood";
        BonesLabel.OfferingName.text = "Bones";
        CropsLabel.OfferingName.text = "Crops";
        ScrollsLabel.OfferingName.text = "Scrolls";

        GoldLabel.Icon.sprite = OfferingHandler.Instance.GetOfferingFrameSprite(OfferingType.Gold);
        BloodLabel.Icon.sprite = OfferingHandler.Instance.GetOfferingFrameSprite(OfferingType.Blood);
        BonesLabel.Icon.sprite = OfferingHandler.Instance.GetOfferingFrameSprite(OfferingType.Bone);
        CropsLabel.Icon.sprite = OfferingHandler.Instance.GetOfferingFrameSprite(OfferingType.Crop);
        ScrollsLabel.Icon.sprite = OfferingHandler.Instance.GetOfferingFrameSprite(OfferingType.Scroll);

        player.OnOfferingsChange -= RefreshResources;
        player.OnOfferingsChange += RefreshResources;

        pendingCollectReveals.Clear();
        displayedAmounts.Clear();
        reservedCollectCounts.Clear();
        foreach (OfferingType type in AllOfferingTypes)
            displayedAmounts[type] = GetActualAmount(type);

        RefreshResources();
    }

    /// <summary>
    /// Called when an offering sprite arrives: advances the displayed count and returns
    /// the pitch index for the collect sound. The spotlight pulse is separate (on audio play).
    /// </summary>
    public int RevealCollectedOffering(OfferingType type)
    {
        int displayed = GetDisplayedAmount(type);
        if (!reservedCollectCounts.TryGetValue(type, out int reserved) || reserved < displayed)
            reserved = displayed;

        reserved++;
        reservedCollectCounts[type] = reserved;

        if (pendingCollectReveals.TryGetValue(type, out int pending) && pending > 0)
            pendingCollectReveals[type] = pending - 1;

        displayedAmounts[type] = displayed + 1;
        ApplyDisplayedAmount(type);
        SyncRitualReadyState();

        return reserved;
    }

    public void RefreshResources()
    {
        foreach (OfferingType type in AllOfferingTypes)
        {
            // Don't clobber in-flight collect reveals.
            if (GetPending(type) > 0)
                continue;

            displayedAmounts[type] = GetActualAmount(type);
            reservedCollectCounts.Remove(type);
        }

        ApplyAllDisplayedAmounts();
    }

    /// <summary>
    /// Snap labels to the player's current offerings and cancel any in-flight collect reveals.
    /// Used when the next turn banner appears.
    /// </summary>
    public void SyncToPlayerState()
    {
        pendingCollectReveals.Clear();
        reservedCollectCounts.Clear();
        foreach (OfferingType type in AllOfferingTypes)
            displayedAmounts[type] = GetActualAmount(type);

        ApplyAllDisplayedAmounts();
        SyncRitualReadyState();
    }

    public void QueuePendingCollect(OfferingType type, int amount)
    {
        if (amount <= 0) return;

        pendingCollectReveals.TryGetValue(type, out int pending);
        pending += amount;
        pendingCollectReveals[type] = pending;

        // Hide newly gained amount until offerings arrive and reveal it.
        // If live totals were already end-of-turn reset, keep the current displayed value.
        int fromActual = GetActualAmount(type) - pending;
        if (fromActual >= 0)
            displayedAmounts[type] = fromActual;

        ApplyDisplayedAmount(type);
    }

    public OfferingLabel GetOfferingLabel(OfferingType type)
    {
        return type switch
        {
            OfferingType.Gold => GoldLabel,
            OfferingType.Blood => BloodLabel,
            OfferingType.Bone => BonesLabel,
            OfferingType.Crop => CropsLabel,
            OfferingType.Scroll => ScrollsLabel,
            _ => null
        };
    }

    public void PlayCollectPulse(OfferingType type)
    {
        OfferingLabel label = GetOfferingLabel(type);
        if (label != null)
            label.PlayCollectPulse();
    }

    public int GetDisplayedAmount(OfferingType type)
    {
        if (displayedAmounts.TryGetValue(type, out int displayed))
            return displayed;

        return GetActualAmount(type);
    }

    public bool HasDisplayedOfferingsFor(Ritual ritual)
    {
        if (ritual == null)
            return false;

        foreach (KeyValuePair<OfferingType, int> cost in ritual.Costs)
        {
            if (GetDisplayedAmount(cost.Key) < ritual.GetCost(cost.Key))
                return false;
        }

        return true;
    }

    void SyncRitualReadyState()
    {
        if (player == null || View.Instance == null)
            return;

        ViewPlayer viewPlayer = View.Instance.GetViewPlayer(player);
        if (viewPlayer == null)
            return;

        if (viewPlayer.ViewMajorRitual != null)
            viewPlayer.ViewMajorRitual.SyncOfferingReadyState();
        if (viewPlayer.ViewMinorRitual != null)
            viewPlayer.ViewMinorRitual.SyncOfferingReadyState();
    }

    private void ApplyAllDisplayedAmounts()
    {
        ApplyDisplayedAmount(OfferingType.Gold);
        ApplyDisplayedAmount(OfferingType.Blood);
        ApplyDisplayedAmount(OfferingType.Bone);
        ApplyDisplayedAmount(OfferingType.Crop);
        ApplyDisplayedAmount(OfferingType.Scroll);
    }

    private void ApplyDisplayedAmount(OfferingType type)
    {
        OfferingLabel label = GetOfferingLabel(type);
        if (label == null || label.OfferingAmount == null)
            return;

        int shown = GetDisplayedAmount(type);
        if (type == OfferingType.Gold)
            label.OfferingAmount.text = shown + "/" + (player != null ? player.GoldPerTurn : 0);
        else
            label.OfferingAmount.text = shown.ToString();
    }

    private int GetActualAmount(OfferingType type)
    {
        if (player == null || player.Offerings == null || !player.Offerings.ContainsKey(type))
            return 0;

        return player.Offerings[type];
    }

    private int GetPending(OfferingType type)
    {
        pendingCollectReveals.TryGetValue(type, out int pending);
        return pending;
    }

    public Vector3 GetOfferingPosition(OfferingType type)
    {
        TextMeshPro text = type switch
        {
            OfferingType.Gold => GoldText,
            OfferingType.Blood => BloodText,
            OfferingType.Bone => BonesText,
            OfferingType.Crop => CropsText,
            OfferingType.Scroll => ScrollsText,
            _ => null
        };

        if (text == null)
        {
            Debug.LogError("Offering type not recognized: " + type.ToString());
            return transform.position;
        }

        return text.transform.position;
    }
}
