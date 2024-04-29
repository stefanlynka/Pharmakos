using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : ITarget
{
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

    public int Health = 20;
    public string Name = "";
    public bool IsHuman = false;

    private int cardsPerTurn = 5;
    public int GoldPerTurn = 3;

    public Action OnHealthChange;
    public Action OnOfferingsChange;


    public virtual void StartTurn()
    {
        
    }
    public virtual void EndTurn()
    {
        DiscardHand();
        Offerings[OfferingType.Gold] = GoldPerTurn;
        View.Instance.UpdateResources(this);

        foreach (Follower follower in BattleRow.Followers)
        {
            follower.DoEndOfTurnEffects();
        }
    }

    public void Init(List<Card> deck)
    {
        BattleRow.Player = this;

        Offerings[OfferingType.Gold] = GoldPerTurn;

        Deck = deck;
        foreach (Card card in Deck)
        {
            card.Init(this);
        }
        ShuffleDeck();
        //DrawHand();
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
        for (int i = 0; i < cardsPerTurn; i++)
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

    public void SummonFollower(Follower follower, int index, bool createCrop = true)
    {
        // Add to BattleRow
        BattleRow.Followers.Insert(index, follower);

        // Trigger Effects?
        if (createCrop) Controller.Instance.CurrentPlayer.ChangeOffering(OfferingType.Crop, 1);

        // Update View
        View.Instance.MoveFollowerToBattleRow(follower, index);
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

    public bool CanPlayRitual(Ritual ritual)
    {
        foreach (KeyValuePair<OfferingType, int> cost in ritual.Costs)
        {
            if (Offerings[cost.Key] < cost.Value) return false;
        }

        return true;
    }

    public void ChangeHealth(int change)
    {
        Health += change;
        OnHealthChange?.Invoke();

        Controller.Instance.CurrentPlayer.ChangeOffering(OfferingType.Blood, Mathf.Abs(change));
    }

    public void ChangeOffering(OfferingType offeringType, int amount)
    {
        Offerings[offeringType] += amount;

        OnOfferingsChange?.Invoke();
    }
}
