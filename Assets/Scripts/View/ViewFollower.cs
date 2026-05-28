using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ViewFollower : ViewCard
{
    [Header("Art Layout")]
    [SerializeField] CardArtLayoutAsset descriptiveArtLayoutAsset;
    [SerializeField] CardArtLayoutAsset statsArtLayoutAsset;
    [SerializeField] CardArtLayoutAsset summaryArtLayoutAsset;
    [SerializeField] CardArtLayoutAsset fullArtLayoutAsset;

    public CardArtLayoutAsset DescriptiveArtLayoutAsset => descriptiveArtLayoutAsset;
    public CardArtLayoutAsset StatsArtLayoutAsset => statsArtLayoutAsset;
    public CardArtLayoutAsset SummaryArtLayoutAsset => summaryArtLayoutAsset;
    public CardArtLayoutAsset FullArtLayoutAsset => fullArtLayoutAsset;

    [Header("Card mode")]
    public GameObject FollowerOnlyRoot;
    public GameObject SpellOnlyRoot;

    [Header("Card base")]
    [SerializeField] Renderer cardBaseRenderer;
    [SerializeField] Material followerMaterial;
    [SerializeField] Material spellMaterial;

    public TextMeshPro FollowerText;
    public TextMeshPro AttackText;
    public TextMeshPro HealthText;

    public SpriteRenderer SkullRenderer;

    public GameObject ReminderTextBox;

    public Follower Follower;

    public SpriteRenderer SummaryIcon;
    public SpriteRenderer SummaryLeftArrow;
    public SpriteRenderer SummaryRightArrow;

    public GameObject DamageIcon;
    public TextMeshPro DamageText;

    public GameObject StatDivider;

    private int attack = 0;
    private int health = 0;

    public void ApplyCardMode(bool isFollower)
    {
        if (FollowerOnlyRoot != null)
            FollowerOnlyRoot.SetActive(isFollower);
        if (SpellOnlyRoot != null)
            SpellOnlyRoot.SetActive(!isFollower);

        ApplyCardBaseMaterial(isFollower);
    }

    void ApplyCardBaseMaterial(bool isFollower)
    {
        if (cardBaseRenderer == null)
            cardBaseRenderer = FindCardBaseRenderer();

        Material material = isFollower ? followerMaterial : spellMaterial;
        if (cardBaseRenderer == null || material == null)
            return;

        cardBaseRenderer.sharedMaterial = material;
    }

    Renderer FindCardBaseRenderer()
    {
        if (CardHolder == null)
            return null;

        Transform cardBasic = CardHolder.transform.Find("CardBasic");
        return cardBasic != null ? cardBasic.GetComponentInChildren<Renderer>() : null;
    }

    public void ResetForPool()
    {
        if (Follower != null)
        {
            Follower.OnChange -= CardChanged;
            Follower = null;
        }

        HideDamage();
        if (FollowerText != null)
            FollowerText.text = string.Empty;
        Card = null;
        Target = null;
        OnClick = null;
        SetHighlight(false);
        inDescriptiveMode = false;
    }

    public override void Load(Card cardData, Action<ViewTarget> onClick = null)
    {
        ApplyCardMode(isFollower: true);
        Follower = cardData as Follower;
        if (Follower == null) return;

        Target = Follower;
        base.Load(cardData, onClick);
        if (FollowerText != null)
            FollowerText.text = cardData.GetText();
        SetStats(Follower.BaseAttack, Follower.BaseHealth);

        SummaryIcon.sprite = CardHandler.GetSummaryIcon(Follower);
        SummaryIcon.enabled = SummaryIcon.sprite != null;
        SummaryLeftArrow.enabled = Follower.AffectsAdjacent;
        SummaryRightArrow.enabled = Follower.AffectsAdjacent;

        DamageIcon.SetActive(false);

        Follower.OnChange -= CardChanged;
        Follower.OnChange += CardChanged;

        GetComponent<CardViewRaycastTarget>()?.SetActiveCard(this);
        //Follower.OnRemove -= CardRemoved;
        //Follower.OnRemove += CardRemoved;
        //attack = Follower.GetCurrentAttack();
        //health = Follower.CurrentHealth;
    }

    // Currently Unused
    private void CardRemoved()
    {
        //View.Instance.RemoveViewCard(this);
    }

    private void CardChanged()
    {
        //AttackText.text = Follower.GetCurrentAttack().ToString();
        //HealthText.text = Follower.CurrentHealth.ToString();
    }

    public void ShowMaxStats()
    {
        attack = Follower.GetCurrentAttack();
        health = Follower.MaxHealth;

        AttackText.text = attack.ToString();
        HealthText.text = health.ToString();
    }

    public void SetStats(int newAttack, int newHealth)
    {
        attack = newAttack;
        AttackText.text = attack.ToString();
        health = newHealth;
        HealthText.text = health.ToString();
    }
    public void ChangeStats(int attackChange, int healthChange)
    {
        attack += attackChange;
        AttackText.text = attack.ToString();

        health += healthChange;
        HealthText.text = health.ToString();
    }
    public void ChangeHealth(int change)
    {
        health += change;
        HealthText.text = health.ToString();
    }

    protected override CardArtLayoutAsset GetArtLayoutAsset(bool descriptiveMode)
    {
        if (descriptiveMode)
            return descriptiveArtLayoutAsset;

        return HasSummaryText() ? summaryArtLayoutAsset : statsArtLayoutAsset;
    }

    protected bool HasSummaryText() =>
        Follower != null && Follower.Icon != IconType.None;

    public override void SetDescriptiveMode(bool value)
    {
        if (Card == null) return;

        ReminderTextBox.SetActive(Card.Text != string.Empty);

        base.SetDescriptiveMode(value);
    }

    public void ShowDamage(int damage)
    {
        DamageIcon.SetActive(true);
        DamageText.text = damage.ToString();
    }
    public void HideDamage()
    {
        DamageIcon.SetActive(false);
    }
}
