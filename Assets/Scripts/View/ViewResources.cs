using System.Collections;
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
        RefreshResources();
    }

    public void RefreshResources()
    {
        ApplyDisplayedAmount(OfferingType.Gold);
        ApplyDisplayedAmount(OfferingType.Blood);
        ApplyDisplayedAmount(OfferingType.Bone);
        ApplyDisplayedAmount(OfferingType.Crop);
        ApplyDisplayedAmount(OfferingType.Scroll);
    }

    public void QueuePendingCollect(OfferingType type, int amount)
    {
        if (amount <= 0) return;

        pendingCollectReveals.TryGetValue(type, out int pending);
        pendingCollectReveals[type] = pending + amount;
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
        if (pendingCollectReveals.TryGetValue(type, out int pending) && pending > 0)
            pendingCollectReveals[type] = pending - 1;

        ApplyDisplayedAmount(type);

        OfferingLabel label = GetOfferingLabel(type);
        if (label != null)
            label.PlayCollectPulse();

        SyncRitualReadyState();
    }

    public int GetDisplayedAmount(OfferingType type)
    {
        if (player == null || player.Offerings == null || !player.Offerings.ContainsKey(type))
            return 0;

        int actual = player.Offerings[type];
        pendingCollectReveals.TryGetValue(type, out int pending);
        if (pending > actual)
            pending = actual;
        if (pending < 0)
            pending = 0;

        return actual - pending;
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

    private void ApplyDisplayedAmount(OfferingType type)
    {
        if (player == null || player.Offerings == null || !player.Offerings.ContainsKey(type))
            return;

        OfferingLabel label = GetOfferingLabel(type);
        if (label == null || label.OfferingAmount == null)
            return;

        int actual = player.Offerings[type];
        pendingCollectReveals.TryGetValue(type, out int pending);
        if (pending > actual)
        {
            pending = actual;
            pendingCollectReveals[type] = pending;
        }

        int shown = actual - pending;
        if (type == OfferingType.Gold)
            label.OfferingAmount.text = shown + "/" + player.GoldPerTurn;
        else
            label.OfferingAmount.text = shown.ToString();
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
