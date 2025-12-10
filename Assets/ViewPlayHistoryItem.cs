using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Pool;

public class ViewPlayHistoryItem : MonoBehaviour
{
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
        componentHolder.transform.parent = ComponentParent.transform;
        RectTransform rectTransform = componentHolder.GetComponent<RectTransform>();
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
                ViewFollower viewFollower = componentObject.GetComponent<ViewFollower>();
                if (viewFollower != null && componentData is FollowerPlayHistoryComponent followerComponent)
                {
                    viewFollower.Load(followerComponent.Follower);
                    viewFollower.SetDescriptiveMode(true);

                    rectTransform.sizeDelta = new Vector2(12f, 7.5f);
                }
                break;
            case PlayHistoryComponentType.Spell:
                ViewSpell viewSpell = componentObject.GetComponent<ViewSpell>();
                if (viewSpell != null && componentData is SpellPlayHistoryComponent spellComponent)
                {
                    viewSpell.Load(spellComponent.Spell);
                    viewSpell.SetDescriptiveMode(true);

                    rectTransform.sizeDelta = new Vector2(12f, 7.5f);
                }
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
        componentObject.transform.parent = componentHolder.transform;
        componentObject.transform.localPosition = new Vector3(0, 0, -1);
    }

    public void Clear()
    {
        foreach (GameObject component in PlayHistoryComponents)
        {
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
