using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.VisualScripting.Member;
using static UnityEngine.GraphicsBuffer;

public class EndTurnAction : GameAction
{
    Player owner;

    public EndTurnAction(Player owner)
    {
        this.owner = owner;
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        EndTurnAction copy = (EndTurnAction)MemberwiseClone();
        copy.owner = newOwner.GameState.GetTargetByID<Player>(owner.GetID());

        return copy;
    }

    public override void Execute(bool simulated = false, bool successful = true)
    {
        // Check if either player has died
        if (Controller.Instance.CheckForPlayerDeath())
        {
            base.Execute(simulated);
            return;
        }


        owner.GameState.EndTurn();

        if (!simulated)
        {
            Controller.Instance.PlayHistoryHandler.ProgressPlayHistory();
        }

        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        List<AnimationAction> animationActions = new List<AnimationAction>()
        {
            new ShowBannerAnimation(this, !owner.IsHuman)
        };
        return animationActions;
    }

    public override void LogAction()
    {
        Debug.LogWarning("EndTurnAction: " + owner.Name + " ends their turn");
    }
}
