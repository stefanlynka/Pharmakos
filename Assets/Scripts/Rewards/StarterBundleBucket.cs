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
    public CardHighlightEffect CardHighlightEffect;

    public ViewBuff ViewTrinket;

    void Awake()
    {
        EnsureCardHighlight();
        SetSelected(false);
    }

    public void Load(StarterBundle starterBundle, Action<ViewTarget> cardClicked)
    {
        StarterBundle = starterBundle;

        if (ViewTarget != null)
        {
            ViewTarget.gameObject.SetActive(true);
            ViewTarget.OnClick = cardClicked;
        }

        ViewRitual.Init(starterBundle.Ritual, false);
        EnableRitualHover(cardClicked);

        for (int i = 0; i < 3; i++)
        {
            CardRewards[i].Load(starterBundle.Cards[i], null, false);
        }

        PlayerEffectDescriptionData descriptionData = starterBundle.Trinket.GetDescriptionData();
        ViewTrinket.SetBuffData(starterBundle.Trinket.MyEffect, descriptionData);
        ViewTrinket.SetVisible(true);

        SetSelected(false);
    }

    void EnableRitualHover(Action<ViewTarget> bucketClicked)
    {
        if (ViewRitual == null)
            return;

        ViewRitual.SummaryPosition = PopupPosition.Left;

        BoxCollider collider = ViewRitual.GetComponent<BoxCollider>();
        if (collider != null)
            collider.enabled = true;

        ViewTarget bucket = ViewTarget;
        ViewRitual.OnClick = _ => bucketClicked?.Invoke(bucket);
    }

    public void SetSelected(bool selected)
    {
        EnsureCardHighlight();

        if (CardHighlightEffect != null)
            CardHighlightEffect.SetActive(selected);
    }

    // The glow preset (ChoiceLarge) is assigned on the Highlight object in the prefab. Its band is widened in the
    // prefab itself: the glow sprites are scaled up with their 9-slice size scaled down by the same factor.
    void EnsureCardHighlight()
    {
        if (CardHighlightEffect == null)
            CardHighlightEffect = GetComponentInChildren<CardHighlightEffect>(true);
    }
}
