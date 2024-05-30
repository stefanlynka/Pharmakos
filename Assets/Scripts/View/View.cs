using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Video;
using UnityEngine.XR;

public class View : MonoBehaviour
{
    public static View Instance;

    public AnimationHandler AnimationHandler;

    public static GameObject FollowerCardPrefab;
    public static GameObject SpellCardPrefab;
    private static ObjectPool<GameObject> followerPool = new ObjectPool<GameObject>(CreateFollowerCard, OnCardGet, OnCardRelease);
    private static ObjectPool<GameObject> spellPool = new ObjectPool<GameObject>(CreateCard, OnCardGet, OnCardRelease);

    public Dictionary<Card, ViewCard> CardMap = new Dictionary<Card, ViewCard>();


    public ViewPlayer Player1;
    public ViewPlayer Player2;



    public SelectionHandler SelectionHandler;

    public ViewTarget CurrentHover { get { return SelectionHandler.CurrentHover; } }


    //public int target = 60;


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

        //QualitySettings.vSyncCount = 0;
        //Application.targetFrameRate = target;


        FollowerCardPrefab = Resources.Load<GameObject>("Prefabs/Cards/Follower");
        SpellCardPrefab = Resources.Load<GameObject>("Prefabs/Cards/Spell");

        SelectionHandler = new SelectionHandler();
    }

    public void Setup()
    {
        SelectionHandler.Setup();

        Player1.Load(Controller.Instance.Player1);
        Player2.Load(Controller.Instance.Player2);

        AnimationHandler = new AnimationHandler();
    }

    public void DoUpdate()
    {
        SelectionHandler.UpdateSelections();
        Player1.UpdatePlayer();
        Player2.UpdatePlayer();
        AnimationHandler.UpdateAnimations();
    }

    public ViewCard MakeNewViewCard(Card card)
    {
        GameObject newCard = null;

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
            viewCard.Load(card);
            CardMap[card] = viewCard;

            return viewCard;
        }

        Debug.LogError("Failed to make ViewCard for: " + card.GetType().Name);
        return null;
    }
    public void RemoveCard(Card card)
    {
        if (!CardMap.ContainsKey(card)) return;

        ViewCard viewCard = CardMap[card];
        RemoveViewCard(viewCard);
        CardMap.Remove(card);
    }
    public void RemoveViewCard(ViewCard viewCard)
    {
        //Debug.LogError("Remove View Card");
        // Remove it from hand
        ViewHandHandler viewHandHandler = viewCard.Card.Owner.IsHuman ? Player1.HandHandler : Player2.HandHandler;
        ViewBattleRow viewBattleRow = viewCard.Card.Owner.IsHuman ? Player1.BattleRow : Player2.BattleRow;
        viewHandHandler.ViewCards.Remove(viewCard);

        // Remove from battlerow and release
        ViewFollower viewFollower = viewCard as ViewFollower;
        if (viewFollower != null)
        {
            viewBattleRow.Followers.Remove(viewFollower);
            followerPool.Release(viewCard.gameObject);
        }
        ViewSpell viewSpell = viewCard as ViewSpell;
        if (viewSpell != null)
        {
            spellPool.Release(viewCard.gameObject);
        }
    }
    //public void ReleaseViewCard(ViewCard viewCard)
    //{
    //    if (viewCard.Card is Follower) followerPool.Release(viewCard.gameObject);
    //    else if (viewCard.Card is Spell) spellPool.Release(viewCard.gameObject);
    //}

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

    public void DrawCard(Card card)
    {
        ViewCard viewCard = MakeNewViewCard(card);

        if (viewCard != null)
        {
            ViewHandHandler handHandler = card.Owner.IsHuman ? Player1.HandHandler : Player2.HandHandler;
            handHandler.MoveCardToHand(viewCard);
        }
    }


    public void DiscardCard(Card card)
    {
        RemoveCard(card);

        //ViewHandHandler viewHandHandler = player.IsHuman ? PlayerHand : AIHand;
        //foreach (ViewCard cardInHand in viewHandHandler.ViewCards)
        //{
        //    if (cardInHand.Card != card) continue;

        //    viewHandHandler.ViewCards.Remove(cardInHand);
        //    if (cardInHand.Card is Follower) followerPool.Release(cardInHand.gameObject);
        //    else if (cardInHand.Card is Spell) spellPool.Release(cardInHand.gameObject);
        //    break;
        //}
    }

    


    //public void DiscardHand(Player player)
    //{
    //    ViewHandHandler viewHandHandler = player.IsHuman ? PlayerHand : AIHand;
    //    foreach (ViewCard cardInHand in viewHandHandler.ViewCards)
    //    {
    //        if (cardInHand.Card is Follower) followerPool.Release(cardInHand.gameObject);
    //        else if (cardInHand.Card is Spell) spellPool.Release(cardInHand.gameObject);
    //    }

    //    viewHandHandler.ViewCards.Clear();
    //}

    //public void UpdateHighlights()
    //{

    //}

    public void HighlightTargets(List<ITarget> targets)
    {
        Player1.SetHighlight(targets.Contains(Controller.Instance.Player1));
        Player2.SetHighlight(targets.Contains(Controller.Instance.Player2));

        Player1.BattleRow.HighlightTargets(targets);
        Player2.BattleRow.HighlightTargets(targets);
    }

    public void UpdateResources(Player player)
    {
        ViewPlayer viewPlayer = player == Player1.Player ? Player1 : Player2;
        viewPlayer.ViewResources.RefreshResources();
    }

    public void MoveFollowerToBattleRow(Follower follower, int index)
    {
        RemoveCard(follower);
        ViewFollower viewFollower = (ViewFollower)MakeNewViewCard(follower);
        ViewBattleRow battleRow = follower.Owner.IsHuman ? Player1.BattleRow : Player2.BattleRow;
        battleRow.AddFollower(viewFollower, index);
    }
}
