using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.VisualScripting.Member;
using static UnityEngine.GraphicsBuffer;

public class PlaySpellAction : GameAction
{
    public Player Owner;
    public Spell Spell;

    private float showSpellDuration = 1f;

    public PlaySpellAction(Player owner, Spell spell)
    {
        Owner = owner;
        Spell = spell;
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        PlaySpellAction copy = (PlaySpellAction)MemberwiseClone();
        copy.Owner = newOwner.GameState.GetTargetByID<Player>(Owner.GetID());
        copy.Spell = newOwner.GameState.GetTargetByID<Spell>(Spell.GetID());

        return copy;
    }

    public override void Execute(bool simulated = false, bool success = true)
    {
        // Do nothing. This is just visual

        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        float duration = Owner.IsHuman ? 0.25f : 0.5f;
        MoveCardAnimation moveCardAnimation = new MoveCardAnimation(this, Spell, Owner, GameZone.Hand, Owner, GameZone.PlayZone, -1, duration);
        moveCardAnimation.SetScale(1, 2);
        moveCardAnimation.ForceDescriptive(true);

        showSpellDuration = Owner.IsHuman ? 0.4f : 0.8f;
        WaitAnimation waitAnimation = new WaitAnimation(this, showSpellDuration);

        RemoveCardAnimation removeCardAnimation = new RemoveCardAnimation(this, Spell);

        List<AnimationAction> animationActions = new List<AnimationAction>()
        {
            moveCardAnimation,
            waitAnimation,
            removeCardAnimation,
        };

        return animationActions;
    }

}
