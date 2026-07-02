using System;
using TMPro;
using UnityEngine;

public class ViewCard : ViewTarget
{
    public SpriteRenderer ArtRenderer;
    public ViewOfferingCost ViewOfferingCost;
    public TextMeshPro NameText;
    public TextMeshPro NameTextWithoutCost;

    public Card Card;

    public SpriteRenderer Highlight;
    public CardHighlightEffect CardHighlightEffect;
    [SerializeField] MeshHighlightSettings highlightSettings;

    public GameObject CardHolder;
    public BoxCollider CardCollider;

    public GameObject FullTextBox;

    public RectTransform NameTextTransform;

    protected bool inDescriptiveMode = false;
    bool isCostShown = true;
    bool isHighlightActive;
    bool hasAppliedHighlightSettings;

    MaterialPropertyBlock artPropertyBlock;

    public bool IsInDescriptiveMode => inDescriptiveMode;
    public bool IsCostShown => isCostShown;

    public virtual void Load(Card cardData, Action<ViewTarget> onClick = null)
    {
        EnsureCardHighlight();
        CleanupNameText();
        Card = cardData;
        ViewOfferingCost.Load(Card.GetCosts());

        SetCostShown(true);

        ArtRenderer.sprite = CardHandler.GetSprite(cardData);
        SetNameText(cardData.GetName());

        OnClick = onClick;

        CardCollider.enabled = true;
        GetComponent<CardViewRaycastTarget>()?.SetActiveCard(this);

        SetDescriptiveMode(false);
    }

    public void SetHighlight(bool highlight)
    {
        EnsureCardHighlight();

        if (isHighlightActive == highlight)
            return;

        isHighlightActive = highlight;

        if (CardHighlightEffect != null)
            CardHighlightEffect.SetActive(highlight);
    }

    public bool IsHighlighted()
    {
        return isHighlightActive;
    }

    public void UpdateCost()
    {
        ViewOfferingCost.Load(Card.GetCosts());
    }

    public virtual void SetDescriptiveMode(bool value)
    {
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

    protected virtual CardArtLayoutAsset GetArtLayoutAsset(bool descriptiveMode) => null;

    protected CardArtClipRect GetArtLayoutForMode(bool descriptiveMode)
    {
        CardArtLayoutAsset asset = GetArtLayoutAsset(descriptiveMode);
        return asset != null ? asset.Layout : CardArtClipRect.Full;
    }

    protected void ApplyArtLayout(bool descriptiveMode)
    {
        if (ArtRenderer == null)
            return;

        CardArtClipRect layout = GetArtLayoutForMode(descriptiveMode);
        if (!isCostShown)
            layout.TopLeftCutout = CardArtCornerCutout.None;

        artPropertyBlock ??= new MaterialPropertyBlock();
        CardArtClipUtility.Apply(ArtRenderer, layout, artPropertyBlock);
    }

    public virtual void SetCostShown(bool value)
    {
        EnsureNameTextReferences();
        isCostShown = value;
        ViewOfferingCost.gameObject.SetActive(value);
        SetNameVisibility(value);
        float newWidth = value ? 7.4f : 8.8f;
        NameTextTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
        ApplyArtLayout(inDescriptiveMode);
    }

    protected void ApplyHighlightSettings(MeshHighlightSettings settings)
    {
        if (settings == null)
            return;

        highlightSettings = settings;
        EnsureCardHighlight();
        if (CardHighlightEffect != null)
        {
            CardHighlightEffect.ApplySettings(settings);
            hasAppliedHighlightSettings = true;
        }
    }

    void ApplyHighlightSettingsOnce()
    {
        if (hasAppliedHighlightSettings || highlightSettings == null)
            return;

        EnsureCardHighlight();
        if (CardHighlightEffect != null)
        {
            CardHighlightEffect.ApplySettings(highlightSettings);
            hasAppliedHighlightSettings = true;
        }
    }

    void SetNameText(string text)
    {
        if (NameText != null)
            NameText.text = text;
        if (NameTextWithoutCost != null)
            NameTextWithoutCost.text = text;
    }

    void SetNameVisibility(bool showTextWithCost)
    {
        if (NameText != null)
            NameText.gameObject.SetActive(showTextWithCost);

        if (NameTextWithoutCost != null)
            NameTextWithoutCost.gameObject.SetActive(!showTextWithCost);
        else if (!showTextWithCost && NameText != null)
            NameText.gameObject.SetActive(true);
    }

    protected void EnsureNameTextReferences()
    {
        if (NameText == null)
            NameText = transform.Find("TextWithCost")?.GetComponent<TextMeshPro>();

        if (NameTextWithoutCost == null)
            NameTextWithoutCost = transform.Find("TextWithoutCost")?.GetComponent<TextMeshPro>();
    }

    protected void CleanupNameText()
    {
        EnsureNameTextReferences();

        if (NameText != null)
        {
            NameText.text = string.Empty;
            NameText.gameObject.SetActive(true);
        }

        if (NameTextWithoutCost != null)
        {
            NameTextWithoutCost.text = string.Empty;
            NameTextWithoutCost.gameObject.SetActive(false);
        }

        isCostShown = true;

        if (NameTextTransform != null)
            NameTextTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 7.4f);

        if (ViewOfferingCost != null)
            ViewOfferingCost.gameObject.SetActive(true);
    }

    void Awake()
    {
        EnsureCardHighlight();
        ApplyHighlightSettingsOnce();
        SetHighlight(false);
    }

    void EnsureCardHighlight()
    {
        if (CardHighlightEffect == null)
            CardHighlightEffect = GetComponent<CardHighlightEffect>();
    }
}
