using System.Collections.Generic;
using UnityEngine;

public class AttackWithFollowerAction : GameAction
{
    public Follower Attacker;
    public ITarget Target;
    private bool attackSuccessful = false;

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

    public override void Execute(bool simulated = false, bool success = true)
    {
        if (Attacker.CurrentHealth <= 0)
        {
            attackSuccessful = false;
            base.Execute(simulated);
            return;
        }

        if (Target is Follower targetFollower)
        {
            if (targetFollower.CurrentHealth <= 0)
            {
                attackSuccessful = false;
                base.Execute(simulated);
                return;
            }

            Attacker.AttackFollower(targetFollower);
        }
        else if (Target is Player targetPlayer)
        {
            if (targetPlayer.Health <= 0)
            {
                attackSuccessful = false;
                base.Execute(simulated);
                return;
            }

            Attacker.AttackPlayer(targetPlayer);
        }

        attackSuccessful = true;
        

        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        if (!attackSuccessful) return new List<AnimationAction>();

        List<AnimationAction> animationActions = new List<AnimationAction>()
        {
            new AttackWithFollowerAnimation(this)
        };

        return animationActions;
    }

    public override void LogAction()
    {
        if (!attackSuccessful) Debug.LogWarning("Attack Failed");
        else
        {
            Debug.LogWarning(Attacker.Owner.GetName() + "'s " + Attacker.GetName() + " attacked " + Target.GetName());
        }
    }
}
