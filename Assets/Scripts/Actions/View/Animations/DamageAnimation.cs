using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.VisualScripting.Member;
using static UnityEngine.GraphicsBuffer;

public class DamageAnimation : AnimationAction
{
    DealDamageAction dealDamageAction;
    ITarget target;
    public DamageAnimation(GameAction gameAction, ITarget target) : base(gameAction)
    {
        if (gameAction is DealDamageAction) dealDamageAction = (DealDamageAction)gameAction;
        this.target = target;
        // Setup Tweens
    }

    public override void Play(Action onFinish = null)
    {
        base.Play(onFinish);

        Follower follower = target as Follower;
        Player player = target as Player;

        if (follower != null)
        {
            if (!View.Instance.TryGetViewFollower(follower, out ViewFollower viewFollower))
            {
                CallCallback();
                return;
            }
            viewFollower.ChangeHealth(-dealDamageAction.Damage);
            //follower.ChangeHealth(Source, -Damage);
        }
        else if (player != null)
        {
            ViewPlayer viewPlayer = View.Instance.GetViewPlayer(player);
            viewPlayer.ChangeHealth(-dealDamageAction.Damage);
        }
        

        CallCallback();
        // Get ViewCards
        // TODO: Add unique int IDs per card. Create dictionaries pointing to those cards and their viewCards. Dictionary<int id, Card Card>
    }

 
}
