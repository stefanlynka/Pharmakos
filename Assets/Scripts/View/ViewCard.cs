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

    public RectTransform NameTextTransform;

    [Header("Art Clip")]
    [SerializeField] CardArtClipRect artLayout = CardArtClipRect.Full;
    [SerializeField] CardArtLayoutAsset defaultArtLayoutAsset;
    [SerializeField] bool useDescriptiveArtLayout;
    [SerializeField] CardArtClipRect descriptiveArtLayout = CardArtClipRect.BottomHalf(0.55f);
    [SerializeField] CardArtLayoutAsset descriptiveArtLayoutAsset;

    protected bool inDescriptiveMode = false;
    MaterialPropertyBlock artPropertyBlock;

    //public Action<ViewCard> OnClick = null;


    public virtual void Load(Card cardData, Action<ViewTarget> onClick = null)
    {
        Card = cardData;
        ViewOfferingCost.Load(Card.GetCosts());

        // Show ViewOfferingCost when card is created
        SetCostShown(true);

        ArtRenderer.sprite = CardHandler.GetSprite(cardData);
        NameText.text = cardData.GetName();
        AbilityText.text = cardData.GetText();

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
        if (Card == null)
        {
            Debug.LogError("ViewCard: Card is null");
            return;
        }
        if (Card.Text == string.Empty) value = false;

        inDescriptiveMode = value;

        FullTextBox.SetActive(value);
        ApplyArtLayout(value);
    }

    public void SetArtLayout(CardArtClipRect layout)
    {
        artLayout = layout;
        ApplyArtLayout(inDescriptiveMode);
    }

    public void SetArtLayout(CardArtLayoutAsset layoutAsset)
    {
        if (layoutAsset == null)
        {
            return;
        }

        SetArtLayout(layoutAsset.Layout);
    }

    protected CardArtClipRect GetArtLayoutForMode(bool descriptiveMode)
    {
        if (descriptiveMode && useDescriptiveArtLayout)
        {
            if (descriptiveArtLayoutAsset != null)
            {
                return descriptiveArtLayoutAsset.Layout;
            }

            return descriptiveArtLayout;
        }

        if (defaultArtLayoutAsset != null)
        {
            return defaultArtLayoutAsset.Layout;
        }

        return artLayout;
    }

    protected void ApplyArtLayout(bool descriptiveMode)
    {
        if (ArtRenderer == null)
        {
            return;
        }

        artPropertyBlock ??= new MaterialPropertyBlock();
        CardArtClipUtility.Apply(ArtRenderer, GetArtLayoutForMode(descriptiveMode), artPropertyBlock);
    }

    public virtual void SetCostShown(bool value)
    {
        ViewOfferingCost.gameObject.SetActive(value);
        float newWidth = value ? 7.4f : 8.8f;
        NameTextTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
    }
}
