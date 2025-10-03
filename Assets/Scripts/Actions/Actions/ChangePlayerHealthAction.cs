using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class ChangePlayerHealthAction : GameAction
{
    Player target;
    ITarget source;
    int amount = 0;

    public ChangePlayerHealthAction(Player target, ITarget source, int amount)
    {
        this.target = target;
        this.source = source;
        this.amount = amount;
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
        target.ChangeHealth(source, amount);

        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        List<AnimationAction> animationActions = new List<AnimationAction>()
        {
            new ChangeStatsAnimation(this, target, 0, amount)
            //new SummonFollowerAnimation(this)
        };
        return animationActions;
    }
}
