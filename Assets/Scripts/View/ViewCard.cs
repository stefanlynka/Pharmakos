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

    public MeshRenderer Highlight;

    public GameObject CardHolder;
    public BoxCollider CardCollider;
    
    //public Action<ViewCard> OnClick = null;


    public virtual void Load(Card cardData, Action<ViewTarget> onClick = null)
    {
        Card = cardData;
        ViewOfferingCost.Load(Card.Costs);

        ArtRenderer.sprite = CardHandler.GetSprite(cardData);
        NameText.text = cardData.GetType().Name;
        AbilityText.text = cardData.Text;

        OnClick = onClick;
    }

    public void SetHighlight(bool highlight)
    {
        Highlight.enabled = highlight;
    }
    public bool IsHighlighted()
    {
        return Highlight.enabled;
    }
}
