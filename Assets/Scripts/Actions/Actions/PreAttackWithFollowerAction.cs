using System.Collections.Generic;
using UnityEngine;

public class PreAttackWithFollowerAction : GameAction
{
    public Follower Attacker;
    public ITarget Target;

    public PreAttackWithFollowerAction(Follower attacker, ITarget target)
    {
        Attacker = attacker;
        Target = target;
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        PreAttackWithFollowerAction copy = (PreAttackWithFollowerAction)MemberwiseClone();
        copy.Attacker = newOwner.GameState.GetTargetByID<Follower>(Attacker.GetID());
        copy.Target = newOwner.GameState.GetTargetByID<ITarget>(Target.GetID());

        return copy;
    }

    public override void Execute(bool simulated = false, bool success = true)
    {
        Attacker.AttackTarget(Target);

        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        //if (!attackHappened) return new List<AnimationAction>();

        List<AnimationAction> animationActions = new List<AnimationAction>()
        {
            //new AttackWithFollowerAnimation(this)
        };

        return animationActions;
    }

    public override void LogAction()
    {
        Debug.LogWarning(Attacker.Owner.GetName() + "'s " + Attacker.GetName() + "is about to attack " + Target.GetName());
    }
}
