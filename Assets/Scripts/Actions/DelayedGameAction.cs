using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DelayedGameAction
{
    public GameAction GameAction;
    public bool LimitedUses = false;
    public int UsesRemaining = 1;

    public DelayedGameAction(GameAction gameAction, bool limitedUses = true, int usesRemaining = 1)
    {
        GameAction = gameAction;
        LimitedUses = limitedUses;
        UsesRemaining = usesRemaining;
    }

    public virtual void SetLimitedUses(bool limitedUses = true, int usesRemaining = 1)
    {
        LimitedUses = limitedUses;
        UsesRemaining = usesRemaining;
    }

    public virtual void TryExecute()
    {
        if (!LimitedUses)
        {
            GameAction.Execute();
        }
        else if (UsesRemaining > 0)
        {
            GameAction.Execute();
            UsesRemaining--;
        }
    }

    public virtual bool HasUsesRemaining()
    {
        return LimitedUses ? UsesRemaining > 0 : true;
    }

    public virtual DelayedGameAction DeepCopy(Player newOwner)
    {
        DelayedGameAction copy = (DelayedGameAction)MemberwiseClone();

        GameAction copiedGameAction = GameAction.DeepCopy(newOwner);
        if (copiedGameAction == null) return null;

        copy.GameAction = copiedGameAction;

        return copy;
    }
}