using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.VisualScripting.Member;
using static UnityEngine.GraphicsBuffer;

public class ChangeStatsAnimation : AnimationAction
{
    //DealDamageAction dealDamageAction;
    private ITarget target;
    private int attackChange;
    private int healthChange;

    private ViewFollower viewFollower;
    private float damageDuration = 1.25f;

    public ChangeStatsAnimation(GameAction gameAction, ITarget target, int attackChange, int healthChange) : base(gameAction)
    {
        this.target = target;
        this.attackChange = attackChange;
        this.healthChange = healthChange;

        Stackable = true;
    }

    public override void Play(Action onFinish = null)
    {
        base.Play(onFinish);

        Follower follower = target as Follower;
        Player player = target as Player;

        bool isFollowerDamage = false;

        if (follower != null)
        {
            if (!View.Instance.TryGetViewFollower(follower, out viewFollower))
            {
                CallCallback();
                return;
            }
            //viewFollower.
            viewFollower.ChangeStats(attackChange, healthChange);

            if (healthChange < 0)
            {
                viewFollower.ShowDamage(-healthChange);
                isFollowerDamage = true;
            }
            //follower.ChangeHealth(Source, -Damage);
        }
        else if (player != null)
        {
            ViewPlayer viewPlayer = View.Instance.GetViewPlayer(player);
            viewPlayer.ChangeHealth(healthChange);
        }


        if (isFollowerDamage)
        {
            
            Sequence moveSequence = new Sequence();
            moveSequence.Add(new Tween(TweenProgress, 0, 1, damageDuration));
            moveSequence.Add(new SequenceAction(Complete));
            moveSequence.Start();
        }
        else
        {
            CallCallback();
        }
    }

    private void TweenProgress(float progress)
    {
        // Do nothing
    }

    private void Complete()
    {
        viewFollower.HideDamage();
        CallCallback();
    }
}
