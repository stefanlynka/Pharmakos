using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.VisualScripting.Member;
using static UnityEngine.GraphicsBuffer;

public class StartFightAction : GameAction
{
    private int fightNum;
    public StartFightAction(int fightNum)
    {
        this.fightNum = fightNum;
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        StartFightAction copy = (StartFightAction)MemberwiseClone();

        return copy;
    }

    public override void Execute(bool simulated = false, bool successful = true)
    {

        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        List<AnimationAction> animationActions = new List<AnimationAction>()
        {
            new StartFightAnimation(this, fightNum)
        };
        return animationActions;
    }

    public override void LogAction()
    {
        Debug.LogWarning("StartFightAction");
    }
}
