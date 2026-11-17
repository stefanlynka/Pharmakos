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

    bool appliedChoicePalette;

    void EnsureCardHighlight()
    {
        if (CardHighlightEffect == null)
            CardHighlightEffect = GetComponentInChildren<CardHighlightEffect>(true);

        // A bundle is a pick outside combat: Choice glow (Docs/StyleGuide.md). The selected bundle is
        // the current pick, so it glows at Selected intensity to read against the pale marble.
        HighlightPalette palette = HighlightPalette.Instance;
        MeshHighlightSettings preset = palette == null ? null : (palette.ChoiceLarge != null ? palette.ChoiceLarge : palette.Choice);
        if (!appliedChoicePalette && CardHighlightEffect != null && preset != null)
        {
            CardHighlightEffect.ApplySettings(preset);
            CardHighlightEffect.IntensityScale = HighlightPalette.Instance.SelectedIntensity;
            WidenGlowBand();
            appliedChoicePalette = true;
        }
    }

    [Tooltip("The glow sprite's 9-slice border is sized for cards, so on a full-width bundle the band is a hairline. " +
             "These scale the band (not the box) for the core and outer glow.")]
    public float GlowCoreBandScale = 1.5f;
    public float GlowOuterBandScale = 2.5f;

    // Scaling a sliced sprite's transform by k while dividing its size by k keeps the outer edge
    // in place but thickens the border band by k.
    void WidenGlowBand()
    {
        foreach (SpriteRenderer glow in CardHighlightEffect.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (glow.drawMode == SpriteDrawMode.Simple)
                continue;

            float k = glow.name.EndsWith("_OuterGlow") ? GlowOuterBandScale : GlowCoreBandScale;
            Vector3 scale = glow.transform.localScale;
            Vector2 worldSize = new Vector2(glow.size.x * scale.x, glow.size.y * scale.y);
            glow.transform.localScale = new Vector3(k, k, scale.z);
            glow.size = worldSize / k;
        }
    }
}
