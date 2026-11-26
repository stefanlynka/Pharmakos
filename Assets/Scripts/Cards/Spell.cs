using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spell : Card, ITarget
{
    public bool HasTargets = true;
    public override List<ITarget> GetTargets()
    {
        return new List<ITarget>();
    }

    // Set by spells that grant a buff when played (Twist of Fate), so the play history can show it.
    public Trinket GrantedTrinket;

    public override void Play(ITarget target)
    {
        GrantedTrinket = null;
        base.Play(target);

        //GameState.CurrentPlayer.ChangeOffering(OfferingType.Scroll, 1);

        if (target is Follower followerTarget)
        {
            followerTarget.ApplyOnTargetedEffects(this);
        }

        // Trigger all "OnSpellPlayed" triggers
        GameState.FireSpellPlayed(this);

        GameAction newAction = new PlaySpellAction(Owner, this, target);
        Owner.GameState.ActionHandler.AddAction(newAction);

        GameAction offeringAction = new CreateOfferingAction(Owner, OfferingType.Scroll, 1, Owner.ITargetID, GameState.CurrentPlayer.ITargetID);
        GameState.ActionHandler.AddAction(offeringAction);

    }

    public virtual VfxEffect GetPlayVfx(ITarget target)
    {
        return null;
    }

    // Thrown from this spell's owner to the target while the spell is shown.
    public virtual ArcProjectileStyle GetThrowStyle()
    {
        return null;
    }

    /// <summary>
    /// Plays the spell inside a play history window: its On Targeted / spell-cast reactions resolve before its entry
    /// exists and are held until it does, and the damage its own Play override deals afterwards is shown on it.
    /// </summary>
    public void PlayRecorded(ITarget target)
    {
        PlayHistoryHandler history = PlayHistoryHandler.For(GameState);
        history?.BeginPlay();
        try
        {
            Play(target);
        }
        finally
        {
            history?.EndPlay();
        }
    }

    public bool HasPlayableTargets()
    {
        return !HasTargets || GetTargets().Count > 0;
    }

}
