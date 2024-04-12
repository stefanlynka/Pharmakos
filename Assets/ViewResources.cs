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
        RefreshResources();
    }

    public void RefreshResources()
    {
        GoldText.text = player.Resources[OfferingType.Gold] + "/" + player.GoldPerTurn;
        BloodText.text = player.Resources[OfferingType.Blood].ToString();
        BonesText.text = player.Resources[OfferingType.Bone].ToString();
        CropsText.text = player.Resources[OfferingType.Crop].ToString();
        ScrollsText.text = player.Resources[OfferingType.Scroll].ToString();
    }
}
