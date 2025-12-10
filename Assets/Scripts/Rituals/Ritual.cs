using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Ritual : ITarget
{
    public Player Owner;

    public Dictionary<OfferingType, int> Costs = new Dictionary<OfferingType, int>();

    public int ID = -1;
    public bool NeedsTargets = true;
    public string Name = "No Name";
    public string Description = "No Desc";
    public string ReminderText = "";

    //public Ritual(){ }

    public Ritual()
    {
    }

    public void Init(Player player)
    {
        Owner = player;
        Owner.GameState.TryAssignID(this);
    }

    public int GetID()
    {
        return ID;
    }
    public string GetName()
    {
        return GetType().Name;
    }

    public virtual List<ITarget> GetTargets()
    {
        var targets = new List<ITarget>();

        return targets;
    }

    public virtual bool CanPlay()
    {
        if (!Owner.CanPayForRitual(this)) return false;

        List<ITarget> targets = GetTargets();
        bool hasTargets = targets.Count > 0 || !NeedsTargets;
        if (!hasTargets) return false;

        return true;
    }

    public virtual void Play(ITarget target)
    {
        Owner.PayRitualCosts(this);

        GameAction action = new PlayRitualAction(this, target);
        Owner.GameState.ActionHandler.AddAction(action);

        //GameAction action = new PlayRitualAction(this, target);
        //Owner.GameState.ActionHandler.AddAction(action);
        //AnimationAction animationAction = new
        //AnimationHandler.instance.AddAnimationActionToQueue(, true);
    }

    public virtual void ExecuteEffect(ITarget target)
    {
        // Each Ritual subclass should override this to define its effect
    }

    public Ritual MakeBaseCopy()
    {
        // Get the type of the calling class
        Type callingType = this.GetType();

        // Create a new instance of the calling class
        return (Ritual)Activator.CreateInstance(callingType);
    }

    public Ritual DeepCopy(Player newOwner)
    {
        Ritual copy = MakeBaseCopy();
        copy.Owner = newOwner;
        copy.ID = ID;
        copy.Owner.GameState.TargetsByID[ID] = copy;
        return copy;
    }

    public int GetCost(OfferingType offeringType)
    {
        if (Owner == null) return Costs[offeringType];

        return Math.Max(0, Costs[offeringType] - Owner.RitualCostReductions[offeringType]);
    }
}
