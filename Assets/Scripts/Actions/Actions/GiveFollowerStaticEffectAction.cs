using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

// Not sure it's possible or necessary to do this

public class GiveFollowerStaticEffectAction : GameAction
{
    //public ITarget Source;
    public ITarget Target;
    public StaticEffect StaticEffectName = StaticEffect.None;

    public GiveFollowerStaticEffectAction(ITarget target, StaticEffect staticEffectName)
    {
        Target = target;
        StaticEffectName = staticEffectName;
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        GiveFollowerStaticEffectAction copy = (GiveFollowerStaticEffectAction)MemberwiseClone();
        copy.Target = newOwner.GameState.GetTargetByID<ITarget>(Target.GetID());

        return copy;
    }

    public override void Execute(bool simulated = false, bool success = true)
    {
        if (Target is Follower followerTarget)
        {
            StaticEffectDef newStaticEffectDef = new StaticEffectDef(EffectTarget.Self, StaticEffectName);
            followerTarget.InnateEffects.Add(newStaticEffectDef);
            newStaticEffectDef.Apply(followerTarget);
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
