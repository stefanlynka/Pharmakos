using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// One entry in the play history (ContentScrollView renders these in a column, newest first).
/// A row reads as a sentence in fixed columns: source card, verb (icon + label), target. Each turn starts with a
/// header row ("Your Turn" / "Enemy Turn") instead of repeating the acting player's portrait on every row.
/// </summary>
public class ViewPlayHistoryItemNew : MonoBehaviour
{
    static readonly Quaternion FlatDisplayRotation = Quaternion.Euler(90f, 0f, 0f);

    public Transform ComponentParent;

    [Header("Row")]
    [Tooltip("Depth (screen height, in world units) a row takes in the column, padding included.")]
    public float RowDepth = 12f;
    [Tooltip("Centre X of the acting card / ritual.")]
    public float SourceX = -11f;
    [Tooltip("Centre X of the verb icon and label.")]
    public float VerbX = 0f;
    [Tooltip("Centre X of the target card / portrait.")]
    public float TargetX = 11f;
    public float CardScale = 1.3f;
    public float RitualScale = 0.45f;
    public float PortraitScale = 1.2f;
    [Tooltip("The player's portrait art is painted larger than enemy art; this shrinks it to sit inside the frame.")]
    public float HumanPortraitArtScale = 0.77f;
    [Tooltip("How much of their depth rituals and portraits keep. This camera looks straight down, so a raised "
        + "altar or frame beside the centre shows its sides in perspective; flattening them keeps rows clean.")]
    [Range(0.02f, 1f)] public float DepthFlatten = 0.1f;
    [Tooltip("Z offset of the verb icon above the row centre (the label sits below it).")]
    public float VerbIconZ = 1f;
    [Tooltip("Z of the verb label when the row has an icon; rows without one centre the label.")]
    public float VerbLabelZ = -2.4f;
    [Tooltip("Thin rule at the top of a row, shown between rows of the same turn.")]
    public SpriteRenderer Divider;
    public TextMeshPro VerbLabel;

    [Header("Turn header")]
    public float HeaderDepth = 5f;
    public GameObject Header;
    public TextMeshPro HeaderText;
    public SpriteRenderer HeaderRuleLeft;
    public SpriteRenderer HeaderRuleRight;
    [Tooltip("World units between the header text and each rule.")]
    public float HeaderRuleGap = 1.2f;

    public PlayHistoryItem PlayHistoryItem { get; private set; }
    public bool IsHeader { get; private set; }
    /// <summary>How much of the column this item takes, so headers can be shorter than rows.</summary>
    public float Depth => IsHeader ? HeaderDepth : RowDepth;

    readonly List<GameObject> _playHistoryComponents = new List<GameObject>();
    readonly List<Transform> _componentHolders = new List<Transform>();
    static ObjectPool<Transform> _holderPool;

    void Awake()
    {
        if (ComponentParent == null)
        {
            Transform holders = transform.Find("ComponentHolders");
            if (holders != null)
                ComponentParent = holders;
        }
    }

    static ObjectPool<Transform> HolderPool
    {
        get
        {
            if (_holderPool == null)
            {
                _holderPool = new ObjectPool<Transform>(
                    CreateHolder,
                    OnHolderGet,
                    OnHolderRelease,
                    null,
                    false);
            }

            return _holderPool;
        }
    }

    /// <summary>A turn header: "Your Turn" or "Enemy Turn".</summary>
    public void LoadHeader(Player owner) =>
        LoadMessage(owner != null && owner.IsHuman ? "Your Turn" : "Enemy Turn");

    /// <summary>Header-style line of text, e.g. for an empty history.</summary>
    public void LoadMessage(string text)
    {
        Clear();
        IsHeader = true;
        SetRowChrome(false, showDivider: false);
        if (Header != null)
            Header.SetActive(true);

        if (HeaderText == null)
            return;

        HeaderText.text = text;
        HeaderText.ForceMeshUpdate();
        float halfText = HeaderText.textBounds.extents.x;
        PlaceHeaderRule(HeaderRuleLeft, -1f, halfText);
        PlaceHeaderRule(HeaderRuleRight, 1f, halfText);
    }

