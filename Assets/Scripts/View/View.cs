using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Video;
using UnityEngine.XR;

public class View : MonoBehaviour
{
    public static View Instance;

    public static GameObject FollowerCardPrefab;
    public static GameObject SpellCardPrefab;
    private static ObjectPool<GameObject> followerPool = new ObjectPool<GameObject>(CreateFollowerCard, OnCardGet, OnCardRelease);
    private static ObjectPool<GameObject> spellPool = new ObjectPool<GameObject>(CreateCard, OnCardGet, OnCardRelease);

    public ViewPlayer Player1;
    public ViewPlayer Player2;
    public ViewHandHandler PlayerHand;
    public ViewHandHandler AIHand;

    public ViewBattleRow PlayerBattleRow;
    public ViewBattleRow AIBattleRow;

    public SelectionHandler SelectionHandler;

    public ViewTarget CurrentHover { get { return SelectionHandler.CurrentHover; } }


    public int target = 60;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = target;


        FollowerCardPrefab = Resources.Load<GameObject>("Prefabs/Cards/Follower");
        SpellCardPrefab = Resources.Load<GameObject>("Prefabs/Cards/Spell");

        SelectionHandler = new SelectionHandler();
    }

    public void Setup()
    {
        SelectionHandler.Setup();

        Player1.Load(Controller.Instance.Player1);
        Player2.Load(Controller.Instance.Player2);

        Player1.ViewResources.Init(Player1.Player);
        Player2.ViewResources.Init(Player2.Player);

        PlayerBattleRow.Setup(Controller.Instance.Player1.BattleRow);
        AIBattleRow.Setup(Controller.Instance.Player2.BattleRow);
    }

    public void DoUpdate()
    {
        SelectionHandler.UpdateSelections();
        PlayerHand.UpdateHand();
        AIHand.UpdateHand();

        PlayerBattleRow.UpdateRow();
        AIBattleRow.UpdateRow();
    }

    


    private static GameObject CreateFollowerCard()
    {
        if (FollowerCardPrefab != null)
        {
            GameObject card = Instantiate(FollowerCardPrefab);
            return card;
        }
        Debug.LogError("FollowerCardPrefab was Null when trying to instantiate new Card");
        return null;
    }
    private static GameObject CreateCard()
    {
        if (SpellCardPrefab != null)
        {
            GameObject card = Instantiate(SpellCardPrefab);
            return card;
        }
        Debug.LogError("SpellCardPrefab was Null when trying to instantiate new Card");
        return null;
    }
    private static void OnCardGet(GameObject gameObject)
    {
        gameObject.SetActive(true);
    }
    private static void OnCardRelease(GameObject gameObject)
    {
        gameObject.SetActive(false);
    }

    public void DrawCard(Player player, Card card)
    {
        GameObject newCard = null;
        //ViewCard viewCard = null;
        if (card is Follower) 
        {
            newCard = followerPool.Get();
        }
        else if (card is Spell)
        {
            newCard = spellPool.Get();
        }

        if (newCard.TryGetComponent(out ViewCard viewCard))
        {
            ViewHandHandler handHandler = player == Controller.Instance.Player1 ? PlayerHand : AIHand;
            handHandler.AddCard(viewCard, card);
        }
    }
    public void DiscardCard(Player player, Card card)
    {
        ViewHandHandler viewHandHandler = player.IsHuman ? PlayerHand : AIHand;
        foreach (ViewCard cardInHand in viewHandHandler.ViewCards)
        {
            if (cardInHand.Card != card) continue;

            viewHandHandler.ViewCards.Remove(cardInHand);
            if (cardInHand.Card is Follower) followerPool.Release(cardInHand.gameObject);
            else if (cardInHand.Card is Spell) spellPool.Release(cardInHand.gameObject);
            break;
        }
    }

    public void ReleaseCard(ViewCard viewCard)
    {
        if (viewCard.Card is Follower) followerPool.Release(viewCard.gameObject);
        else if (viewCard.Card is Spell) spellPool.Release(viewCard.gameObject);

    }


    public void DiscardHand(Player player)
    {
        ViewHandHandler viewHandHandler = player.IsHuman ? PlayerHand : AIHand;
        foreach (ViewCard cardInHand in viewHandHandler.ViewCards)
        {
            if (cardInHand.Card is Follower) followerPool.Release(cardInHand.gameObject);
            else if (cardInHand.Card is Spell) spellPool.Release(cardInHand.gameObject);
        }

        viewHandHandler.ViewCards.Clear();
    }

    public void HighlightTargets(List<ITarget> targets)
    {
        Player1.SetHighlight(targets.Contains(Controller.Instance.Player1));
        Player2.SetHighlight(targets.Contains(Controller.Instance.Player2));

        PlayerBattleRow.HighlightTargets(targets);
        AIBattleRow.HighlightTargets(targets);
    }

    public void UpdateResources(Player player)
    {
        ViewPlayer viewPlayer = player == Player1.Player ? Player1 : Player2;
        viewPlayer.ViewResources.RefreshResources();
    }
}
