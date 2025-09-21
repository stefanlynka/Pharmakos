using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Video;
using UnityEngine.XR;
using static ProgressionHandler;

public class View : MonoBehaviour
{
    public static View Instance;

    public AnimationHandler AnimationHandler;

    public static GameObject FollowerCardPrefab;
    public static GameObject SpellCardPrefab;
    private static ObjectPool<GameObject> followerPool = new ObjectPool<GameObject>(CreateFollowerCard, OnCardGet, OnCardRelease, null, false);
    private static ObjectPool<GameObject> spellPool = new ObjectPool<GameObject>(CreateCard, OnCardGet, OnCardRelease, null, false);

    public static GameObject OfferingPrefab;
    private static ObjectPool<GameObject> offeringPool = new ObjectPool<GameObject>(CreateOffering, OnOfferingGet, OnOfferingRelease, null, false);

    public Dictionary<Card, ViewCard> CardMap = new Dictionary<Card, ViewCard>();


    public ViewPlayer Player1;
    public ViewPlayer Player2;

    public SelectionHandler SelectionHandler;
    public SequenceHandler SequenceHandler;
    //public TweenManager TweenManager;

    public ViewTarget CurrentHover { get { return SelectionHandler.CurrentHover; } }

    public SpriteRenderer BackgroundRenderer;
    public Dictionary<DeckName, string> backgroundNamesByDeckName = new Dictionary<DeckName, string>()
    {
        { DeckName.TestEnemy, "Images/Backgrounds/Labyrinth" },
        { DeckName.Cyclops, "Images/Backgrounds/Cyclops" },
        { DeckName.Labyrinth, "Images/Backgrounds/Labyrinth" },
        { DeckName.Bacchanalia, "Images/Backgrounds/Bacchanalia" },
        { DeckName.Caves, "Images/Backgrounds/Cave" },
        { DeckName.Hunt, "Images/Backgrounds/Hunt" },
        { DeckName.Troy, "Images/Backgrounds/Troy" },
        { DeckName.Trials, "Images/Backgrounds/Trials" },
        { DeckName.SeasideCliffs, "Images/Backgrounds/Cliffs" },
        { DeckName.Delphi, "Images/Backgrounds/Temple" },
        { DeckName.Underworld, "Images/Backgrounds/Underworld" }
    };





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


        FollowerCardPrefab = Resources.Load<GameObject>("Prefabs/Cards/Follower2");
        SpellCardPrefab = Resources.Load<GameObject>("Prefabs/Cards/Spell2");

        OfferingPrefab = Resources.Load<GameObject>("Prefabs/Offerings/Offering");

        SelectionHandler = new SelectionHandler();

