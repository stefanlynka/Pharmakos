using System;
using System.Collections.Generic;
using UnityEngine;

public class Player : ITarget
{
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

    // Delayed Effects
    public List<DelayedGameAction> StartOfTurnActions = new List<DelayedGameAction>();
    public List<DelayedGameAction> EndOfTurnActions = new List<DelayedGameAction>();
    public List<DelayedGameAction> StartOfEveryTurnActions = new List<DelayedGameAction>();
    public List<DelayedGameAction> EndOfEveryTurnActions = new List<DelayedGameAction>();

    // Player Effects (Rituals)
    public List<PlayerEffect> PlayerEffects = new List<PlayerEffect>();

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
        Player copy = MakeBaseCopy(); // Brand new Player instance

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
        copy.BattleRow.ReapplyAllEffects(); // Can only reapply effects after all followers are copied over

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

    public void DeepCopyDelayedEffects(Player copy)
    {
        foreach (DelayedGameAction delayedGameAction in StartOfTurnActions)
        {
            copy.StartOfTurnActions.Add(delayedGameAction.DeepCopy(copy));
        }
        foreach (DelayedGameAction delayedGameAction in StartOfEveryTurnActions)
        {
            copy.StartOfEveryTurnActions.Add(delayedGameAction.DeepCopy(copy));
        }
        foreach (DelayedGameAction delayedGameAction in EndOfTurnActions)
        {
            copy.EndOfTurnActions.Add(delayedGameAction.DeepCopy(copy));
        }
        foreach (DelayedGameAction delayedGameAction in EndOfEveryTurnActions)
        {
            copy.EndOfEveryTurnActions.Add(delayedGameAction.DeepCopy(copy));
        }
    }

    public void DeepCopyPlayerEffects(Player copy)
    {
        foreach (PlayerEffect playerEffectDef in PlayerEffects)
        {
            copy.AddPlayerEffect(playerEffectDef.DeepCopy(copy));
        }
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

        foreach (Follower follower in BattleRow.Followers)
        {
            follower.DoStartOfMyTurnEffects();
            follower.DoStartOfEachTurnEffects();
        }

        Player otherPlayer = GameState.GetOtherPlayer(PlayerID);
        foreach (Follower follower in otherPlayer.BattleRow.Followers)
        {
            follower.DoStartOfEachTurnEffects();
        }

        DoStartOfTurnPlayerActions();
        DoStartOfEveryTurnPlayerActions();

        otherPlayer.DoStartOfEveryTurnPlayerActions();
    }

    // Cleanup for this Player. Should only be called by GameState.EndTurn()
    public virtual void EndTurn()
    {
        if (!IsMyTurn) return;

        DiscardHand();
        Offerings[OfferingType.Gold] = GoldPerTurn;
        View.Instance.UpdateResources(this);

        DoEndOfTurnPlayerActions();
        DoEndOfEveryTurnPlayerActions();

        foreach (Follower follower in BattleRow.Followers)
        {
            follower.DoEndOfMyTurnEffects();
            follower.DoEndOfEachTurnEffects();
        }

        Player otherPlayer = GameState.GetOtherPlayer(PlayerID);
        if (otherPlayer != null)
        {
            foreach (Follower follower in otherPlayer.BattleRow.Followers)
            {
                follower.DoEndOfEachTurnEffects();
            }
        }

        otherPlayer.DoEndOfEveryTurnPlayerActions();

        GameState.FollowerDeathsThisTurn = 0;
    }

    private void DoEndOfTurnPlayerActions()
    {
        List<DelayedGameAction> actions = new List<DelayedGameAction>();
        foreach (DelayedGameAction delayedGameAction in EndOfTurnActions)
        {
            delayedGameAction.TryExecute();
            bool actionShouldPersist = delayedGameAction.HasUsesRemaining();
            if (actionShouldPersist) actions.Add(delayedGameAction);
        }

        EndOfTurnActions = actions;
    }
    private void DoEndOfEveryTurnPlayerActions()
    {
        List<DelayedGameAction> actions = new List<DelayedGameAction>();
        foreach (DelayedGameAction delayedGameAction in EndOfEveryTurnActions)
        {
            delayedGameAction.TryExecute();
            bool actionShouldPersist = delayedGameAction.HasUsesRemaining();
            if (actionShouldPersist) actions.Add(delayedGameAction);
        }

        EndOfEveryTurnActions = actions;
    }

