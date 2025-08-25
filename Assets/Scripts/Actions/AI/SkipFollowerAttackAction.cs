using System.Collections.Generic;
using UnityEngine;

public class SkipFollowerAttackAction : GameAction
{
    public Follower Attacker;

    public SkipFollowerAttackAction(Follower attacker)
    {
        Attacker = attacker;
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        SkipFollowerAttackAction copy = (SkipFollowerAttackAction)MemberwiseClone();
        copy.Attacker = newOwner.GameState.GetTargetByID<Follower>(Attacker.ID);

        return copy;
    }

    public override void Execute(bool simulated = false, bool successful = true)
    {
        Attacker.SkippedAttack = true;

        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        return new List<AnimationAction>();
    }

}

