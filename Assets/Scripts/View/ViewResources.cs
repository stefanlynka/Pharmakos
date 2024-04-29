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

    private Player player;
    public void Init(Player player)
    {
        this.player = player;

        player.OnOfferingsChange -= RefreshResources;
        player.OnOfferingsChange += RefreshResources;

        RefreshResources();
    }

    public void RefreshResources()
    {
        GoldText.text = player.Offerings[OfferingType.Gold] + "/" + player.GoldPerTurn;
        BloodText.text = player.Offerings[OfferingType.Blood].ToString();
        BonesText.text = player.Offerings[OfferingType.Bone].ToString();
        CropsText.text = player.Offerings[OfferingType.Crop].ToString();
        ScrollsText.text = player.Offerings[OfferingType.Scroll].ToString();
    }
}
