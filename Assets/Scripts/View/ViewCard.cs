using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class ViewCard : ViewTarget
{
    public SpriteRenderer ArtRenderer;
    public ViewOfferingCost ViewOfferingCost;
    public TextMeshPro NameText;
    public TextMeshPro AbilityText;

    public Card Card;

    public SpriteRenderer Highlight;

    public GameObject CardHolder;
    public BoxCollider CardCollider;

    public GameObject FullTextBox;

    protected bool inDescriptiveMode = false;
    
    //public Action<ViewCard> OnClick = null;


    public virtual void Load(Card cardData, Action<ViewTarget> onClick = null)
    {
        Card = cardData;
        ViewOfferingCost.Load(Card.GetCosts());

        ArtRenderer.sprite = CardHandler.GetSprite(cardData);
        NameText.text = cardData.GetName();
        AbilityText.text = cardData.Text;

        OnClick = onClick;

        CardCollider.enabled = true;

        SetDescriptiveMode(false);
    }

    public void SetHighlight(bool highlight)
    {
        Highlight.enabled = highlight;
    }
    public bool IsHighlighted()
    {
        return Highlight.enabled;
    }

    public void UpdateCost()
    {
        ViewOfferingCost.Load(Card.GetCosts());
    }

    public virtual void SetDescriptiveMode(bool value)
    {
        //if (inDescriptiveMode == value) return;
        if (Card.Text == string.Empty) value = false;

        inDescriptiveMode = value;

        FullTextBox.SetActive(value);
    }
}
