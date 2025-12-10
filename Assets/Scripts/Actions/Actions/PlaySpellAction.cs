using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class PlaySpellAction : GameAction
{
    public Player Owner;
    public Spell Spell;
    public ITarget Target;

    private float showSpellDuration = 1f;

    public PlaySpellAction(Player owner, Spell spell, ITarget target)
    {
        Owner = owner;
        Spell = spell;
        Target = target;
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
        MoveCardAnimation moveCardAnimation = new MoveCardAnimation(this, Spell, Owner, GameZone.Hand, Owner, GameZone.PlayZone, duration);
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
    public override void LogAction()
    {
        if (Target != null)
        {
            Debug.LogWarning(Spell.Owner.GetName() + " played " + Spell.GetName() + " targeting " + Target.GetName());
        }
        else
        {
            Debug.LogWarning(Spell.Owner.GetName() + " played " + Spell.GetName());
        }
    }
    public override PlayHistoryItem MakePlayHistoryItem()
    {
        return new PlaySpellPlayHistory(Spell.Owner, Spell, Target);
    }
}
