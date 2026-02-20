using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.VisualScripting.Member;

public class UpdateStatsAction : GameAction
{
    //public ITarget Source;
    public ITarget Target;
    bool foundTarget = false;
    int CurrentAttack = 0;
    int CurrentHealth = 0;
    public UpdateStatsAction(ITarget target)
    {
        Target = target;
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        if (Target == null)
        {
            return null;
        }
        UpdateStatsAction copy = (UpdateStatsAction)MemberwiseClone();
        copy.Target = newOwner.GameState.GetTargetByID<ITarget>(Target.GetID());

        return copy;
    }

    public override void Execute(bool simulated = false, bool successful = true)
    {
        Follower follower = Target as Follower;
        Player player = Target as Player;
        GameState gameState = follower.GameState;

        if (follower != null)
        {
            foundTarget = true;

            CurrentAttack = follower.GetCurrentAttack();
            CurrentHealth = follower.CurrentHealth;
        }
        else if (player != null)
        {
            foundTarget = true;
            CurrentHealth = player.Health;
        }

        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        List<AnimationAction> animationActions = new List<AnimationAction>()
        {
        };
        if (foundTarget)
        {
            animationActions.Add(new UpdateStatsAnimation(this, Target, CurrentAttack, CurrentHealth));
        }
        //if (LastStep) animationActions.Add(new IdleAnimation(this));
        return animationActions;
    }
    public override void LogAction()
    {
        if (Target is Player player)
        {
            Debug.LogWarning("UpdateStatsAction: for " + player.GetName());
        }
        else if (Target is Follower follower)
        {
            Debug.LogWarning("UpdateStatsAction: for " + follower.GetName());
        }
        
    }
}
