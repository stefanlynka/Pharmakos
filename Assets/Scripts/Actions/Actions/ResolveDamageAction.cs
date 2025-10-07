using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class ResolveDamageAction : GameAction
{
    public ITarget Target;

    public ResolveDamageAction(ITarget target)
    {
        Target = target;
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        DealDamageAction copy = (DealDamageAction)MemberwiseClone();
        copy.Target = newOwner.GameState.GetTargetByID<ITarget>(Target.GetID());

        return copy;
    }

    public override void Execute(bool simulated = false, bool success = true)
    {
        Follower follower = Target as Follower;
        //Player player = Target as Player;

        if (follower != null )
        {
            follower.ResolveDamage();
        }

        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        List<AnimationAction> animationActions = new List<AnimationAction>()
        {
            //new WaitAnimation(this, 0.01f)
            //new ChangeStatsAnimation(this, Target, -attackChange, -damageDealt)
            //new DamageAnimation(this)
        };
        //if (LastStep) animationActions.Add(new IdleAnimation(this));
        return animationActions;
    }

}