    private void DoStartOfTurnPlayerActions()
    {
        List<DelayedGameAction> actions = new List<DelayedGameAction>();
        foreach (DelayedGameAction delayedGameAction in StartOfTurnActions)
        {
            delayedGameAction.TryExecute();
            bool actionShouldPersist = delayedGameAction.HasUsesRemaining();
            if (actionShouldPersist) actions.Add(delayedGameAction);
        }

        StartOfTurnActions = actions;
    }
    private void DoStartOfEveryTurnPlayerActions()
    {
        List<DelayedGameAction> actions = new List<DelayedGameAction>();
        foreach (DelayedGameAction delayedGameAction in StartOfEveryTurnActions)
        {
            delayedGameAction.TryExecute();
            bool actionShouldPersist = delayedGameAction.HasUsesRemaining();
            if (actionShouldPersist) actions.Add(delayedGameAction);
        }

        StartOfEveryTurnActions = actions;
    }

    public void AddPlayerEffect(PlayerEffect playerEffectDef)
    {
        PlayerEffects.Add(playerEffectDef);
        playerEffectDef.Apply();
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
        foreach (TriggeredFollowerEffectInstance effectInstance in follower.OnDeathEffects)
        {
            effectInstance.Trigger();
        }    
        BattleRow.RemoveFollower(follower);
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
    }

    //public void PlayFollower(Follower follower, int index)
    //{
    //    // Pay costs and remove from hand
    //    PlayCard(follower);

    //    SummonFollower(follower, index);
    //}

    public void TryPlaySpell(Spell spell, ITarget target)
    {
        PlayCard(spell);
        spell.Play(target);
        View.Instance.RemoveCard(spell);
    }

    public void SummonFollower(Follower follower, int index, bool createCrop = true)
    {
        // Add to BattleRow
        index = Mathf.Min(index, BattleRow.Followers.Count);
        BattleRow.AddFollower(follower, index);

        // Trigger Offerings
        if (createCrop) GameState.CurrentPlayer.ChangeOffering(OfferingType.Crop, 1);

        // Trigger Effects
        GameState.FireFollowerEnters(follower);

        // Apply Follower static effects 
        follower.ApplyInnateEffects();

        // Apply one time on enter effects
        follower.ApplyOnEnterEffects();


        // Update View
        //if (!GameState.IsSimulated) View.Instance.MoveFollowerToBattleRow(follower, index);
    }

    public void PayCosts(Card card)
    {
        foreach (KeyValuePair<OfferingType, int> cost in card.GetCosts())
        {
            Offerings[cost.Key] -= cost.Value;
        }
        OnOfferingsChange?.Invoke();
        //View.Instance.UpdateResources(this);
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

    public void ChangeHealth(ITarget source, int value)
    {
        Health += value;

        Follower sourceFollower = source as Follower;
        if (sourceFollower != null)
        {
            if (value < 0) sourceFollower.ApplyOnDamageEffects(this, -value);

            sourceFollower.ApplyOnDrawBloodEffects(this, -value);
        }

        OnHealthChange?.Invoke();

        GameState.CurrentPlayer.ChangeOffering(OfferingType.Blood, Mathf.Abs(value));
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
        SkipFollowerAttackDecision skipFollowerAttackDecision = playerDecision as SkipFollowerAttackDecision;

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
            //attackingFollower.AttackTarget(defender);
        }
        else if (skipFollowerAttackDecision != null)
        {
            if (!GameState.TargetsByID.TryGetValue(attackWithFollowerDecision.FollowerID, out ITarget attacker)) return false;
            Follower attackingFollower = attacker as Follower;
            if (attackingFollower == null) return false;
            GameAction newAction = new SkipFollowerAttackAction(attackingFollower);
            GameState.ActionHandler.AddAction(newAction);

        }

        return true;
    }

    public Player GetOtherPlayer()
    {
        return GameState.GetOtherPlayer(PlayerID);
    }
}
