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

    public string OverrideName = "";
    public string Text = "";

    public int ID = -1;

    public Action OnChange;
    public Action OnRemove;

    public IconType Icon = IconType.None;
    public bool AffectsAdjacent = false;

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

    private string myType = "";
    public string GetCardType()
    {
        if (myType == "") myType = GetType().Name;

        return myType;
    }
    public string GetName()
    {
        if (OverrideName == "") OverrideName = GetType().Name;

        return OverrideName + (Controller.ShowCardIDs ? " " + ID : "");
    }

    public virtual bool CanPlay()
    {
        if (Owner == null) return false;
        if (!Owner.IsMyTurn) return false;
        if (this is Spell spell && !spell.HasPlayableTargets()) return false;

        foreach (KeyValuePair<OfferingType, int> cost in GetCosts())
        {
            if (Owner.Offerings[cost.Key] < cost.Value) return false;
        }

        return true;
    }

    public virtual void Play(ITarget target) 
    {
        //PayCosts();
    }
    public void PayCosts()
    {
        Owner.PayCosts(this);
    }
    public virtual Dictionary<OfferingType, int> GetCosts()
    {
        return Costs;
    }



    // Initialize a new instance of this class
    public Card MakeBaseCopy()
    {
        // Get the type of the calling class
        Type callingType = this.GetType();

        // Create a new instance of the calling class
        Card newCard = (Card)Activator.CreateInstance(callingType);
        
        return newCard;
    }

    // This is for Deep Copying a card into a new GameState
    public Card DeepCopy(Player newOwner)
    {
        Card copy = (Card)Clone();
        copy.Owner = newOwner;
        copy.GameState = newOwner.GameState;
        copy.GameState.TargetsByID[ID] = copy;

        copy.Icon = Icon;

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

    public static List<Follower> GetAllFollowers(Player player)
    {
        List<Follower> targets = new List<Follower>();
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

    public static List<ITarget> GetAllFollowersOnEdges(Player player)
    {
        List<ITarget> targets = new List<ITarget>();
        if (player.BattleRow.Followers.Count > 1)
        {
            targets.Add(player.BattleRow.Followers[0]);
            targets.Add(player.BattleRow.Followers[player.BattleRow.Followers.Count-1]);
        }

        Player otherPlayer = player.GameState.GetOtherPlayer(player.PlayerID);
        if (otherPlayer.BattleRow.Followers.Count > 1)
        {
            targets.Add(otherPlayer.BattleRow.Followers[0]);
            targets.Add(otherPlayer.BattleRow.Followers[otherPlayer.BattleRow.Followers.Count - 1]);
        }

        return targets;
    }
}

public enum  IconType
{
    None,
    Blood, // OnDrawBlood
    Bolt, // Sprint
    Bow, // Ranged
    Fangs, // OnDamage (+lifesiphon)
    Horn, // OnEnter
    Scroll, // OnSpellCast
    Shield, // DamageReduction
    Sickle, // OnKill
    Skull, // OnDeath
    Sundial, // TurnStart/End
    Sword, // OnAttack (+cleave)
    Target, // Taunt

    Star, // Other
}