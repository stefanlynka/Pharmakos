using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class DealDamageAction : GameAction
{
    public ITarget Source;
    public ITarget Target;
    public int Damage;

    public DealDamageAction(ITarget source, ITarget target, int damage)
    {
        Source = source;
        Target = target;
        Damage = damage;
    }

    public override void Execute(bool simulated = false)
    {
        Follower follower = Target as Follower;
        Player player = Target as Player;

        if (follower != null )
        {
            follower.ChangeHealth(Source , -Damage);
        }
        else if (player != null )
        {
            player.ChangeHealth(Source , -Damage);
        }

        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        List<AnimationAction> animationActions = new List<AnimationAction>(AnimationActions)
        {
            new DamageAnimation(this)
        };
        //if (LastStep) animationActions.Add(new IdleAnimation(this));
        return animationActions;
    }

}
