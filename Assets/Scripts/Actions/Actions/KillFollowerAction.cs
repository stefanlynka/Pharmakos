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

    public KillFollowerAction(ITarget source, ITarget target)
    {
        Source = source;
        Target = target;
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        KillFollowerAction copy = (KillFollowerAction)MemberwiseClone();
        copy.Source = newOwner.GameState.GetTargetByID<ITarget>(Source.GetID());
        copy.Target = newOwner.GameState.GetTargetByID<ITarget>(Target.GetID());

        return copy;
    }

    public override void Execute(bool simulated = false)
    {
        Follower follower = Target as Follower;

        if (follower != null )
        {
            follower.Die();
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
