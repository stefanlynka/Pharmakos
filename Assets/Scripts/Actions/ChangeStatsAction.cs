using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

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

    public override void Execute(bool simulated = false)
    {
        Follower follower = Target as Follower;
        //Player player = Target as Player;

        if (follower != null )
        {
            follower.ChangeStats(AttackChange, HealthChange);
        }
        //else if (player != null )
        //{
        //    player.ChangeHealth(Source , -Damage);
        //}

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
