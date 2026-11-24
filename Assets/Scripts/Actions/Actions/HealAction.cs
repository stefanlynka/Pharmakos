using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealAction : GameAction
{
    ITarget target;
    ITarget source;
    int amount = 0;
    int amountHealed = 0;
    int attackChange = 0;
    public HealAction(ITarget target, ITarget source, int amount)
    {
        this.target = target;
        this.source = source;
        this.amount = amount;
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        HealAction copy = (HealAction)MemberwiseClone();
        copy.target = newOwner.GameState.GetTargetByID<ITarget>(target.GetID());
        copy.source = newOwner.GameState.GetTargetByID<ITarget>(source.GetID());

        return copy;
    }

    public override void Execute(bool simulated = false, bool success = true)
    {
        Follower followerTarget = target as Follower;
        Player playerTarget = target as Player;

        // Lethal damage already landed; a heal can't bring the follower back (see Follower.IsDying).
        if (followerTarget != null && followerTarget.IsDying) return;

        if (followerTarget != null)
        {
            int prevAttack = followerTarget.GetCurrentAttack();
            int healthBefore = followerTarget.CurrentHealth;
            followerTarget.Heal(amount);
            amountHealed = followerTarget.CurrentHealth - healthBefore;
            attackChange = followerTarget.GetCurrentAttack() - prevAttack;

            if (amountHealed > 0) followerTarget.GameState.FireFollowerHealthChanges(followerTarget, amountHealed);
        }
        else if (playerTarget != null)
        {
            int healthBefore = playerTarget.Health;
            playerTarget.ChangeHealth(source, amount);
            amountHealed = playerTarget.Health - healthBefore;
        }

        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        List<AnimationAction> animationActions = new List<AnimationAction>()
        {
            new ChangeStatsAnimation(this, target, attackChange, amountHealed)
            //new SummonFollowerAnimation(this)
        };
        return animationActions;
    }
}
