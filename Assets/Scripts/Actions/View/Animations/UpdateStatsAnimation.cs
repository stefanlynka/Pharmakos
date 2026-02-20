using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdateStatsAnimation : AnimationAction
{
    //DealDamageAction dealDamageAction;
    private ITarget target;
    private int newAttack;
    private int newHealth;

    private ViewPlayer viewPlayer;
    private ViewFollower viewFollower;

    private bool playerDied = false;

    public UpdateStatsAnimation(GameAction gameAction, ITarget target, int newAttack, int newHealth) : base(gameAction)
    {
        this.target = target;
        this.newAttack = newAttack;
        this.newHealth = newHealth;

        Stackable = true;
    }

    public override void Play(Action onFinish = null)
    {
        base.Play(onFinish);

        Follower follower = target as Follower;
        Player player = target as Player;

        if (follower != null)
        {
            if (!View.Instance.TryGetViewFollower(follower, out viewFollower))
            {
                CallCallback();
                return;
            }
            viewFollower.SetStats(newAttack, newHealth);

            //if (newHealth < 0)
            //{
            //    viewFollower.ShowDamage(-newHealth);
            //    isDamage = true;
            //}
        }
        else if (player != null)
        {
            viewPlayer = View.Instance.GetViewPlayer(player);
            viewPlayer.SetHealth(newHealth);

            if (newHealth < 0)
            {
                //ScreenShakeHandler.Shake(0.2f, 0.3f);

                //viewPlayer.ShowDamage(-newHealth);
                //isDamage = true;

                if (viewPlayer.GetHealth() <= 0)
                {
                    playerDied = true;
                    //View.Instance.AudioHandler.PlaySoundEffect(AudioHandler.SoundEffectType.Defeat);
                    ScreenShakeHandler.Shake(1f, 0.2f);
                }
            }
        }


        CallCallback();
        //if (isDamage)
        //{
        //    damageDuration = View.Instance.IsHumansTurn ? 0.6f : 0.9f;
        //    Sequence moveSequence = new Sequence();
        //    moveSequence.Add(new Tween(TweenProgress, 0, 1, damageDuration));
        //    moveSequence.Add(new SequenceAction(Complete));
        //    moveSequence.Start();
        //}
        //else
        //{
        //    CallCallback();
        //}
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

        if (playerDied) Controller.Instance.CheckForPlayerDeath();
    }

    protected override void Log()
    {
        base.Log();
        if (target == null)
        {
            Debug.LogError("ChangeStatsAnimation: Target is null");
        }
        else
        {
            Debug.LogWarning("ChangeStatsAnimation: " + target.GetName() + ": " + newAttack + "/" + newHealth);
        }
    }
}