        SequenceHandler = new SequenceHandler();
        //TweenManager = new TweenManager();
    }

    public void Setup()
    {
        SelectionHandler.Setup();

        Player1.Load(Controller.Instance.Player1);
        Player2.Load(Controller.Instance.Player2);

        LoadBackground();

        AnimationHandler = new AnimationHandler();
    }
    public void Clear()
    {
        Player1.Clear();
        Player2.Clear();

        SelectionHandler.Clear();
        // TODO: Check if everything in CardMap is getting cleared up
        List<Card> cardsInCardMap = new List<Card>();
        foreach (KeyValuePair<Card, ViewCard> kvp in CardMap)
        {
            cardsInCardMap.Add(kvp.Key);
        }
        foreach (Card card in cardsInCardMap)
        {
            RemoveCard(card);
        }
        CardMap.Clear();
    }

    public void DoUpdate()
    {
        SelectionHandler.UpdateSelections();
        Player1.UpdatePlayer();
        Player2.UpdatePlayer();

        SequenceHandler.Update();
        //TweenManager.Update();
        AnimationHandler.UpdateAnimations();
    }

    private void LoadBackground()
    {
        if (!backgroundNamesByDeckName.ContainsKey(Controller.Instance.ProgressionHandler.CurrentEnemy))
        {
            Debug.LogError("No background found for current enemy: " + Controller.Instance.ProgressionHandler.CurrentEnemy);
            return;
        }
        string backgroundName = backgroundNamesByDeckName[Controller.Instance.ProgressionHandler.CurrentEnemy];

        BackgroundRenderer.sprite = Resources.Load<Sprite>(backgroundName);
    }
    

    public ViewCard MakeNewViewCard(Card card, bool addToCardMap = true)
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
            viewCard.SetHighlight(false);
            if (addToCardMap) CardMap[card] = viewCard;

            viewCard.transform.localScale = new Vector3(1, 1, 1);

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
    }
    public void RemoveViewCard(ViewCard viewCard)
    {
        if (viewCard == null)
        {
            Debug.LogError("Hm");
            return;
        }
        
        // Remove it from hand
        ViewHandHandler viewHandHandler = viewCard.Card.Owner.IsHuman ? Player1.HandHandler : Player2.HandHandler;
        ViewBattleRow viewBattleRow = viewCard.Card.Owner.IsHuman ? Player1.BattleRow : Player2.BattleRow;
        viewHandHandler.RemoveCard(viewCard);

        // Remove from battlerow and release
        ViewFollower viewFollower = viewCard as ViewFollower;
        if (viewFollower != null)
        {
            viewBattleRow.Followers.Remove(viewFollower);
            if (viewCard.gameObject != null)
            {
                viewCard.gameObject.transform.SetParent(null);
                followerPool.Release(viewCard.gameObject);
            }
        }
        ViewSpell viewSpell = viewCard as ViewSpell;
        if (viewSpell != null)
        {
            if (viewCard.gameObject != null)
            {
                viewCard.gameObject.transform.SetParent(null);
                spellPool.Release(viewCard.gameObject);
            }
        }

        CardMap.Remove(viewCard.Card);
    }

    public void ReleaseCard(ViewCard viewCard)
    {
        viewCard.transform.localScale = new Vector3(1, 1, 1);

        ViewFollower viewFollower = viewCard as ViewFollower;
        if (viewFollower != null)
        {
            followerPool.Release(viewCard.gameObject);
        }
        ViewSpell viewSpell = viewCard as ViewSpell;
        if (viewSpell != null)
        {
            spellPool.Release(viewCard.gameObject);
        }
    }

    public bool TryGetViewCard(Card card, out ViewCard viewCard)
    {
        if (CardMap.ContainsKey(card))
        {
            viewCard = CardMap[card];
            return true;
        }

        viewCard = null;
        return false;
    }
    public bool TryGetViewFollower(Card card, out ViewFollower viewFollower)
    {
        if (!TryGetViewCard(card, out ViewCard viewCard))
        {
            viewFollower = null;
            return false;
        }

        viewFollower = viewCard as ViewFollower;

        return viewFollower != null;
    }

    public ViewPlayer GetViewPlayer(Player player)
    {
        return Player1.Player == player ? Player1 : Player2;
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


    public GameObject MakeNewOffering(OfferingType offeringType)
    {
        GameObject newOffering = offeringPool.Get();
        if (newOffering == null)
        {
            Debug.LogError("Offering creation failed");
            return null;
        }

        SpriteRenderer spriteRenderer = newOffering.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("Offering prefab does not have a SpriteRenderer component");
            return null;
        }

        spriteRenderer.sprite = OfferingHandler.Instance.GetOfferingSprite(offeringType);
        return newOffering;
    }
    public void RemoveOffering(GameObject offering)
    {
        offeringPool.Release(offering);
    }

    private static GameObject CreateOffering()
    {
        if (OfferingPrefab != null)
        {
            GameObject card = Instantiate(OfferingPrefab);
            return card;
        }

        Debug.LogError("OfferingPrefab was Null when trying to instantiate new Card");
        return null;
    }
    private static void OnOfferingGet(GameObject gameObject)
    {
        gameObject.SetActive(true);
    }
    private static void OnOfferingRelease(GameObject gameObject)
    {
        gameObject.SetActive(false);
    }

    public void DrawCard(Card card)
    {
        //Debug.LogError(card.Owner.GetName() + " draws: " + card.GetName());
        ViewCard viewCard = MakeNewViewCard(card);

        if (card == null || card.Owner == null)
        {
            Debug.LogError("oops");
        }

        if (viewCard != null)
        {
            ViewHandHandler handHandler = card.Owner.IsHuman ? Player1.HandHandler : Player2.HandHandler;
            handHandler.MoveCardToHand(viewCard);
        }
    }


    public void DiscardCard(Card card)
    {
        RemoveCard(card);
    }


    public void HighlightTargets(List<ITarget> targets)
    {
        Player1.SetHighlight(targets.Contains(Controller.Instance.Player1));
        Player2.SetHighlight(targets.Contains(Controller.Instance.Player2));

        Player1.BattleRow.HighlightTargets(targets);
        Player2.BattleRow.HighlightTargets(targets);
    }

    public ViewTarget GetViewTargetByID(int targetID)
    {
        // Player
        if (Player1.Player.ITargetID == targetID) return Player1;
        if (Player2.Player.ITargetID == targetID) return Player2;
        // In BattleRow
        ViewFollower follower = Player1.BattleRow.GetViewFollowerByID(targetID);
        if (follower != null) return follower;
        follower = Player2.BattleRow.GetViewFollowerByID(targetID);
        if (follower != null) return follower;
        // In Hand
        ViewCard viewCard = Player1.HandHandler.GetViewCardByITargetID(targetID);
        if (viewCard != null) return viewCard;
        viewCard = Player2.HandHandler.GetViewCardByITargetID(targetID);
        if (viewCard != null) return viewCard;
        // Rituals
        if (Player1.ViewMinorRitual.Ritual != null && Player1.ViewMinorRitual.Ritual.ID == targetID) return Player1.ViewMinorRitual;
        if (Player1.ViewMajorRitual.Ritual != null && Player1.ViewMajorRitual.Ritual.ID == targetID) return Player1.ViewMajorRitual;
        if (Player2.ViewMinorRitual.Ritual != null && Player2.ViewMinorRitual.Ritual.ID == targetID) return Player2.ViewMinorRitual;
        if (Player2.ViewMajorRitual.Ritual != null && Player2.ViewMajorRitual.Ritual.ID == targetID) return Player2.ViewMajorRitual;

        return null;
    }

    public void UpdateResources(Player player)
    {
        ViewPlayer viewPlayer = player == Player1.Player ? Player1 : Player2;
        viewPlayer.ViewResources.RefreshResources();
    }

    public void MoveFollowerToBattleRow(Follower follower, int index = -1)
    {
        RemoveCard(follower);
        ViewFollower viewFollower = (ViewFollower)MakeNewViewCard(follower);
        ViewBattleRow battleRow = follower.Owner.IsHuman ? Player1.BattleRow : Player2.BattleRow;
        if (index == -1) index = battleRow.Followers.Count;
        battleRow.AddFollower(viewFollower, index);
        CardMap[follower] = viewFollower;
    }

    public void UpdatePlayerBuffs()
    {
        Player1.ClearBuffs();
        Player2.ClearBuffs();

        foreach (PlayerEffect playerEffect in Player1.Player.PlayerEffects)
        {
            ViewPlayer effectTarget = Player1;
            if (playerEffect is RitualPlayerEffect ritualPlayerEffect)
            {
                effectTarget = ritualPlayerEffect.Target == Player1.Player ? Player1 : Player2;
            }
            effectTarget.AddBuff(playerEffect);
        }

        foreach (PlayerEffect playerEffect in Player2.Player.PlayerEffects)
        {
            ViewPlayer effectTarget = Player2;
            if (playerEffect is RitualPlayerEffect ritualPlayerEffect)
            {
                effectTarget = ritualPlayerEffect.Target == Player1.Player ? Player1 : Player2;
            }
            effectTarget.AddBuff(playerEffect);
        }
    }
}
