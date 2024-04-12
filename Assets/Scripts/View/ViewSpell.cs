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

    public SpriteRenderer TargetSprite;

    public Spell Spell;

    public override void Load(Card cardData, Action<ViewTarget> onClick = null)
    {
        base.Load(cardData, onClick);
        Spell = cardData as Spell;
        Target = Spell;
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
}
