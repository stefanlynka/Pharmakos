using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public abstract class Card : ITarget
{
    public Player Owner = null;

    public Dictionary<OfferingType, int> Costs = new Dictionary<OfferingType, int>();

    public string Text = "";

    public Action OnChange;
    public Action OnRemove;

    public virtual void Init(Player player)
    {
        Owner = player;
        OnChange = null;
        OnRemove = null;
    }

    public virtual void Reset() { }

    public abstract List<ITarget> GetPlayableTargets();

    public virtual void Play(ITarget target) 
    {
        //PayCosts();
    }
    public void PayCosts()
    {
        Owner.PayCosts(this);
    }

    public Card MakeBaseCopy()
    {
        // Get the type of the calling class
        Type callingType = this.GetType();

        // Create a new instance of the calling class
        return (Card)Activator.CreateInstance(callingType);
    }
}

public interface ITarget
{
    public static List<ITarget> GetAllFollowers(Player player)
    {
        List<ITarget> targets = new List<ITarget>();
        targets.AddRange(player.BattleRow.Followers);
        targets.AddRange(Controller.Instance.GetOtherPlayer(player).BattleRow.Followers);
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
        targets.AddRange(Controller.Instance.GetOtherPlayer(player).BattleRow.Followers);
        return targets;
    }
    public static List<ITarget> GetAllPlayers(Player player)
    {
        List<ITarget> targets = new List<ITarget>
        {
            player,
            Controller.Instance.GetOtherPlayer(player)
        };
        return targets;
    }
}