using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.VisualScripting.Member;
using static UnityEngine.GraphicsBuffer;

public class DiscardCardAction : GameAction
{
    public ITarget Source;
    public ITarget Target;
    public Card cardToDiscard;

    public DiscardCardAction(ITarget source, ITarget target, Card cardToDiscard)
    {
        Source = source;
        Target = target;
        this.cardToDiscard = cardToDiscard;
    }

    public override GameAction DeepCopy(Player newOwner)
    {
        DiscardCardAction copy = (DiscardCardAction)MemberwiseClone();
        copy.Source = newOwner.GameState.GetTargetByID<ITarget>(Source.GetID());
        copy.Target = newOwner.GameState.GetTargetByID<ITarget>(Target.GetID());
        copy.cardToDiscard = newOwner.GameState.GetTargetByID<Card>(cardToDiscard.GetID());

        return copy;
    }

    public override void Execute(bool simulated = false, bool success = true)
    {
        Player player = Target as Player;

        if (player != null )
        {
            player.DiscardCard(cardToDiscard);
        }

        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        List<AnimationAction> animationActions = new List<AnimationAction>()
        {
        };
        MoveCardAnimation animation = new MoveCardAnimation(this, cardToDiscard, cardToDiscard.Owner, GameZone.Hand, cardToDiscard.Owner, GameZone.Discard, 0.25f);
        animation.Stackable = true;
        animationActions.Add(animation);

        DelayStartOfAnimation waitAnimation = new DelayStartOfAnimation(this, 0.1f);
        waitAnimation.Stackable = true;
        animationActions.Add(waitAnimation);

        return animationActions;
    }

}
