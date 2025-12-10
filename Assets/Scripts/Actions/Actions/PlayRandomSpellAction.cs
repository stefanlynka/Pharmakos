using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class PlayRandomSpellAction : GameAction
{
    public Player Owner;
    public ITarget Target;

    public PlayRandomSpellAction(Player owner)
    {
        Owner = owner;
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        PlayRandomSpellAction copy = (PlayRandomSpellAction)MemberwiseClone();
        copy.Owner = newOwner.GameState.GetTargetByID<Player>(Owner.GetID());

        return copy;
    }

    public override void Execute(bool simulated = false, bool successful = true)
    {
        List<Spell> potentialSpells = new List<Spell>();

        foreach (Spell spell in CardHandler.AllSpells)
        {
            Spell potentialSpell = (Spell)spell.MakeBaseCopy();
            if (potentialSpell == null) continue;

            potentialSpell.Owner = Owner;
            if (potentialSpell.HasPlayableTargets())
            {
                potentialSpells.Add(potentialSpell);
            }
        }

        if (potentialSpells.Count == 0)
        {
            Debug.LogError("PlayRandomSpellAction: No Spells found");
        }

        Spell randomSpell = potentialSpells[Owner.GameState.RNG.Next(0, potentialSpells.Count - 1)];
        randomSpell.Init(Owner);
        List<ITarget> possibleTargets = randomSpell.GetTargets();
        if (possibleTargets.Count == 0)
        {
            randomSpell.Play(null);
            base.Execute(simulated);
            return;
        }
        ITarget randomTarget = possibleTargets[Owner.GameState.RNG.Next(0, possibleTargets.Count - 1)];
        randomSpell.Play(randomTarget);

        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        List<AnimationAction> animationActions = new List<AnimationAction>()
        {
            
        };
        return animationActions;
    }
}
