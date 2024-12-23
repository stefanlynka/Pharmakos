using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
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

    public virtual bool TryExecute()
    {
        if (!LimitedUses || UsesRemaining > 0)
        {
            GameAction.Execute();
        }

        return LimitedUses ? UsesRemaining > 0 : true;
    }

    public virtual DelayedGameAction DeepCopy(Player newOwner)
    {
        DelayedGameAction copy = (DelayedGameAction)MemberwiseClone();

        copy.GameAction = GameAction.DeepCopy(newOwner);

        return copy;
    }
}