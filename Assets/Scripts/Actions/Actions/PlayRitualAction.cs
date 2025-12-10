using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
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
        //MoveCardAnimation moveCardAnimation = new MoveCardAnimation(this, Spell, Owner, GameZone.Hand, Owner, GameZone.PlayZone);
        //moveCardAnimation.SetScale(1, 2);
        //moveCardAnimation.ForceDescriptive(true);

        //showSpellDuration = Owner.IsHuman ? 0.5f : 0.8f;
        //WaitAnimation waitAnimation = new WaitAnimation(this, showSpellDuration);

        //RemoveCardAnimation removeCardAnimation = new RemoveCardAnimation(this, Spell);

        List<AnimationAction> animationActions = new List<AnimationAction>()
        {
            new RitualAnimation(this, ritual, target),
            //moveCardAnimation,
            //waitAnimation,
            //removeCardAnimation,
        };

        return animationActions;
    }
    public override PlayHistoryItem MakePlayHistoryItem()
    {
        return new RitualPlayHistory(ritual.Owner, ritual, target);
    }
}
