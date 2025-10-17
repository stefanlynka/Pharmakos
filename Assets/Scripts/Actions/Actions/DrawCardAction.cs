using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR;
using static Unity.VisualScripting.Member;
using static UnityEngine.GraphicsBuffer;

public class DrawCardAction : GameAction
{
    public ITarget Source;
    public ITarget Target;
    Player player;
    private int cardCount;
    private List<Card> cardsDrawn = new List<Card>();
    private int preDrawIndex = 0;

    public DrawCardAction(ITarget source, ITarget target, int cardCount)
    {
        Source = source;
        Target = target;
        this.cardCount = cardCount;
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
        player = Target as Player;

        if (player != null )
        {
            preDrawIndex = player.Hand.Count;

            for (int i = 0; i < cardCount; i++)
            {
                Card cardDrawn = player.DrawCard();
                if (cardDrawn != null) cardsDrawn.Add(cardDrawn);
            }
        }

        base.Execute(simulated);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        List<AnimationAction> animationActions = new List<AnimationAction>()
        {  
        };
        int index = -1;
        if (player != null)
        {
            //ViewHandHandler handHandler = player.IsHuman ? View.Instance.Player1.HandHandler : View.Instance.Player2.HandHandler;
            index = preDrawIndex; //handHandler.ViewCards.Count;
        }
        foreach (Card card in cardsDrawn)
        {
            MoveCardAnimation animation = new MoveCardAnimation(this, card, card.Owner, GameZone.Deck, card.Owner, GameZone.Hand, 0.25f, index);
            animation.Stackable = true;
            animationActions.Add(animation);

            DelayStartOfAnimation waitAnimation = new DelayStartOfAnimation(this, 0.1f);
            waitAnimation.Stackable = true;
            animationActions.Add(waitAnimation);

            if (index >= 0) index++;
        }

        return animationActions;
    }

}
