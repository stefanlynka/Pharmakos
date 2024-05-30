using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public class Player : ITarget
{
    ///
    /// Deep Copied
    ///
    public List<Card> Deck = new List<Card>();
    public List<Card> Hand = new List<Card>();
    public List<Card> Graveyard = new List<Card>();

    public BattleRow BattleRow = new BattleRow();

    public Dictionary<OfferingType, int> Offerings = new Dictionary<OfferingType, int>()
        {
            { OfferingType.Gold, 0},
            { OfferingType.Blood, 3},
            { OfferingType.Bone, 3},
            { OfferingType.Crop, 3},
            { OfferingType.Scroll, 3},
        };
    public Ritual MinorRitual;
    public Ritual MajorRitual;

    // For identifying which Player is which
    public int PlayerID = -1;

    // For identifying which ITarget is which
    public int ITargetID = -1;

    public int Health = 20;
    public string Name = "";
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

    public Player() { }

    private Player MakeBaseCopy()
    {
        // Get the type of the calling class
        Type callingType = this.GetType();

        // Create a new instance of the calling class
        Player newCard = (Player)Activator.CreateInstance(callingType);
        return newCard;
    }

    public void Init(GameState gameState, List<Card> deck, int playerID)
    {
        PlayerID = playerID;

        AttachToGameState(gameState);
        gameState.TryAssignID(this);

        BattleRow.Owner = this;

        Offerings[OfferingType.Gold] = GoldPerTurn;

        Deck = deck;
        foreach (Card card in Deck)
        {
            card.Init(this);
        }
        ShuffleDeck();
        //DrawHand();
    }

    public Player DeepCopy(GameState newGameState)
    {
        Player copy = MakeBaseCopy();

        copy.AttachToGameState(newGameState);

        copy.Health = Health;
        copy.Name = Name;
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
        copy.MinorRitual = MinorRitual.DeepCopy(copy);
        copy.MajorRitual = MajorRitual.DeepCopy(copy);

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

    public virtual void RunUpdate()
    {

    }
    public virtual void StartTurn()
    {
        DrawHand();
    }
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

        GameState.EndTurn();
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
        if (!GameState.IsSimulated) View.Instance.MoveFollowerToBattleRow(follower, index);
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

    public void DoDecision(PlayerDecision playerDecision)
    {
        PlayFollowerDecision playFollowerDecision = playerDecision as PlayFollowerDecision;
        PlaySpellDecision playSpellDecision = playerDecision as PlaySpellDecision;
        UseRitualDecision useRitualDecision = playerDecision as UseRitualDecision;

        if (playFollowerDecision != null)
        {
            if (!GameState.TargetsByID.TryGetValue(playFollowerDecision.CardID, out ITarget target)) return;
            Follower follower = target as Follower;
            if (follower == null) return;
            TryPlayFollower(follower, playFollowerDecision.PlacementIndex);
        }
        else if (playSpellDecision != null)
        {
            if (!GameState.TargetsByID.TryGetValue(playSpellDecision.CardID, out ITarget cardToPlay)) return;
            Spell spell = (Spell)cardToPlay;
            if (spell == null) return;

            ITarget target = null;
            if (playSpellDecision.TargetID != -1)
            {
                if (!GameState.TargetsByID.TryGetValue(playSpellDecision.TargetID, out target)) return;
            }
            TryPlaySpell(spell, target);
        }
        else if (useRitualDecision != null)
        {
            if (!GameState.TargetsByID.TryGetValue(useRitualDecision.RitualID, out ITarget ritualToUse)) return;
            Ritual ritual = ritualToUse as Ritual;
            if (ritual == null) return;

            ITarget target = null;
            if (useRitualDecision.TargetID != -1)
            {
                if (!GameState.TargetsByID.TryGetValue(useRitualDecision.TargetID, out target)) return;
            }
            ritual.Play(target);
        }
    }

}
