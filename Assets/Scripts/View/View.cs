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
    private static GameObject RitualAnimationPrefab;

    public Dictionary<int, ViewCard> CardMap = new Dictionary<int, ViewCard>();


    public ViewPlayer Player1;
    public ViewPlayer Player2;


    public SelectionHandler SelectionHandler;
    public SequenceHandler SequenceHandler;
    //public TweenManager TweenManager;

    public RectTransform PlayerTurnBanner;
    public RectTransform AITurnBanner;

    public bool IsHumansTurn = true;
    public bool TurnIsEnding = false;
    public bool IsInteractible { get { return IsHumansTurn && !TurnIsEnding; } }
    public ViewTarget CurrentHover { get { return SelectionHandler.CurrentHover; } }

    public SpriteRenderer BackgroundRenderer;
    public Dictionary<DeckName, string> backgroundNamesByDeckName = new Dictionary<DeckName, string>()
    {
        { DeckName.TestEnemy, "Images/Backgrounds/Underworld" },
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
        RitualAnimationPrefab = Resources.Load<GameObject>("Prefabs/Animations/MagicCircle2");

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
        Dictionary<int, ViewCard> tempCardMap = new Dictionary<int, ViewCard>(CardMap);

        foreach (KeyValuePair<int, ViewCard> kvp in tempCardMap)
        {
            RemoveViewCard(kvp.Value);
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
        if (CardMap.ContainsKey(card.ID))
        {
            Debug.LogError("FUCK");
        }
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
            if (addToCardMap) CardMap[card.ID] = viewCard;

            return viewCard;
        }

        Debug.LogError("Failed to make ViewCard for: " + card.GetType().Name);
        return null;
    }
    public void RemoveCard(int id)
    {
        if (!CardMap.ContainsKey(id))
        {
            Debug.LogError("ViewCard: " + id + " not found");
            return;
        }

        ViewCard viewCard = CardMap[id];
        RemoveViewCard(viewCard);
    }

    public void RemoveViewCard(ViewCard viewCard)
    {
        if (viewCard == null)
        {
            Debug.LogError("Tried to remove null ViewCard");
            return;
        }
        if (viewCard.Card == null)
        {
            Debug.LogError("Removing ViewCard without Card");
            ReleaseCard(viewCard);
            return;
        }

        int cardID = viewCard.Card.ID;

        // Remove it from hand
        ViewBattleRow viewBattleRow = null;
        if (viewCard.Card.Owner != null)
        {
            ViewHandHandler viewHandHandler = viewCard.Card.Owner.IsHuman ? Player1.HandHandler : Player2.HandHandler;
            viewBattleRow = viewCard.Card.Owner.IsHuman ? Player1.BattleRow : Player2.BattleRow;
            viewHandHandler.RemoveCard(viewCard);
        }

        // If it's a Follower, remove it from the battlerow and release
        ViewFollower viewFollower = viewCard as ViewFollower;
        if (viewFollower != null)
        {
            if (viewBattleRow != null)
            {
                viewBattleRow.TryRemoveFollower(viewFollower);
                //viewBattleRow.Followers.Remove(viewFollower);
            }

            ReleaseCard(viewCard);
        }
        // Or if it's a Spell, just release it
        ViewSpell viewSpell = viewCard as ViewSpell;
        if (viewSpell != null)
        {
            ReleaseCard(viewCard);
        }

        CardMap.Remove(cardID);
    }

    public void ReleaseCard(ViewCard viewCard)
    {
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
        if (CardMap.ContainsKey(card.ID))
        {
            viewCard = CardMap[card.ID];
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
    private static void OnCardGet(GameObject cardObject)
    {
        cardObject.SetActive(true);
        cardObject.transform.SetParent(null);
        cardObject.transform.localScale = new Vector3(1, 1, 1);
    }
    private static void OnCardRelease(GameObject cardObject)
    {
        cardObject.transform.localScale = new Vector3(1, 1, 1);
        cardObject.transform.SetParent(null);
        
        // Clear the ViewCard's card reference to prevent cross-contamination
        ViewCard viewCard = cardObject.GetComponent<ViewCard>();
        if (viewCard != null)
        {
            viewCard.Card = null;
        }

        cardObject.SetActive(false);
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

    public GameObject CreateRitualAnimation()
    {
        GameObject animationObject = Instantiate(RitualAnimationPrefab);
        if (animationObject == null) Debug.LogError("RitualAnimationPrefab was Null when trying to instantiate new RitualAnimation");

        return animationObject;
    }

    public void DrawCard(Card card)
    {
        //Debug.LogError(card.Owner.GetName() + " draws: " + card.GetName());
        ViewCard viewCard = MakeNewViewCard(card);

        if (card == null || card.Owner == null)
        {
            Debug.LogError("Failed to make and draw ViewCard");
        }

        if (viewCard != null)
        {
            ViewHandHandler handHandler = card.Owner.IsHuman ? Player1.HandHandler : Player2.HandHandler;
            handHandler.MoveCardToHand(viewCard);
        }
    }


    public void DiscardCard(Card card)
    {
        RemoveCard(card.ID);
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
        // If we can't find a ViewCard for this Follower
        if (!TryGetViewCard(follower, out ViewCard viewCard))
        {
            // Make a new ViewCard
            viewCard = MakeNewViewCard(follower);
        }

        ViewFollower viewFollower = viewCard as ViewFollower;
        if (viewFollower == null)
        {
            Debug.LogError("Didn't find ViewFollower");
            return;
        }

        //View.Instance.Player1.BattleRow.TryRemoveFollower(viewFollower);
        //View.Instance.Player2.BattleRow.TryRemoveFollower(viewFollower);

        //ViewFollower viewFollower = (ViewFollower)MakeNewViewCard(follower);
        ViewBattleRow battleRow = follower.Owner.IsHuman ? Player1.BattleRow : Player2.BattleRow;
        battleRow.AddFollower(viewFollower, index);

        viewFollower.SetDescriptiveMode(false);

        //CardMap[follower.ID] = viewFollower;
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
