using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeStatsAnimation : AnimationAction
{
    //DealDamageAction dealDamageAction;
    private ITarget target;
    private int attackChange;
    private int healthChange;

    private ViewPlayer viewPlayer;
    private ViewFollower viewFollower;
    private float damageDuration = 0.6f;

    private bool playerDied = false;
    private bool playDeathFlames = false;

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
        bool waitingOnStatChange = false;

        if (follower != null)
        {
            if (!View.Instance.TryGetViewFollower(follower, out viewFollower))
            {
                CallCallback();
                return;
            }
            viewFollower.ChangeStats(attackChange, healthChange);

            if (healthChange < 0 && GameAction is DealDamageAction)
            {
                viewFollower.ShowDamage(-healthChange);
                isDamage = true;
            }
            else
            {
                waitingOnStatChange = viewFollower.PlayStatChangeFeedback(attackChange, healthChange, CallCallback);
            }
        }
        else if (player != null)
        {
            viewPlayer = View.Instance.GetViewPlayer(player);
            int previousHealth = viewPlayer.GetHealth();
            viewPlayer.ChangeHealth(healthChange);

            if (healthChange < 0)
            {
                //ScreenShakeHandler.Shake(0.2f, 0.3f);

                viewPlayer.ShowDamage(-healthChange);
                isDamage = true;

                if (previousHealth > 0 && viewPlayer.GetHealth() <= 0)
                {
                    playerDied = true;
                    //View.Instance.AudioHandler.PlaySoundEffect(AudioHandler.SoundEffectType.Defeat);
                    ScreenShakeHandler.Shake(1f, 0.2f);
                    playDeathFlames = !player.IsHuman;
                }
            }
        }


        if (isDamage)
        {
            damageDuration = View.Instance.IsHumansTurn ? 0.8f : 1.2f;
            int pending = 1;
            Action finishOne = () =>
            {
                pending--;
                if (pending == 0) Complete();
            };

            if (playDeathFlames)
            {
                pending++;
                viewPlayer.PlayDeathFlames(finishOne);
            }

            Sequence moveSequence = new Sequence();
            moveSequence.Add(new Tween(TweenProgress, 0, 1, damageDuration));
            moveSequence.Add(new SequenceAction(finishOne));
            moveSequence.Start();
        }
        else if (!waitingOnStatChange)
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
            Debug.LogWarning("ChangeStatsAnimation: " + target.GetName() + ": " + attackChange + "/" + healthChange);
        }
    }
}
