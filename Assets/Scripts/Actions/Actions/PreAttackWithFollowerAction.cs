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
        if (!simulated && ShouldPlayAttackAnimation())
        {
            View.Instance.AnimationHandler.AddAnimationActionToQueue(new AttackWithFollowerAnimation(this));
        }

        Attacker.AttackTarget(Target);

        base.Execute(simulated);
    }

    private bool ShouldPlayAttackAnimation()
    {
        if (Attacker == null || Target == null) return false;
        if (Attacker.CurrentHealth <= 0) return false;
        if (Target is Follower follower && follower.CurrentHealth <= 0) return false;
        if (Target is Player player && player.Health <= 0) return false;
        return true;
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        return new List<AnimationAction>();
    }

    public override void LogAction()
    {
        Debug.LogWarning(Attacker.Owner.GetName() + "'s " + Attacker.GetName() + " is about to attack " + Target.GetName());
    }
}
