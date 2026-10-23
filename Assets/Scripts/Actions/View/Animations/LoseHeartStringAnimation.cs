using System;
using UnityEngine;

public class LoseHeartStringAnimation : AnimationAction
{
    private Player player;
    private ViewPlayer viewPlayer;
    private float duration = 0.8f;

    public LoseHeartStringAnimation(GameAction gameAction, Player player) : base(gameAction)
    {
        this.player = player;
    }

    public override void Play(Action onFinish = null)
    {
        base.Play(onFinish);

        viewPlayer = View.Instance.GetViewPlayer(player);
        if (viewPlayer == null)
        {
            CallCallback();
            return;
        }

        viewPlayer.SetHeartStrings(player.CurrentHeartStrings, Player.MaxHeartStrings);
        ScreenShakeHandler.Shake(0.5f, 0.25f);
        PlayStrum();

        VfxLibrary.PortraitHeartstringFlash(duration).Play(new VfxContext(null, viewPlayer), Complete);
    }

    void PlayStrum()
    {
        if (View.Instance == null || View.Instance.AudioHandler == null)
            return;

        View.Instance.AudioHandler.PlayHeartstringStrum();
    }

    private void Complete()
    {
        CallCallback();
    }

    protected override void Log()
    {
        Debug.LogWarning("LoseHeartStringAnimation: " + player.GetName());
    }
}
