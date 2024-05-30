using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class DealDamageAction : GameAction
{
    public Player Source;
    public ITarget Target;
    public int Damage;

    public DealDamageAction(Player source, ITarget target, int damage)
    {
        Source = source;
        Target = target;
        Damage = damage;
    }

    public override void Execute(bool simulated = false)
    {
        Follower follower = (Follower)Target;
        Player player = (Player)Target;

        if (follower != null )
        {
            follower.ChangeHealth(-Damage);
        }
        else if (player != null )
        {
            player.ChangeHealth(Damage);
        }

        if (!simulated) View.Instance.AnimationHandler.AddGameActionToQueue(this);
    }

    public override List<AnimationAction> GetAnimationActions()
    {
        List<AnimationAction> animationActions = new List<AnimationAction>(AnimationActions)
        {
            new MoveAnimation(this)
        };
        //if (LastStep) animationActions.Add(new IdleAnimation(this));
        return animationActions;
    }

}
