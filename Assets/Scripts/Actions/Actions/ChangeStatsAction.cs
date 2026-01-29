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
        if (Target == null)
        {
            return null;
        }
        ChangeStatsAction copy = (ChangeStatsAction)MemberwiseClone();
        copy.Target = newOwner.GameState.GetTargetByID<ITarget>(Target.GetID());

        return copy;
    }

    public override void Execute(bool simulated = false, bool successful = true)
    {
        Follower follower = Target as Follower;
        GameState gameState = follower.GameState;

        if (follower != null)
        {
            int currentAttack = follower.GetCurrentAttack();
            if (currentAttack + AttackChange < 0) AttackChange = -currentAttack;
            follower.ChangeStats(AttackChange, HealthChange);

            gameState.FireFollowerHealthChanges(follower, HealthChange);
        }


        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        List<AnimationAction> animationActions = new List<AnimationAction>()
        {
            new ChangeStatsAnimation(this, Target, AttackChange, HealthChange)
            //new DamageAnimation(this)
        };
        //if (LastStep) animationActions.Add(new IdleAnimation(this));
        return animationActions;
    }
    public override void LogAction()
    {
        if (Target is Player player)
        {
            Debug.LogWarning("ChangeStatsAction: " + player.GetName() + " changed health: " + HealthChange);
        }
        else if (Target is Follower follower)
        {
            Debug.LogWarning("ChangeStatsAction: " + follower.GetName() + " changed stats: " + AttackChange + "/" + HealthChange);
        }
        
    }
}
