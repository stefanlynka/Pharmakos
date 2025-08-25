using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.VisualScripting.Member;

public class SetStatsAction : GameAction
{
    //public ITarget Source;
    public ITarget Target;
    public int NewAttack = -1;
    public int NewHealth = -1;

    public SetStatsAction(ITarget target, int newAttack = -1, int newHealth = -1)
    {
        Target = target;
        NewAttack = newAttack;
        NewHealth = newHealth;
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
        //Player player = Target as Player;

        if (follower != null )
        {
            if (NewAttack == -1) NewAttack = follower.GetCurrentAttack();
            if (NewHealth == -1) NewHealth = follower.CurrentHealth;

            follower.SetStats(NewAttack, NewHealth);
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
