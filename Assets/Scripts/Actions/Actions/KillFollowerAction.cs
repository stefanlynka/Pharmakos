using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.VisualScripting.Member;
using static UnityEngine.GraphicsBuffer;

public class KillFollowerAction : GameAction
{
    public ITarget Source;
    public ITarget Target;
    public bool Sacrifice;

    public KillFollowerAction(ITarget source, ITarget target, bool sacrifice = false)
    {
        Source = source;
        Target = target;
        Sacrifice = sacrifice;
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        KillFollowerAction copy = (KillFollowerAction)MemberwiseClone();
        copy.Source = newOwner.GameState.GetTargetByID<ITarget>(Source.GetID());
        copy.Target = newOwner.GameState.GetTargetByID<ITarget>(Target.GetID());

        return copy;
    }

    public override void Execute(bool simulated = false, bool successful = true)
    {
        Follower follower = Target as Follower;

        if (follower != null )
        {
            follower.Die();
        }

        if (Sacrifice)
        {
            // Fire FollowerSacrificed before the follower dies
            follower.Owner.GameState.FireFollowerSacrificed(follower);
        }

        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        List<AnimationAction> animationActions = new List<AnimationAction>()
        {
        };

        return animationActions;
    }

}
