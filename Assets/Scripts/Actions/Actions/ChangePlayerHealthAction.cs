using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangePlayerHealthAction : GameAction
{
    Player target;
    ITarget source;
    int amount = 0;
    int healthChanged = 0;

    public ChangePlayerHealthAction(Player target, ITarget source, int amount)
    {
        this.target = target;
        this.source = source;
        this.amount = amount;
    }

    public override void AddPlayHistoryResults(TriggeredEffectPlayHistory record)
    {
        record.AddResult(target);
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        ChangePlayerHealthAction copy = (ChangePlayerHealthAction)MemberwiseClone();
        copy.target = newOwner.GameState.GetTargetByID<Player>(target.GetID());
        copy.source = newOwner.GameState.GetTargetByID<ITarget>(source.GetID());

        return copy;
    }

    public override void Execute(bool simulated = false, bool success = true)
    {
        int prevHealth = target.Health;
        target.ChangeHealth(source, amount);
        healthChanged = target.Health - prevHealth;

        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        List<AnimationAction> animationActions = new List<AnimationAction>()
        {
            new ChangeStatsAnimation(this, target, 0, healthChanged)
            //new SummonFollowerAnimation(this)
        };
        return animationActions;
    }
}
