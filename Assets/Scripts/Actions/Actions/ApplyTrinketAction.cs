using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;


public class ApplyTrinketAction : GameAction
{
    private Trinket trinket;
    private Player target;

    public ApplyTrinketAction(Trinket trinket, Player target)
    {
        this.trinket = trinket;
        this.target = target;
    }

    public override void LogAction()
    {
        Debug.LogWarning($"ApplyTrinketAction: {trinket.Name} on {target}");
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        ApplyTrinketAction applyTrinketAction = (ApplyTrinketAction)MemberwiseClone();
        applyTrinketAction.trinket = trinket.MakeBaseCopy();
        applyTrinketAction.target = newOwner.GameState.GetTargetByID<Player>(target.GetID());
        return applyTrinketAction;
    }

    public override void Execute(bool simulated = false, bool success = true)
    {
        // Apply the effect to the owner
        trinket.Owner.AddPlayerEffect(trinket.MyEffect);

        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        List<AnimationAction> animationActions = new List<AnimationAction>()
        {
            new UpdateViewAnimation(this, trinket.Owner),
        };

        return animationActions;
    }
}
