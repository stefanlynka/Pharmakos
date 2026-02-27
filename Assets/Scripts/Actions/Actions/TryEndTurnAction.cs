using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.VisualScripting.Member;
using static UnityEngine.GraphicsBuffer;

public class TryEndTurnAction : GameAction
{
    Player owner;

    public TryEndTurnAction(Player owner)
    {
        this.owner = owner;
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        TryEndTurnAction copy = (TryEndTurnAction)MemberwiseClone();
        copy.owner = newOwner.GameState.GetTargetByID<Player>(owner.GetID());

        return copy;
    }

    public override void Execute(bool simulated = false, bool successful = true)
    {
        owner.GameState.DoEndOfTurnActions();
        View.Instance.DoingEndOfTurnActions = true;
        //var endTurnAction = new EndTurnAction(owner.GameState.CurrentPlayer);
        //owner.GameState.ActionHandler.AddAction(endTurnAction);

        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        //View.Instance.TurnIsEnding = true;
        List<AnimationAction> animationActions = new List<AnimationAction>()
        {
            new StartEndTurnAnimation(this)
        };
        return animationActions;
    }
}
