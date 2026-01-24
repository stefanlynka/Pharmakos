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
    public void Init(Player player)
    {
        this.player = player;

        GoldLabel.SummaryText.text = "Used to play cards. You have " + player.GoldPerTurn + " gold to spend each turn";
        BloodLabel.SummaryText.text = "When a Player's life changes on your turn, gain that much Blood";
        BonesLabel.SummaryText.text = "When a Follower dies on your turn, gain one Bone";
        CropsLabel.SummaryText.text = "When a Follower is summoned on your turn, gain one Crop";
        ScrollsLabel.SummaryText.text = "When you play a Spell on your turn, gain one Scroll";

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

        RefreshResources();
    }

    public void RefreshResources()
    {
        GoldLabel.OfferingAmount.text = player.Offerings[OfferingType.Gold] + "/" + player.GoldPerTurn;
        BloodLabel.OfferingAmount.text = player.Offerings[OfferingType.Blood].ToString();
        BonesLabel.OfferingAmount.text = player.Offerings[OfferingType.Bone].ToString();
        CropsLabel.OfferingAmount.text = player.Offerings[OfferingType.Crop].ToString();
        ScrollsLabel.OfferingAmount.text = player.Offerings[OfferingType.Scroll].ToString();

        //GoldText.text = player.Offerings[OfferingType.Gold] + "/" + player.GoldPerTurn;
        //BloodText.text = player.Offerings[OfferingType.Blood].ToString();
        //BonesText.text = player.Offerings[OfferingType.Bone].ToString();
        //CropsText.text = player.Offerings[OfferingType.Crop].ToString();
        //ScrollsText.text = player.Offerings[OfferingType.Scroll].ToString();
    }
}
