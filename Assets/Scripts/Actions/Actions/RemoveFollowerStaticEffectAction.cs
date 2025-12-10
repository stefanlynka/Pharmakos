using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

// Not sure it's possible or necessary to do this

public class RemoveFollowerStaticEffectAction : GameAction
{
    //public ITarget Source;
    public ITarget Target;
    public StaticEffect StaticEffectName = StaticEffect.None;

    public RemoveFollowerStaticEffectAction(ITarget target, StaticEffect staticEffectName)
    {
        Target = target;
        StaticEffectName = staticEffectName;
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        RemoveFollowerStaticEffectAction copy = (RemoveFollowerStaticEffectAction)MemberwiseClone();
        copy.Target = newOwner.GameState.GetTargetByID<ITarget>(Target.GetID());

        return copy;
    }

    public override void Execute(bool simulated = false, bool success = true)
    {
        if (Target is Follower followerTarget)
        {
            List<FollowerEffect> followerEffects = new List<FollowerEffect>(followerTarget.InnateEffects);

            foreach (FollowerEffect followerEffect in followerEffects)
            {
                if (followerEffect is StaticEffectDef staticEffectDef && staticEffectDef.StaticEffectName == StaticEffectName)
                {
                    followerEffect.Unapply();
                    followerTarget.InnateEffects.Remove(followerEffect);
                    break;
                }
            }
            //StaticEffectDef newStaticEffectDef = new StaticEffectDef(EffectTarget.Self, StaticEffectName);
            //followerTarget.InnateEffects.Add(newStaticEffectDef);
            //newStaticEffectDef.Apply(followerTarget);
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
