using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.VisualScripting.Member;
using static UnityEngine.GraphicsBuffer;

public class StartFightAction : GameAction
{
    private string fightName;
    public StartFightAction(string fightName)
    {
        this.fightName = fightName;
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
            new StartFightAnimation(this, fightName)
        };
        return animationActions;
    }

    public override void LogAction()
    {
        Debug.LogWarning("StartFightAction");
    }
}
