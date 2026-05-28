using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ViewSpell : ViewCard
{
    //public TextMeshPro AttackText;
    //public TextMeshPro HealthText;

    public TextMeshPro SpellText;
    public SpriteRenderer TargetSprite;

    public Spell Spell;

    public void ResetForPool()
    {
        EnterCardMode();
        if (SpellText != null)
            SpellText.text = string.Empty;
        Spell = null;
        Card = null;
        Target = null;
        OnClick = null;
        SetHighlight(false);
        inDescriptiveMode = false;
    }

    public override void Load(Card cardData, Action<ViewTarget> onClick = null)
    {
        ViewFollower viewFollower = GetComponent<ViewFollower>();
        if (viewFollower != null)
            viewFollower.ApplyCardMode(isFollower: false);

        base.Load(cardData, onClick);
        Spell = cardData as Spell;
        if (SpellText != null)
            SpellText.text = cardData.GetText();
        Target = Spell;
        EnterCardMode();

        GetComponent<CardViewRaycastTarget>()?.SetActiveCard(this);
    }

    public void EnterCardMode()
    {
        CardHolder.SetActive(true);
        TargetSprite.enabled = false;
        CardCollider.enabled = true;
    }

    public void EnterTargetMode()
    {
        CardHolder.SetActive(false);
        TargetSprite.enabled = true;
        CardCollider.enabled = false;
    }

    protected override CardArtLayoutAsset GetArtLayoutAsset(bool descriptiveMode)
    {
        ViewFollower viewFollower = GetComponent<ViewFollower>();
        if (viewFollower == null)
            return null;

        return descriptiveMode
            ? viewFollower.DescriptiveArtLayoutAsset
            : viewFollower.FullArtLayoutAsset;
    }
}
