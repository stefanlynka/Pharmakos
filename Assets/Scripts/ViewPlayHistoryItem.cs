using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ViewPlayHistoryItem : MonoBehaviour
{
    const float CardScale = 1.5f;

    public GameObject ComponentParent;
    public PlayHistoryItem PlayHistoryItem;
    public List<GameObject> PlayHistoryComponents = new List<GameObject>();
    public static GameObject ComponentHolderPrefab;

    private static ObjectPool<GameObject> componentHolderPool = new ObjectPool<GameObject>(CreateComponentHolder, OnItemGet, OnItemRelease, null, false);

    private void Awake()
    {
        ComponentHolderPrefab = Resources.Load<GameObject>("Prefabs/UI/PlayHistory/UIComponentHolder");

    }
    public void Load(PlayHistoryItem playHistoryItem)
    {
        Clear();
        PlayHistoryItem = playHistoryItem;

        AddComponent(new PlayerPlayHistoryComponent(PlayHistoryItem.Owner, 0, false), PlayHistoryComponentType.Player);

        List<PlayHistoryComponent> playHistoryComponents = playHistoryItem.GetComponents();
        foreach (PlayHistoryComponent playHistoryComponent in playHistoryComponents)
        {
            PlayHistoryComponentType componentType = playHistoryComponent.GetComponentType();
            AddComponent(playHistoryComponent, componentType);
        }
    }

    private void AddComponent(PlayHistoryComponent componentData, PlayHistoryComponentType componentType)
    {
        // Create a ComponentHolder for each component (It has a RectTransform unlike the components we're creating)
        GameObject componentHolder = componentHolderPool.Get();
        componentHolder.transform.SetParent(ComponentParent.transform, false);
        RectTransform rectTransform = componentHolder.GetComponent<RectTransform>();
        if (rectTransform != null)
            rectTransform.anchoredPosition3D = new Vector3(0, 0, -1);
        if (rectTransform == null) return;
            
        rectTransform.sizeDelta = new Vector2(7.5f, 7.5f);

        // Create the Component
        GameObject componentPrefab = Controller.Instance.ViewPlayHistoryHandler.GetComponentPrefab(componentType);
        GameObject componentObject = Instantiate(componentPrefab);
        switch (componentType)
        {
            case PlayHistoryComponentType.Player:
                ViewPlayerPortrait viewPlayerPortrait = componentObject.GetComponent<ViewPlayerPortrait>();
                if (viewPlayerPortrait != null && componentData is PlayerPlayHistoryComponent playerComponent)
                {
                    viewPlayerPortrait.Load(playerComponent.Player);
                    viewPlayerPortrait.SetHealth(playerComponent.Health);
                    viewPlayerPortrait.SetHealthVisible(playerComponent.ShowHealth);
                    //componentObject.transform.localScale = new Vector3(1.5f, 1.5f);

                    if (playerComponent.ShowHealth)
                    {
                        rectTransform.sizeDelta = new Vector2(12.5f, 7.5f);
                    }
                    else
                    {
                        rectTransform.sizeDelta = new Vector2(15f, 7.5f);
                    }
                }
                break;
            case PlayHistoryComponentType.Follower:
                if (componentData is FollowerPlayHistoryComponent followerComponent)
                    ConfigureCardComponent(componentObject, followerComponent.Follower, rectTransform);
                break;
            case PlayHistoryComponentType.Spell:
                if (componentData is SpellPlayHistoryComponent spellComponent)
                    ConfigureCardComponent(componentObject, spellComponent.Spell, rectTransform);
                break;
            case PlayHistoryComponentType.Target:
                break;
            case PlayHistoryComponentType.Attack:
                break;
            case PlayHistoryComponentType.Ritual:
                ViewRitual viewRitual = componentObject.GetComponent<ViewRitual>();
                if (viewRitual != null && componentData is RitualPlayHistoryComponent ritualComponent)
                {
                    viewRitual.Init(ritualComponent.Ritual);

                    rectTransform.sizeDelta = new Vector2(12.5f, 7.5f);
                }
                break;
        }

        PlayHistoryComponents.Add(componentObject);
        componentObject.transform.SetParent(componentHolder.transform, false);
        componentObject.transform.localPosition = new Vector3(0, 0, -1);
        componentObject.transform.localRotation = Quaternion.identity;
    }

    void ConfigureCardComponent(GameObject cardObject, Card card, RectTransform holderRect)
    {
        ViewCard viewCard = ViewPlayHistoryHandler.GetViewCardComponent(cardObject, card);
        if (viewCard == null)
            return;

        viewCard.Load(card);
        viewCard.SetDescriptiveMode(true);
        if (viewCard.CardCollider != null)
            viewCard.CardCollider.enabled = false;

        cardObject.transform.localScale = new Vector3(CardScale, CardScale, 1f);
        holderRect.sizeDelta = new Vector2(12f, 7.5f);
    }

    public void Clear()
    {
        foreach (GameObject component in PlayHistoryComponents)
        {
            ViewFollower viewFollower = component.GetComponent<ViewFollower>();
            if (viewFollower != null)
                viewFollower.ResetForPool();

            ViewSpell viewSpell = component.GetComponent<ViewSpell>();
            if (viewSpell != null)
                viewSpell.ResetForPool();

            if (viewFollower != null)
                viewFollower.ApplyCardMode(isFollower: true);

            GameObject parent = component.transform.parent.gameObject;
            componentHolderPool.Release(parent);
            Destroy(component);
        }

        PlayHistoryComponents.Clear();
    }

    private static GameObject CreateComponentHolder()
    {
        if (ComponentHolderPrefab != null)
        {
            GameObject card = Instantiate(ComponentHolderPrefab);
            return card;
        }
        Debug.LogError("ComponentHolderPrefab was Null when trying to instantiate new Holder");
        return null;
    }
    private static void OnItemGet(GameObject item)
    {
        item.SetActive(true);
        item.transform.SetParent(null);
        item.transform.localScale = new Vector3(1, 1, 1);

        RectTransform rectTransform = item.GetComponent<RectTransform>();
        if (rectTransform != null)
            rectTransform.anchoredPosition3D = new Vector3(0, 0, -1);
    }

    private static void OnItemRelease(GameObject item)
    {
        item.transform.localScale = new Vector3(1, 1, 1);
        item.transform.SetParent(null);

        // Clear the ViewCard's card reference to prevent cross-contamination
        ViewPlayHistoryItem viewItem = item.GetComponent<ViewPlayHistoryItem>();

        item.SetActive(false);
    }
}
