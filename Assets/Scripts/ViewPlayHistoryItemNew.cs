using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ViewPlayHistoryItemNew : MonoBehaviour
{
    const float CardScale = 1.5f;
    const float ComponentY = 0.05f;
    const float PortraitY = 1.4f;
    const float HumanPortraitScale = 0.77f;
    const float DefaultWidth = 7.5f;
    const float PortraitWithHealthWidth = 12.5f;
    const float PortraitNoHealthWidth = 15f;
    const float CardWidth = 12f;
    const float RitualWidth = 12.5f;
    static readonly Quaternion FlatDisplayRotation = Quaternion.Euler(90f, 0f, 0f);

    public Transform ComponentParent;

    public PlayHistoryItem PlayHistoryItem { get; private set; }

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

    public void Load(PlayHistoryItem playHistoryItem)
    {
        Clear();
        PlayHistoryItem = playHistoryItem;

        if (ComponentParent == null)
        {
            Debug.LogError("ViewPlayHistoryItemNew: ComponentParent is not assigned.");
            return;
        }

        var layoutEntries = new List<(PlayHistoryComponent data, PlayHistoryComponentType type, float width)>();

        layoutEntries.Add((
            new PlayerPlayHistoryComponent(PlayHistoryItem.Owner, 0, false),
            PlayHistoryComponentType.Player,
            PortraitNoHealthWidth));

        foreach (PlayHistoryComponent component in playHistoryItem.GetComponents())
        {
            PlayHistoryComponentType componentType = component.GetComponentType();
            layoutEntries.Add((component, componentType, GetComponentWidth(component, componentType)));
        }

        float totalWidth = 0f;
        foreach ((_, _, float width) in layoutEntries)
            totalWidth += width;

        float cursorX = -totalWidth * 0.5f;
        foreach ((PlayHistoryComponent data, PlayHistoryComponentType type, float width) in layoutEntries)
        {
            Transform holder = HolderPool.Get();
            holder.SetParent(ComponentParent, false);
            holder.localPosition = new Vector3(cursorX + width * 0.5f, ComponentY, 0f);
            holder.localRotation = Quaternion.identity;
            holder.localScale = Vector3.one;
            _componentHolders.Add(holder);

            AddComponent(holder, data, type);
            cursorX += width;
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
    }

    static float GetComponentWidth(PlayHistoryComponent componentData, PlayHistoryComponentType componentType)
    {
        switch (componentType)
        {
            case PlayHistoryComponentType.Player:
                if (componentData is PlayerPlayHistoryComponent playerComponent)
                    return playerComponent.ShowHealth ? PortraitWithHealthWidth : PortraitNoHealthWidth;
                return DefaultWidth;
            case PlayHistoryComponentType.Follower:
            case PlayHistoryComponentType.Spell:
                return CardWidth;
            case PlayHistoryComponentType.Ritual:
                return RitualWidth;
            default:
                return DefaultWidth;
        }
    }

    void AddComponent(Transform holder, PlayHistoryComponent componentData, PlayHistoryComponentType componentType)
    {
        ViewPlayHistoryHandler handler = Controller.Instance?.ViewPlayHistoryHandler;
        if (handler == null)
        {
            Debug.LogError("ViewPlayHistoryItemNew: ViewPlayHistoryHandler is not available.");
            return;
        }

        GameObject componentPrefab = handler.GetComponentPrefab(componentType);
        if (componentPrefab == null)
            return;

        GameObject componentObject = Instantiate(componentPrefab, holder);
        componentObject.transform.localPosition = Vector3.zero;
        componentObject.transform.localRotation =
            componentType == PlayHistoryComponentType.Ritual
                ? Quaternion.identity
                : FlatDisplayRotation;

        switch (componentType)
        {
            case PlayHistoryComponentType.Player:
                componentObject.transform.localPosition = new Vector3(0, PortraitY, 0);
                componentObject.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
                ViewPlayerPortrait viewPlayerPortrait = componentObject.GetComponent<ViewPlayerPortrait>();
                if (viewPlayerPortrait != null && componentData is PlayerPlayHistoryComponent playerComponent)
                {
                    viewPlayerPortrait.Load(playerComponent.Player);
                    viewPlayerPortrait.SetHealth(playerComponent.Health);
                    viewPlayerPortrait.SetHealthVisible(playerComponent.ShowHealth);

                    if (playerComponent.Player.IsHuman && viewPlayerPortrait.PortraitRenderer != null)
                    {
                        viewPlayerPortrait.PortraitRenderer.transform.localScale =
                            new Vector3(HumanPortraitScale, HumanPortraitScale, 1f);
                    }
                }
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
                componentObject.transform.localEulerAngles = new Vector3(90, 0, 0);
                componentObject.transform.localPosition = new Vector3(0, 0.9f, 0);
                ViewRitual viewRitual = componentObject.GetComponent<ViewRitual>();
                if (viewRitual != null && componentData is RitualPlayHistoryComponent ritualComponent)
                {
                    viewRitual.Init(ritualComponent.Ritual, clickable: false);
                    viewRitual.SetDisplayScale(new Vector3(0.5f, 0.5f, 0.5f));
                }
                break;
        }

        _playHistoryComponents.Add(componentObject);
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
