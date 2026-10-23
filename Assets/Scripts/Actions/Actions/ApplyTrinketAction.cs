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
    private bool revealWithAnimation;

    public ApplyTrinketAction(Trinket trinket, Player target, bool revealWithAnimation = false)
    {
        this.trinket = trinket;
        this.target = target;
        this.revealWithAnimation = revealWithAnimation;
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
        // The AI resolves the whole turn before its animations play. Hide the icon
        // until Twist of Fate's play animation reveals it.
        if (revealWithAnimation && !simulated && trinket.MyEffect != null && View.Instance != null)
            View.Instance.HideBuffUntilRevealed(trinket.MyEffect);

        // Apply the effect to the owner
        trinket.Owner.AddPlayerEffect(trinket.MyEffect);

        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        if (revealWithAnimation)
        {
            return new List<AnimationAction>()
            {
                new RevealTrinketAnimation(this, trinket.MyEffect),
            };
        }

        List<AnimationAction> animationActions = new List<AnimationAction>()
        {
            new UpdateViewAnimation(this, trinket.Owner),
        };

        return animationActions;
    }
}