    void PlaceHeaderRule(SpriteRenderer rule, float side, float halfText)
    {
        if (rule == null)
            return;

        float halfRule = rule.bounds.extents.x;
        Vector3 position = rule.transform.localPosition;
        position.x = HeaderText.transform.localPosition.x + side * (halfText + HeaderRuleGap + halfRule);
        rule.transform.localPosition = position;
    }

    public void Load(PlayHistoryItem playHistoryItem, bool showDivider)
    {
        Clear();
        PlayHistoryItem = playHistoryItem;
        IsHeader = false;
        if (Header != null)
            Header.SetActive(false);

        if (ComponentParent == null)
        {
            Debug.LogError("ViewPlayHistoryItemNew: ComponentParent is not assigned.");
            return;
        }

        // Components arrive as: source, [verb icon, target]. Plays without a target have no verb icon.
        List<PlayHistoryComponent> components = playHistoryItem.GetComponents();
        PlayHistoryComponent source = components.Count > 0 ? components[0] : null;
        PlayHistoryComponent verbIcon = null;
        PlayHistoryComponent target = null;
        for (int i = 1; i < components.Count; i++)
        {
            PlayHistoryComponentType type = components[i].GetComponentType();
            if (type == PlayHistoryComponentType.Attack || type == PlayHistoryComponentType.Target)
                verbIcon = components[i];
            else
                target = components[i];
        }

        if (source != null)
            AddComponent(source, SourceX, 0f);
        if (verbIcon != null)
            AddComponent(verbIcon, VerbX, VerbIconZ);
        if (target != null)
            AddComponent(target, TargetX, 0f);

        SetRowChrome(true, showDivider);
        if (VerbLabel != null)
        {
            VerbLabel.text = GetVerb(playHistoryItem, target != null);
            Vector3 labelPosition = VerbLabel.transform.localPosition;
            labelPosition.x = VerbX;
            labelPosition.z = verbIcon != null ? VerbLabelZ : 0f;
            VerbLabel.transform.localPosition = labelPosition;
        }
    }

    static string GetVerb(PlayHistoryItem item, bool hasTarget)
    {
        switch (item)
        {
            case AttackWithFollowerPlayHistory _:
                return "Attacks";
            case PlaySpellPlayHistory _:
                return hasTarget ? "Cast on" : "Cast";
            case RitualPlayHistory _:
                return hasTarget ? "Invoked on" : "Invoked";
            default:
                return "Played";
        }
    }

    void SetRowChrome(bool visible, bool showDivider)
    {
        if (VerbLabel != null)
            VerbLabel.gameObject.SetActive(visible);
        if (Divider != null)
        {
            Divider.gameObject.SetActive(visible && showDivider);
            Vector3 position = Divider.transform.localPosition;
            position.z = RowDepth * 0.5f;
            Divider.transform.localPosition = position;
        }
    }

    public void Clear()
    {
        foreach (GameObject component in _playHistoryComponents)
        {
            ViewFollower viewFollower = component.GetComponent<ViewFollower>();
            if (viewFollower != null)
                viewFollower.ResetForPool();

            ViewSpell viewSpell = component.GetComponent<ViewSpell>();
            if (viewSpell != null)
                viewSpell.ResetForPool();

            if (viewFollower != null)
                viewFollower.ApplyCardMode(isFollower: true);

            Destroy(component);
        }

        _playHistoryComponents.Clear();

        foreach (Transform holder in _componentHolders)
            HolderPool.Release(holder);

        _componentHolders.Clear();
        PlayHistoryItem = null;
        IsHeader = false;
    }

