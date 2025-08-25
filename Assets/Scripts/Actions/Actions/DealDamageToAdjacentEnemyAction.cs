using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

public class DealDamageToAdjacentEnemyAction : GameAction
{
    public ITarget Source;
    public int Damage;

    public DealDamageToAdjacentEnemyAction(ITarget source,int damage)
    {
        Source = source;
        Damage = damage;
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        DealDamageToAdjacentEnemyAction copy = (DealDamageToAdjacentEnemyAction)MemberwiseClone();
        copy.Source = newOwner.GameState.GetTargetByID<ITarget>(Source.GetID());

        return copy;
    }

    public override void Execute(bool simulated = false, bool success = true)
    {
        Follower follower = Source as Follower;

        List<ITarget> targets = follower.GetAttackTargets(true);
        if (targets.Count != 0)
        {
            ITarget target = targets[follower.GameState.RNG.Next(0, targets.Count)];
            if (target is Follower targetFollower)
            {
                targetFollower.ChangeHealth(Source, -Damage);
            }
            else if (target is Player targetPlayer)
            {
                targetPlayer.ChangeHealth(Source, -Damage);
            }
        }

        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        List<AnimationAction> animationActions = new List<AnimationAction>()
        {
            new DamageAnimation(this)
        };

        return animationActions;
    }
}
