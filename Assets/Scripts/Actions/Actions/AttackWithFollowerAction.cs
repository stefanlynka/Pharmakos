using System.Collections.Generic;
using UnityEngine;
using static Unity.VisualScripting.Member;

public class AttackWithFollowerAction : GameAction
{
    public Follower Attacker;
    public ITarget Target;

    private bool attackHappened = false;

    public AttackWithFollowerAction(Follower attacker, ITarget target)
    {
        Attacker = attacker;
        Target = target;
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        AttackWithFollowerAction copy = (AttackWithFollowerAction)MemberwiseClone();
        copy.Attacker = newOwner.GameState.GetTargetByID<Follower>(Attacker.GetID());
        copy.Target = newOwner.GameState.GetTargetByID<ITarget>(Target.GetID());

        return copy;
    }

    public override void Execute(bool simulated = false)
    {
        attackHappened = Attacker.AttackTarget(Target);

        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        if (!attackHappened) return new List<AnimationAction>();

        List<AnimationAction> animationActions = new List<AnimationAction>()
        {
            new AttackWithFollowerAnimation(this)
        };

        return animationActions;
    }

    public override void LogAction()
    {
        if (!attackHappened) Debug.LogWarning("Attack Failed");
        else
        {
            Debug.LogWarning(Attacker.Owner.GetName() + "'s " + Attacker.GetName() + " attacked " + Target.GetName());
        }
    }
}
