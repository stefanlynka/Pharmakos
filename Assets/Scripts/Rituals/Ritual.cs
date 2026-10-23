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

    // Cards this ritual names, shown beside the hover summary. Empty when none apply.
    public virtual void CollectReferencedCards(List<Card> cards) { }

    protected static void CollectLastDeadFollower(List<Card> cards)
    {
        if (cards == null || Controller.Instance == null || !Controller.Instance.GameRunning)
            return;

        // The play-history button is shown on top of combat and becomes ScreenHandler.CurrentScreen.
        // The board is still up as long as the game screen itself has not been exited.
        if (ScreenHandler.Instance == null
            || !ScreenHandler.Instance.TryGetScreen(ScreenName.Game, out Screen gameScreen)
            || !IsGameBoardVisible(gameScreen))
            return;

        Follower last = Controller.Instance.CanonGameState != null
            ? Controller.Instance.CanonGameState.LastFollowerThatDied
            : null;
        if (last == null)
            return;

        if (last.MakeBaseCopy() is Follower copy)
            cards.Add(copy);
    }

    static bool IsGameBoardVisible(Screen gameScreen)
    {
        if (gameScreen == null)
            return false;

        if (gameScreen.Camera != null)
            return gameScreen.Camera.isActiveAndEnabled;

        return gameScreen.UIHolder != null && gameScreen.UIHolder.interactable;
    }

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

    public virtual VfxEffect GetPlayVfx(ITarget target)
    {
        VfxEffect ritualVfx = VfxLibrary.DefaultRitual(this);
        if (target is not Player || !TargetsOnlyPlayers())
            return ritualVfx;

        return new ParallelVfx(ritualVfx, VfxLibrary.PortraitOfferingFlash(this));
    }

    // True when every current legal target is a player. Rituals that can also hit followers stay false.
    public bool TargetsOnlyPlayers()
    {
        List<ITarget> targets = GetTargets();
        if (targets == null || targets.Count == 0)
            return false;

        foreach (ITarget candidate in targets)
        {
            if (candidate is not Player)
                return false;
        }

        return true;
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
