using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public abstract class Card : ICloneable, ITarget
{
    public GameState GameState { get; private set; }
    public Player Owner = null;

    public Dictionary<OfferingType, int> Costs = new Dictionary<OfferingType, int>();

    public string Text = "";

    public int ID = -1;

    public Action OnChange;
    public Action OnRemove;

    
    // Must be called once when a card comes into existence (Becoming a card in deck/hand/play, no longer just part of a deck list)
    public virtual void Init(Player player)
    {
        Owner = player;
        GameState = player.GameState;

        ID = -1;
        GameState.TryAssignID(this);

        OnChange = null;
        OnRemove = null;
    }

    public virtual void Reset() { }

    public abstract List<ITarget> GetTargets();

    public string GetName()
    {
        return GetType().Name;
    }

    public virtual void Play(ITarget target) 
    {
        //PayCosts();
    }
    public void PayCosts()
    {
        Owner.PayCosts(this);
    }

    

    // Initialize a new instance of this class
    public Card MakeBaseCopy()
    {
        // Get the type of the calling class
        Type callingType = this.GetType();

        // Create a new instance of the calling class
        Card newCard = (Card)Activator.CreateInstance(callingType);
        newCard.Init(Owner);
        return newCard;
    }

    // This is for Deep Copying a card into a new GameState
    public Card DeepCopy(Player newOwner)
    {
        Card copy = (Card)Clone();
        copy.Owner = newOwner;
        copy.GameState = newOwner.GameState;
        copy.GameState.TargetsByID[ID] = copy;

        return copy;
    }

    // Call DeepCopy instead for cloning GameStates
    // Copy this card
    public object Clone()
    {
        Card clone = (Card)this.MemberwiseClone(); // Creates new Card, copies over all variables. (Reference types will point to the original's object, so be careful)
        clone.HandleCloned(this); // For Followers this wipes out all Effects
        return clone;
    }

    // Children can implement this to handle specifics
    protected virtual void HandleCloned(Card original)
    {
        
    }

    public int GetID()
    {
        return ID;
    }

}


public interface ITarget
{
    public int GetID();

    public string GetName();

    public static List<ITarget> GetAllFollowers(Player player)
    {
        List<ITarget> targets = new List<ITarget>();
        targets.AddRange(player.BattleRow.Followers);
        
        targets.AddRange(player.GameState.GetOtherPlayer(player.PlayerID).BattleRow.Followers);
        return targets;
    }
    public static List<ITarget> GetOwnFollowers(Player player)
    {
        List<ITarget> targets = new List<ITarget>();
        targets.AddRange(player.BattleRow.Followers);
        return targets;
    }
    public static List<ITarget> GetEnemyFollowers(Player player)
    {
        List<ITarget> targets = new List<ITarget>();
        targets.AddRange(player.GameState.GetOtherPlayer(player.PlayerID).BattleRow.Followers);
        return targets;
    }
    public static List<ITarget> GetAllPlayers(Player player)
    {
        List<ITarget> targets = new List<ITarget>
        {
            player,
            player.GameState.GetOtherPlayer(player.PlayerID)
        };
        return targets;
    }
}