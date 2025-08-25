using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.VisualScripting.Member;
using static UnityEngine.GraphicsBuffer;

public class DrawCardAction : GameAction
{
    public ITarget Source;
    public ITarget Target;
    public int CardsDrawn;

    public DrawCardAction(ITarget source, ITarget target, int cardsDrawn)
    {
        Source = source;
        Target = target;
        CardsDrawn = cardsDrawn;
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        DrawCardAction copy = (DrawCardAction)MemberwiseClone();
        copy.Source = newOwner.GameState.GetTargetByID<ITarget>(Source.GetID());
        copy.Target = newOwner.GameState.GetTargetByID<ITarget>(Target.GetID());

        return copy;
    }

    public override void Execute(bool simulated = false, bool success = true)
    {
        Player player = Target as Player;

        if (player != null )
        {
            for (int i = 0; i < CardsDrawn; i++)
            {
                player.DrawCard();
            }
        }

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
