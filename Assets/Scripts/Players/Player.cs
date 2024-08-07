using System;
using System.Collections.Generic;
using UnityEngine;

public class Player : ITarget
{
    ///
    /// Deep Copied
    ///
    public List<Card> Deck = new List<Card>();
    public List<Card> Hand = new List<Card>();
    public List<Card> Graveyard = new List<Card>();

    public BattleRow BattleRow = new BattleRow();

    public Dictionary<OfferingType, int> Offerings = new Dictionary<OfferingType, int>();

    public Dictionary<OfferingType, int> InitialOfferings = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };
    public Ritual MinorRitual;
    public Ritual MajorRitual;

    // For identifying which Player is which
    public int PlayerID = -1;

    // For identifying which ITarget is which
    public int ITargetID = -1;

    public int Health = 20;
    public int StartingHealth = 5;
    public string Name { get { return IsHuman ? "Human" : "AI"; } }
    public bool IsHuman = false;

    public int CardsPerTurn = 5;
    public int GoldPerTurn = 3;

    ///
    /// Not Deep Copied
    ///
    public GameState GameState {  get; private set; }
    public Action OnHealthChange;
    public Action OnOfferingsChange;

    public bool IsMyTurn
    {
        get { return GameState.CurrentTeamID == PlayerID; }
    }

    public int GetID()
    {
        return ITargetID;
    }
    public string GetName()
    {
        return IsHuman ? "Human" : "AI";
    }

    public Player() { }

    private Player MakeBaseCopy()
    {
        // Get the type of the calling class
        Type callingType = this.GetType();

        // Create a new instance of the calling class
        Player newCard = (Player)Activator.CreateInstance(callingType);
        return newCard;
    }

    public void Init(int playerID)
    {
        GameState.TryAssignID(this);

        PlayerID = playerID;

        Health = StartingHealth;

        LoadDeck(Deck);

        MinorRitual?.Init(this);
        MajorRitual?.Init(this);

        BattleRow.Init(this);

        Offerings = new Dictionary<OfferingType, int>(InitialOfferings);
        Offerings[OfferingType.Gold] = GoldPerTurn;
    }

    public void LoadDeck(List<Card> deck)
    {
        Deck = deck;
        foreach (Card card in Deck)
        {
            card.Init(this);
        }
        ShuffleDeck();
    }

    // index: 0:Minor 1:Major
    public void LoadRitual(Ritual ritual, int index)
    {
        if (index == 0)
        {
            MinorRitual = ritual;
            MinorRitual.Init(this);
        }
        else
        {
            MajorRitual = ritual;
            MajorRitual.Init(this);
        }
    }

    public Player DeepCopy(GameState newGameState)
    {
        Player copy = MakeBaseCopy();

        copy.AttachToGameState(newGameState);

        copy.Health = Health;
        copy.IsHuman = IsHuman;
        copy.CardsPerTurn = CardsPerTurn;
        copy.GoldPerTurn = GoldPerTurn;

        copy.PlayerID = PlayerID;
        copy.ITargetID = ITargetID;
        copy.GameState.TargetsByID[ITargetID] = copy;

        copy.Hand = DeepCopyCardList(Hand, copy);
        copy.Deck = DeepCopyCardList(Deck, copy);
        copy.Graveyard = DeepCopyCardList(Graveyard, copy);

        copy.BattleRow = BattleRow.DeepCopy(copy);

        copy.Offerings = new Dictionary<OfferingType, int>(Offerings);
        copy.MinorRitual = MinorRitual?.DeepCopy(copy);
        copy.MajorRitual = MajorRitual?.DeepCopy(copy);

        return copy;
    }

    public List<Card> DeepCopyCardList(List<Card> cards, Player newOwner)
    {
        List<Card> copy = new List<Card>();

        foreach (Card card in cards)
        {
            Card newCard = card.DeepCopy(newOwner);
            copy.Add(newCard);
        }

        return copy;
    }

    public void AttachToGameState(GameState gameState)
    {
        GameState = gameState;
    }

    public void Clear()
    {
        Deck.Clear();
        Hand.Clear();
        Graveyard.Clear();

        BattleRow.Clear();
        MinorRitual = null;
        MajorRitual = null;

        Offerings = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 0},
            { OfferingType.Bone, 0},
            { OfferingType.Crop, 0},
            { OfferingType.Scroll, 0},
        };

        OnHealthChange = null;
        OnOfferingsChange = null;
    }

    public virtual void RunUpdate()
    {

    }

    // Setup for this Player. Should only be called by GameState.EndTurn()
    public virtual void StartTurn()
    {
        DrawHand();
    }

    // Cleanup for this Player. Should only be called by GameState.EndTurn()
    public virtual void EndTurn()
    {
        if (!IsMyTurn) return;

        DiscardHand();
        Offerings[OfferingType.Gold] = GoldPerTurn;
        View.Instance.UpdateResources(this);

        foreach (Follower follower in BattleRow.Followers)
        {
            follower.DoEndOfTurnEffects();
        }
    }


    public void ShuffleDeck()
    {
        for (var i = 0; i < Deck.Count; i++)
        {
            Card temp = Deck[i];
            int randIndex = UnityEngine.Random.Range(0, Deck.Count);
            Deck[i] = Deck[randIndex];
            Deck[randIndex] = temp;
        }
    }

    public void DrawHand()
    {
        for (int i = 0; i < CardsPerTurn; i++)
        {
            DrawCard();
        }
    }
    public void DrawCard()
    {
        if (Deck.Count == 0)
        {
            if (Graveyard.Count == 0) return;

            Deck = new List<Card>(Graveyard);
            Graveyard.Clear();
        }

        Card card = Deck[0];
        Deck.RemoveAt(0);
        Hand.Add(card);
        View.Instance.DrawCard(card);
    }

    public void DiscardHand()
    {
        for (int i = Hand.Count - 1; i >= 0; i--)
        {
            Card card = Hand[i];
            DiscardCard(card);
        }
    }
    public void DiscardCard(Card card)
    {
        Hand.Remove(card);
        Graveyard.Add(card);
        View.Instance.DiscardCard(card);
    }

    public void FollowerDied(Follower follower)
    {
        BattleRow.Followers.Remove(follower);
    }
    public void PlayCard(Card card)
    {
        card.PayCosts();
        Hand.Remove(card);
    }
    public void TryPlayFollower(Follower follower, int index)
    {
        GameAction newAction = new PlayFollowerAction(follower, index);
        GameState.ActionHandler.AddAction(newAction);
        GameState.ActionHandler.StartEvaluating();
    }

    public void PlayFollower(Follower follower, int index)
    {
        // Pay costs and remove from hand
        PlayCard(follower);

        SummonFollower(follower, index);
    }

    public void TryPlaySpell(Spell spell, ITarget target)
    {
        PlayCard(spell);
        spell.Play(target);
        View.Instance.RemoveCard(spell);
    }

    public void SummonFollower(Follower follower, int index, bool createCrop = true)
    {
        // Add to BattleRow
        BattleRow.Followers.Insert(index, follower);

        // Trigger Effects?
        if (createCrop) GameState.CurrentPlayer.ChangeOffering(OfferingType.Crop, 1);

        // Update View
        //if (!GameState.IsSimulated) View.Instance.MoveFollowerToBattleRow(follower, index);
    }

    public void PayCosts(Card card)
    {
        foreach (KeyValuePair<OfferingType, int> cost in card.Costs)
        {
            Offerings[cost.Key] -= cost.Value;
        }
        OnOfferingsChange?.Invoke();
        //View.Instance.UpdateResources(this);
    }

    public bool CanPlayCard(Card card)
    {
        foreach (KeyValuePair<OfferingType, int> cost in card.Costs)
        {
            if (Offerings[cost.Key] < cost.Value) return false;
        }

        return true;
    }

    public bool CanPayForRitual(Ritual ritual)
    {
        foreach (KeyValuePair<OfferingType, int> cost in ritual.Costs)
        {
            if (Offerings[cost.Key] < cost.Value) return false;
        }

        return true;
    }
    public void PayRitualCosts(Ritual ritual)
    {
        foreach (KeyValuePair<OfferingType, int> cost in ritual.Costs)
        {
            if (Offerings[cost.Key] < cost.Value) return;
            Offerings[cost.Key] -= cost.Value;
        }
    }

    public void ChangeHealth(int change)
    {
        Health += change;
        OnHealthChange?.Invoke();

        GameState.CurrentPlayer.ChangeOffering(OfferingType.Blood, Mathf.Abs(change));
    }

    public void ChangeOffering(OfferingType offeringType, int amount)
    {
        Offerings[offeringType] += amount;

        OnOfferingsChange?.Invoke();
    }

    public bool DoDecision(PlayerDecision playerDecision)
    {
        PlayFollowerDecision playFollowerDecision = playerDecision as PlayFollowerDecision;
        PlaySpellDecision playSpellDecision = playerDecision as PlaySpellDecision;
        UseRitualDecision useRitualDecision = playerDecision as UseRitualDecision;
        AttackWithFollowerDecision attackWithFollowerDecision = playerDecision as AttackWithFollowerDecision;

        if (playFollowerDecision != null)
        {
            if (!GameState.TargetsByID.TryGetValue(playFollowerDecision.CardID, out ITarget target)) return false;
            Follower follower = target as Follower;
            if (follower == null) return false;
            TryPlayFollower(follower, playFollowerDecision.PlacementIndex);
        }
        else if (playSpellDecision != null)
        {
            if (!GameState.TargetsByID.TryGetValue(playSpellDecision.CardID, out ITarget cardToPlay)) return false;
            Spell spell = (Spell)cardToPlay;
            if (spell == null) return false;

            ITarget target = null;
            if (playSpellDecision.TargetID != -1)
            {
                if (!GameState.TargetsByID.TryGetValue(playSpellDecision.TargetID, out target)) return false;
            }
            TryPlaySpell(spell, target);
        }
        else if (useRitualDecision != null)
        {
            if (!GameState.TargetsByID.TryGetValue(useRitualDecision.RitualID, out ITarget ritualToUse)) return false;
            Ritual ritual = ritualToUse as Ritual;
            if (ritual == null) return false;

            ITarget target = null;
            if (useRitualDecision.TargetID != -1)
            {
                if (!GameState.TargetsByID.TryGetValue(useRitualDecision.TargetID, out target)) return false;
            }
            ritual.Play(target);
        }
        else if (attackWithFollowerDecision != null)
        {
            if (!GameState.TargetsByID.TryGetValue(attackWithFollowerDecision.FollowerID, out ITarget attacker)) return false;
            if (!GameState.TargetsByID.TryGetValue(attackWithFollowerDecision.TargetID, out ITarget defender)) return false;
            Follower attackingFollower = attacker as Follower;
            if (attackingFollower == null || defender == null) return false;
            GameAction newAction = new AttackWithFollowerAction(attackingFollower, defender);
            GameState.ActionHandler.AddAction(newAction);
            GameState.ActionHandler.StartEvaluating();
            //attackingFollower.AttackTarget(defender);
        }

        return true;
    }

}
