using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class StarterBundleBucket : MonoBehaviour
{
    public StarterBundle StarterBundle;

    public ViewRitual ViewRitual;
    public Ritual Ritual;

    public List<Card> Cards = new List<Card>();
    public List<CardReward> CardRewards = new List<CardReward>();

    public ViewTarget ViewTarget;
    public MeshRenderer HighlightRenderer;

    public ViewBuff ViewTrinket;

    public void Load(StarterBundle starterBundle, Action<ViewTarget> cardClicked)
    {
        StarterBundle = starterBundle;

        ViewTarget.OnClick = cardClicked;

        ViewRitual.Init(starterBundle.Ritual, false);

        for (int i = 0; i < 3; i++)
        {
            CardRewards[i].Load(starterBundle.Cards[i], null, false);
        }

        PlayerEffectDescriptionData descriptionData = starterBundle.Trinket.GetDescriptionData();
        ViewTrinket.SetBuffData(descriptionData);
        ViewTrinket.SetVisible(true);

        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        HighlightRenderer.enabled = selected;
    }
}
