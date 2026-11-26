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
    [Tooltip("Centre X of the target card / portrait (the first result, when a trigger has several).")]
    public float TargetX = 11f;
    [Tooltip("Width of a card / portrait in the row, for spacing several results (a trigger hitting several followers).")]
    public float CardWidth = 6.6f;
    public float PortraitWidth = 9.8f;
    [Tooltip("Gap between results.")]
    public float ResultGap = 1.4f;
    [Tooltip("Results never reach past this X; when they would, they shrink and close up.")]
    public float ResultsMaxRight = 30f;
    public float CardScale = 1.3f;
    public float RitualScale = 0.45f;
    public float PortraitScale = 1.2f;
    [Tooltip("The player's portrait art is painted larger than enemy art; this shrinks it to sit inside the frame.")]
    public float HumanPortraitArtScale = 0.77f;
    [Tooltip("How much of their depth rituals and portraits keep. This camera looks straight down, so a raised "
        + "altar or frame beside the centre shows its sides in perspective; flattening them keeps rows clean.")]
    [Range(0.02f, 1f)] public float DepthFlatten = 0.1f;
    [Tooltip("Height in world units of a trinket's or ritual buff's icon.")]
    public float EffectIconSize = 6f;
    [Tooltip("Gap between a trinket's icon and its name underneath.")]
    public float EffectNameGap = 0.4f;
    [Tooltip("The name wraps to this width.")]
    public float EffectNameWidth = 9f;
    [Tooltip("Size of the name relative to the verb label.")]
    public float EffectNameSize = 0.85f;
    [Tooltip("Scale of a trigger's icon (the summary glyphs), matching the attack and target icon prefabs.")]
    public float TriggerIconScale = 1.7f;
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
    // What the hover popups can show for each component: its holder (for bounds) and its card, ritual or effect.
    readonly List<(Transform holder, object subject)> _hoverSubjects = new List<(Transform, object)>();
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

        // Components arrive as: source, [verb icon], results. Plays without a target have no verb icon.
        List<PlayHistoryComponent> components = playHistoryItem.GetComponents();
        PlayHistoryComponent source = components.Count > 0 ? components[0] : null;
        PlayHistoryComponent verbIcon = null;
        List<PlayHistoryComponent> results = new List<PlayHistoryComponent>();
        for (int i = 1; i < components.Count; i++)
        {
            PlayHistoryComponentType type = components[i].GetComponentType();
            if (verbIcon == null && (type == PlayHistoryComponentType.Attack
                || type == PlayHistoryComponentType.Target
                || type == PlayHistoryComponentType.TriggerIcon))
                verbIcon = components[i];
            else
                results.Add(components[i]);
        }

        if (source != null)
            AddComponent(source, SourceX, 0f, 1f);
        if (verbIcon != null)
            AddComponent(verbIcon, VerbX, VerbIconZ, 1f);

        // Results sit side by side from TargetX; if they would run past ResultsMaxRight, they shrink and close up.
        if (results.Count > 0)
        {
            float left = TargetX - GetResultWidth(results[0]) * 0.5f;
            float extent = 0f;
            for (int i = 0; i < results.Count; i++)
                extent += GetResultWidth(results[i]) + (i > 0 ? ResultGap : 0f);

            float resultScale = extent > 0f ? Mathf.Min(1f, (ResultsMaxRight - left) / extent) : 1f;
            float cursor = left;
            foreach (PlayHistoryComponent result in results)
            {
                float width = GetResultWidth(result) * resultScale;
                AddComponent(result, cursor + width * 0.5f, 0f, resultScale);
                cursor += width + ResultGap * resultScale;
            }
        }

        bool hasTarget = results.Count > 0;
        SetRowChrome(true, showDivider);
        if (VerbLabel != null)
        {
            VerbLabel.text = GetVerb(playHistoryItem, hasTarget);
            Vector3 labelPosition = VerbLabel.transform.localPosition;
            labelPosition.x = VerbX;
            labelPosition.z = verbIcon != null ? VerbLabelZ : 0f;
            VerbLabel.transform.localPosition = labelPosition;
        }
    }

    float GetResultWidth(PlayHistoryComponent component)
    {
        switch (component.GetComponentType())
        {
            case PlayHistoryComponentType.Player:
                return PortraitWidth;
            case PlayHistoryComponentType.Effect:
                return Mathf.Max(EffectIconSize, EffectNameWidth * 0.8f);
            default:
                return CardWidth;
        }
    }

    static string GetVerb(PlayHistoryItem item, bool hasTarget)
    {
        switch (item)
        {
            case TriggeredEffectPlayHistory triggered:
                return triggered.Label;
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
        _hoverSubjects.Clear();
        PlayHistoryItem = null;
        IsHeader = false;
    }

    void AddComponent(PlayHistoryComponent componentData, float x, float z, float scale)
    {
        ViewPlayHistoryHandler handler = Controller.Instance?.ViewPlayHistoryHandler;
        if (handler == null)
        {
            Debug.LogError("ViewPlayHistoryItemNew: ViewPlayHistoryHandler is not available.");
            return;
        }

        PlayHistoryComponentType componentType = componentData.GetComponentType();
        GameObject componentPrefab = handler.GetComponentPrefab(componentType);
        bool builtHere = componentType == PlayHistoryComponentType.TriggerIcon
            || componentType == PlayHistoryComponentType.Effect;
        if (componentPrefab == null && !builtHere)
            return;

        Transform holder = HolderPool.Get();
        holder.SetParent(ComponentParent, false);
        holder.localPosition = new Vector3(x, 0f, z);
        holder.localRotation = Quaternion.identity;
        holder.localScale = Vector3.one * scale;
        _componentHolders.Add(holder);

        // Squash raised objects (altar, portrait frame) along the camera's view axis, so their sides don't show.
        if (componentType == PlayHistoryComponentType.Ritual || componentType == PlayHistoryComponentType.Player)
            holder.localScale = new Vector3(scale, DepthFlatten * scale, scale);

        object hoverSubject = GetHoverSubject(componentData);
        if (hoverSubject != null)
            _hoverSubjects.Add((holder, hoverSubject));

        if (builtHere)
        {
            if (componentData is TriggerIconPlayHistoryComponent triggerIcon)
                AddIcon(holder, Resources.Load<Sprite>("Images/Icons/SummaryMono/" + triggerIcon.IconName), TriggerIconScale);
            else if (componentData is EffectPlayHistoryComponent effect)
                AddEffect(holder, effect);
            return;
        }

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
                {
                    ConfigureCardComponent(componentObject, followerComponent.Follower);
                    ApplyFollowerState(componentObject.GetComponent<ViewFollower>(), followerComponent.State);
                }
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

    static object GetHoverSubject(PlayHistoryComponent component)
    {
        switch (component)
        {
            case FollowerPlayHistoryComponent follower: return follower.Follower;
            case SpellPlayHistoryComponent spell: return spell.Spell;
            case RitualPlayHistoryComponent ritual: return ritual.Ritual;
            case EffectPlayHistoryComponent effect: return effect;
            default: return null;
        }
    }

    /// <summary>
    /// The card, ritual or effect (EffectPlayHistoryComponent) drawn at <paramref name="worldPoint"/> (on the row's
    /// plane), with its world bounds for placing the popup.
    /// </summary>
    public bool TryGetHoverSubject(Vector3 worldPoint, out object subject, out Bounds bounds)
    {
        foreach ((Transform holder, object candidate) in _hoverSubjects)
        {
            if (holder == null || !TryGetBounds(holder, out Bounds holderBounds))
                continue;

            if (worldPoint.x >= holderBounds.min.x && worldPoint.x <= holderBounds.max.x
                && worldPoint.z >= holderBounds.min.z && worldPoint.z <= holderBounds.max.z)
            {
                subject = candidate;
                bounds = holderBounds;
                return true;
            }
        }

        subject = null;
        bounds = default;
        return false;
    }

    static bool TryGetBounds(Transform root, out Bounds bounds)
    {
        bounds = default;
        bool found = false;
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
        {
            if (!renderer.enabled || renderer is ParticleSystemRenderer)
                continue;

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }
        return found;
    }

    GameObject AddIcon(Transform holder, Sprite sprite, float scale)
    {
        if (sprite == null)
            return null;

        GameObject iconObject = new GameObject("Icon");
        iconObject.transform.SetParent(holder, false);
        iconObject.transform.localRotation = FlatDisplayRotation;
        iconObject.transform.localScale = Vector3.one * scale;
        iconObject.AddComponent<SpriteRenderer>().sprite = sprite;
        _playHistoryComponents.Add(iconObject);
        return iconObject;
    }

    // A trinket's or ritual buff's icon at about card height, with its name underneath in the verb label's style.
    void AddEffect(Transform holder, EffectPlayHistoryComponent effect)
    {
        float spriteHeight = effect.Icon != null ? effect.Icon.bounds.size.y : 0f;
        if (spriteHeight > 0f)
            AddIcon(holder, effect.Icon, EffectIconSize / spriteHeight);

        if (string.IsNullOrEmpty(effect.Name) || VerbLabel == null)
            return;

        GameObject nameObject = Instantiate(VerbLabel.gameObject, holder);
        nameObject.name = "EffectName";
        nameObject.SetActive(true);
        TextMeshPro nameText = nameObject.GetComponent<TextMeshPro>();
        nameText.text = effect.Name;
        nameText.fontSize = VerbLabel.fontSize * EffectNameSize;
        nameText.enableWordWrapping = true;
        nameText.alignment = TextAlignmentOptions.Top;
        float rectHeight = nameText.rectTransform.sizeDelta.y;
        nameText.rectTransform.sizeDelta = new Vector2(EffectNameWidth, rectHeight);
        // Hang the (possibly two-line) name from just under the icon.
        float top = -EffectIconSize * 0.5f - EffectNameGap;
        nameObject.transform.localPosition = new Vector3(0f, VerbLabel.transform.localPosition.y, top - rectHeight * 0.5f);
        _playHistoryComponents.Add(nameObject);
    }

    void ConfigurePortrait(GameObject portraitObject, PlayerPlayHistoryComponent playerComponent)
    {
        ViewPlayerPortrait viewPlayerPortrait = portraitObject.GetComponent<ViewPlayerPortrait>();
        if (viewPlayerPortrait == null)
            return;

        viewPlayerPortrait.Load(playerComponent.Player);
        viewPlayerPortrait.SetHealth(playerComponent.Health);
        viewPlayerPortrait.SetHealthVisible(playerComponent.ShowHealth);
        if (playerComponent.State != null && playerComponent.State.Damage > 0)
            viewPlayerPortrait.ShowDamageImmediate(playerComponent.State.Damage);
        if (viewPlayerPortrait.HeartStringHolder != null)
            viewPlayerPortrait.HeartStringHolder.SetActive(false);
        // The frame stands proud of the floor here, and its shadow reads as a dark smear beside it.
        foreach (Renderer frameRenderer in portraitObject.GetComponentsInChildren<Renderer>(true))
            frameRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
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

    // Stats as the entry saw them, the damage dealt in it, or the skull if the follower died in it (no damage then).
    static void ApplyFollowerState(ViewFollower viewFollower, PlayHistoryTargetState state)
    {
        if (viewFollower == null || state == null)
            return;

        viewFollower.SetStatsSnapshot(state.Attack, Mathf.Max(0, state.Health), state.MaxHealth);
        if (state.Died)
            viewFollower.SetSkullVisible(true);
        else if (state.Damage > 0)
            viewFollower.ShowDamageImmediate(state.Damage);
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
