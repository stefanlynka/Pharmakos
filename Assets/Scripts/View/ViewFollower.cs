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

    [Header("Highlight")]
    [SerializeField] MeshHighlightSettings followerHighlightSettings;

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

    [SerializeField] DamageIconStroke damageStroke = new DamageIconStroke();

    public GameObject StatDivider;

    private int attack = 0;
    private int health = 0;

    public int DisplayedAttack => attack;
    public int DisplayedHealth => health;

    // Printed stats stay white. Numbers above the printed value are light green; damage and debuffs below it are light red.
    static readonly Color BaseStatColor = Color.white;
    static readonly Color RaisedStatColor = new Color(0.24f, 1f, 0.31f, 1f);
    static readonly Color LoweredStatColor = new Color(1f, 0.25f, 0.15f, 1f);

    const string PowerUpSoundPath = "Audio/SFX/PowerUp";
    const string PowerDownSoundPath = "Audio/SFX/PowerDown";
    const float StatChangeSoundOverlapWindow = 0.25f;
    const float StatChangeSoundVolume = 2.8f;

    static readonly Dictionary<string, AudioClip> statChangeClips = new Dictionary<string, AudioClip>();
    static float nextPowerUpSoundTime;
    static float nextPowerDownSoundTime;

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
        CleanupNameText();

        if (Follower != null)
        {
            Follower.OnChange -= CardChanged;
            Follower = null;
        }

        damageStroke.HideImmediate(this, DamageIcon);
        if (FollowerText != null)
            FollowerText.text = string.Empty;
        if (AttackText != null)
            AttackText.color = BaseStatColor;
        if (HealthText != null)
            HealthText.color = BaseStatColor;
        Card = null;
        Target = null;
        OnClick = null;
        SetHighlight(false);
        inDescriptiveMode = false;
        if (StatDivider != null)
            StatDivider.SetActive(false);
    }

    public override void Load(Card cardData, Action<ViewTarget> onClick = null)
    {
        ApplyCardMode(isFollower: true);
        ApplyHighlightSettings(followerHighlightSettings);
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
        ApplyStatText();
    }

    public void SetStats(int newAttack, int newHealth)
    {
        attack = newAttack;
        health = newHealth;
        ApplyStatText();
    }
    public void ChangeStats(int attackChange, int healthChange)
    {
        attack += attackChange;
        health += healthChange;
        ApplyStatText();
    }
    public void ChangeHealth(int change)
    {
        health += change;
        ApplyStatText();
    }

    void ApplyStatText()
    {
        int baseAttack = Follower != null ? Follower.BaseAttack : attack;
        int baseHealth = Follower != null ? Follower.BaseHealth : health;

        if (AttackText != null)
        {
            AttackText.text = attack.ToString();
            AttackText.color = StatColor(attack, baseAttack);
        }

        if (HealthText != null)
        {
            HealthText.text = health.ToString();
            HealthText.color = StatColor(health, baseHealth);
        }
    }

    static Color StatColor(int value, int baseValue)
    {
        if (value > baseValue)
            return RaisedStatColor;
        if (value < baseValue)
            return LoweredStatColor;
        return BaseStatColor;
    }

    // Green and PowerUp when the stats rise overall, red and PowerDown when they fall.
    // A wave of changes shares one sting. onComplete runs once the tint has faded, matching the sting.
    public bool PlayStatChangeFeedback(int attackChange, int healthChange, Action onComplete)
    {
        if (attackChange == 0 && healthChange == 0)
            return false;

        bool buff = attackChange + healthChange > 0;
        AudioClip clip = LoadStatChangeClip(buff ? PowerUpSoundPath : PowerDownSoundPath);
        PlayStatChangeSound(buff, clip);

        float duration = clip != null && clip.length > 0f ? clip.length/2f : 1f;
        VfxLibrary.StatChangeTint(buff, duration).Play(new VfxContext(null, this), onComplete);
        return true;
    }

    static void PlayStatChangeSound(bool buff, AudioClip clip)
    {
        if (clip == null || View.Instance == null || View.Instance.AudioHandler == null)
            return;

        float now = Time.unscaledTime;
        if (buff)
        {
            if (now < nextPowerUpSoundTime) return;
            nextPowerUpSoundTime = now + StatChangeSoundOverlapWindow;
        }
        else
        {
            if (now < nextPowerDownSoundTime) return;
            nextPowerDownSoundTime = now + StatChangeSoundOverlapWindow;
        }

        View.Instance.AudioHandler.PlayOneShot(clip, StatChangeSoundVolume);
    }

    static AudioClip LoadStatChangeClip(string path)
    {
        if (!statChangeClips.TryGetValue(path, out AudioClip clip))
        {
            clip = Resources.Load<AudioClip>(path);
            if (clip == null)
                Debug.LogWarning("ViewFollower: missing sound Resources/" + path);
            statChangeClips[path] = clip;
        }
        return clip;
    }

    protected override CardArtLayoutAsset GetArtLayoutAsset(bool descriptiveMode)
    {
        if (descriptiveMode)
            return descriptiveArtLayoutAsset;

        return HasSummaryText() ? summaryArtLayoutAsset : statsArtLayoutAsset;
    }

    protected bool HasSummaryText() =>
        Follower != null && Follower.Icon != IconType.None;

    void UpdateStatDividerVisibility()
    {
        if (StatDivider == null)
            return;

        StatDivider.SetActive(IsInDescriptiveMode || HasSummaryText());
    }

    public override void SetDescriptiveMode(bool value)
    {
        if (Card == null) return;

        ReminderTextBox.SetActive(Card.Text != string.Empty);

        base.SetDescriptiveMode(value);
        UpdateStatDividerVisibility();
    }

    public void ShowDamage(int damage)
    {
        damageStroke.Show(this, DamageIcon, DamageText, damage);
    }

    public void HideDamage()
    {
        damageStroke.Hide(this, DamageIcon, DamageText);
    }
}
