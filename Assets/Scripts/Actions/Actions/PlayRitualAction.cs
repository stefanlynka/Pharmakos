using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.UI.GridLayoutGroup;


public class PlayRitualAction : GameAction
{
    private Ritual ritual;
    private ITarget target;

    public PlayRitualAction(Ritual ritual, ITarget target)
    {
        this.ritual = ritual;
        this.target = target;
    }

    public override void LogAction()
    {
        Debug.Log($"PlayRitualAction: {ritual.GetName()} on {target}");
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        PlayRitualAction playRitualAction = (PlayRitualAction)MemberwiseClone();
        playRitualAction.ritual = newOwner.GameState.GetTargetByID<Ritual>(ritual.GetID());
        playRitualAction.target = newOwner.GameState.GetTargetByID<ITarget>(target.GetID());
        return playRitualAction;
    }

    public override void Execute(bool simulated = false, bool success = true)
    {
        ritual.ExecuteEffect(target);
        
        if (ritual.Owner != null && ritual.Owner.GameState != null)
        {
            ritual.Owner.GameState.FireRitualUsed(ritual);
        }

        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        List<AnimationAction> animationActions = new List<AnimationAction>()
        {
            new PlayVfxAnimation(this, ritual.GetPlayVfx(target), ritual, target),
        };

        return animationActions;
    }
    public override PlayHistoryItem MakePlayHistoryItem()
    {
        return new RitualPlayHistory(ritual.Owner, ritual, target);
    }
}
