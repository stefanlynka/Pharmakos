using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static Unity.VisualScripting.Member;
using static UnityEngine.GraphicsBuffer;

public class ChangeStatsAnimation : AnimationAction
{
    //DealDamageAction dealDamageAction;
    private ITarget target;
    private int attackChange;
    private int healthChange;

    private ViewPlayer viewPlayer;
    private ViewFollower viewFollower;
    private float damageDuration = 0.6f;

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

        bool isDamage = false;

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
                isDamage = true;
            }
            //follower.ChangeHealth(Source, -Damage);
        }
        else if (player != null)
        {
            viewPlayer = View.Instance.GetViewPlayer(player);
            viewPlayer.ChangeHealth(healthChange);

            if (healthChange < 0)
            {
                viewPlayer.ShowDamage(-healthChange);
                isDamage = true;
            }
        }


        if (isDamage)
        {
            damageDuration = View.Instance.IsHumansTurn ? 0.6f : 0.9f;
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
        viewPlayer?.HideDamage();
        viewFollower?.HideDamage();
        CallCallback();
    }

    protected override void Log()
    {
        base.Log();
        Debug.LogWarning("ChangeStatsAnimation: " + target.GetName() + ": " + attackChange + "/" + healthChange);
    }
}