    void AddComponent(PlayHistoryComponent componentData, float x, float z)
    {
        ViewPlayHistoryHandler handler = Controller.Instance?.ViewPlayHistoryHandler;
        if (handler == null)
        {
            Debug.LogError("ViewPlayHistoryItemNew: ViewPlayHistoryHandler is not available.");
            return;
        }

        PlayHistoryComponentType componentType = componentData.GetComponentType();
        GameObject componentPrefab = handler.GetComponentPrefab(componentType);
        if (componentPrefab == null)
            return;

        Transform holder = HolderPool.Get();
        holder.SetParent(ComponentParent, false);
        holder.localPosition = new Vector3(x, 0f, z);
        holder.localRotation = Quaternion.identity;
        holder.localScale = Vector3.one;
        _componentHolders.Add(holder);

        // Squash raised objects (altar, portrait frame) along the camera's view axis, so their sides don't show.
        if (componentType == PlayHistoryComponentType.Ritual || componentType == PlayHistoryComponentType.Player)
            holder.localScale = new Vector3(1f, DepthFlatten, 1f);

        GameObject componentObject = Instantiate(componentPrefab, holder);
        componentObject.transform.localPosition = Vector3.zero;
        componentObject.transform.localRotation = FlatDisplayRotation;

        switch (componentType)
        {
            case PlayHistoryComponentType.Player:
                if (componentData is PlayerPlayHistoryComponent playerComponent)
                    ConfigurePortrait(componentObject, playerComponent);
                break;
            case PlayHistoryComponentType.Follower:
                if (componentData is FollowerPlayHistoryComponent followerComponent)
                    ConfigureCardComponent(componentObject, followerComponent.Follower);
                break;
            case PlayHistoryComponentType.Spell:
                if (componentData is SpellPlayHistoryComponent spellComponent)
                    ConfigureCardComponent(componentObject, spellComponent.Spell);
                break;
            case PlayHistoryComponentType.Ritual:
                ViewRitual viewRitual = componentObject.GetComponent<ViewRitual>();
                if (viewRitual != null && componentData is RitualPlayHistoryComponent ritualComponent)
                {
                    viewRitual.Init(ritualComponent.Ritual, clickable: false);
                    viewRitual.SetDisplayScale(Vector3.one * RitualScale);
                    // The altar's own spotlight would light up the rows around it.
                    foreach (Light altarLight in componentObject.GetComponentsInChildren<Light>(true))
                        altarLight.enabled = false;
                }
                break;
        }

        _playHistoryComponents.Add(componentObject);
    }

    void ConfigurePortrait(GameObject portraitObject, PlayerPlayHistoryComponent playerComponent)
    {
        ViewPlayerPortrait viewPlayerPortrait = portraitObject.GetComponent<ViewPlayerPortrait>();
        if (viewPlayerPortrait == null)
            return;

        viewPlayerPortrait.Load(playerComponent.Player);
        viewPlayerPortrait.SetHealth(playerComponent.Health);
        viewPlayerPortrait.SetHealthVisible(playerComponent.ShowHealth);
        if (viewPlayerPortrait.HeartStringHolder != null)
            viewPlayerPortrait.HeartStringHolder.SetActive(false);
        if (playerComponent.Player.IsHuman && viewPlayerPortrait.PortraitRenderer != null)
            viewPlayerPortrait.PortraitRenderer.transform.localScale = Vector3.one * HumanPortraitArtScale;

        // The board portrait leans back toward the board camera. This camera looks straight down, so undo the lean
        // or the frame reads as a trapezoid.
        Transform leaning = viewPlayerPortrait.PortraitRenderer != null
            ? viewPlayerPortrait.PortraitRenderer.transform.parent
            : null;
        if (leaning != null)
            portraitObject.transform.localRotation = FlatDisplayRotation * Quaternion.Inverse(leaning.localRotation);

        portraitObject.transform.localScale = Vector3.one * PortraitScale;
    }

    void ConfigureCardComponent(GameObject cardObject, Card card)
    {
        ViewCard viewCard = ViewPlayHistoryHandler.GetViewCardComponent(cardObject, card);
        if (viewCard == null)
            return;

        viewCard.Load(card);
        viewCard.SetDescriptiveMode(true);
        if (viewCard.CardCollider != null)
            viewCard.CardCollider.enabled = false;

        cardObject.transform.localScale = new Vector3(CardScale, CardScale, 1f);
    }

    static Transform CreateHolder()
    {
        GameObject holderObject = new GameObject("ComponentHolder");
        return holderObject.transform;
    }

    static void OnHolderGet(Transform holder)
    {
        holder.gameObject.SetActive(true);
    }

    static void OnHolderRelease(Transform holder)
    {
        holder.SetParent(null, false);
        holder.gameObject.SetActive(false);
    }
}
