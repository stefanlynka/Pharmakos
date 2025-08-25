using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.VisualScripting.Member;

public class ChangeStatsAction : GameAction
{
    //public ITarget Source;
    public ITarget Target;
    public int AttackChange;
    public int HealthChange;

    public ChangeStatsAction(ITarget target, int attackChange, int healthChange)
    {
        Target = target;
        AttackChange = attackChange;
        HealthChange = healthChange;
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        ChangeStatsAction copy = (ChangeStatsAction)MemberwiseClone();
        copy.Target = newOwner.GameState.GetTargetByID<ITarget>(Target.GetID());

        return copy;
    }

    public override void Execute(bool simulated = false, bool successful = true)
    {
        Follower follower = Target as Follower;

        if (follower != null)
        {
            follower.ChangeStats(AttackChange, HealthChange);
        }

        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        List<AnimationAction> animationActions = new List<AnimationAction>()
        {
            //new DamageAnimation(this)
        };
        //if (LastStep) animationActions.Add(new IdleAnimation(this));
        return animationActions;
    }

}
