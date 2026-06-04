using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ViewPlayHistoryHandler : MonoBehaviour
{
    public GameObject ContentHolder;
    public static GameObject PlayHistoryItemPrefab;
    private GameObject PortraitComponentPrefab;
    private GameObject CardComponentPrefab;
    private GameObject TargetComponentPrefab;
    private GameObject AttackComponentPrefab;
    private GameObject RitualComponentPrefab;

    private static ObjectPool<GameObject> itemPool = new ObjectPool<GameObject>(CreatePlayHistoryItem, OnItemGet, OnItemRelease, null, false);

    public List<PlayHistoryItem> PlayHistoryItems = new List<PlayHistoryItem>();
    public Dictionary<PlayHistoryItem, ViewPlayHistoryItem> ViewPlayHistoryItems = new Dictionary<PlayHistoryItem, ViewPlayHistoryItem>();


    private void Awake()
    {
        PlayHistoryItemPrefab = Resources.Load<GameObject>("Prefabs/UI/PlayHistory/UICardHolder");
        PortraitComponentPrefab = Resources.Load<GameObject>("Prefabs/View/ViewPlayerPortraitBig"); 
        CardComponentPrefab = Resources.Load<GameObject>("Prefabs/Cards/CardView");
        TargetComponentPrefab = Resources.Load<GameObject>("Prefabs/UI/PlayHistory/Components/TargetComponent");
        AttackComponentPrefab = Resources.Load<GameObject>("Prefabs/UI/PlayHistory/Components/AttackComponent");
        RitualComponentPrefab = Resources.Load<GameObject>("Prefabs/View/ViewRitual");
    }

    public void Load(List<PlayHistoryItem> playHistoryItems)
    {
        Clear();

        PlayHistoryItems.AddRange(playHistoryItems);

        foreach (PlayHistoryItem playHistoryItem in PlayHistoryItems)
        {
            GameObject newObject = itemPool.Get();
            ViewPlayHistoryItem viewPlayHistoryItem = newObject.GetComponent<ViewPlayHistoryItem>();
            if (viewPlayHistoryItem != null) ViewPlayHistoryItems[playHistoryItem] = viewPlayHistoryItem;

            newObject.transform.SetParent(ContentHolder.transform);
            viewPlayHistoryItem.Load(playHistoryItem);
        }
    }
    public void Exit()
    {
        Clear();
    }
    private void Clear()
    {
        foreach (PlayHistoryItem playHistoryItem in PlayHistoryItems)
        {
            if (!ViewPlayHistoryItems.ContainsKey(playHistoryItem)) continue;

            ViewPlayHistoryItem viewPlayHistoryItem = ViewPlayHistoryItems[playHistoryItem];
            itemPool.Release(viewPlayHistoryItem.gameObject);
        }

        PlayHistoryItems.Clear();
        ViewPlayHistoryItems.Clear();
    }

    public GameObject GetComponentPrefab(PlayHistoryComponentType componentType)
    {
        switch (componentType)
        {
            case PlayHistoryComponentType.Player:
                return PortraitComponentPrefab;
            case PlayHistoryComponentType.Follower:
            case PlayHistoryComponentType.Spell:
                return CardComponentPrefab;
            case PlayHistoryComponentType.Target:
                return TargetComponentPrefab;
            case PlayHistoryComponentType.Attack:
                return AttackComponentPrefab;
            case PlayHistoryComponentType.Ritual:
                return RitualComponentPrefab;
        }
        return null;
    }

    public static ViewCard GetViewCardComponent(GameObject cardObject, Card card)
    {
        if (card is Follower)
            return cardObject.GetComponent<ViewFollower>();

        if (card is Spell)
            return cardObject.GetComponent<ViewSpell>();

        return null;
    }

    private static GameObject CreatePlayHistoryItem()
    {
        if (PlayHistoryItemPrefab != null)
        {
            GameObject card = Instantiate(PlayHistoryItemPrefab);
            return card;
        }
        Debug.LogError("PlayHistoryItemPrefab was Null when trying to instantiate new Card");
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

        ViewPlayHistoryItem viewItem = item.GetComponent<ViewPlayHistoryItem>();
        if (viewItem != null)
            viewItem.Clear();

        item.SetActive(false);
    }
}
