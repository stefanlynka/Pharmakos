using System;
using System.Collections.Generic;

// Shows a trinket icon that was granted earlier in the turn but kept hidden until this animation.
public class RevealTrinketAnimation : AnimationAction
{
    readonly StaticPlayerEffect effect;

    public Player Owner => effect != null ? effect.Owner : null;

    public RevealTrinketAnimation(GameAction gameAction, StaticPlayerEffect effect) : base(gameAction)
    {
        this.effect = effect;
    }

    public override void Play(Action onFinish = null)
    {
        base.Play(onFinish);
        View.Instance.RevealBuff(effect);
        CallCallback();
    }
}

// Plays the queued trinket reveal as part of Twist of Fate, once that card is on screen.
public class RevealPlayedTrinketAnimation : AnimationAction
{
    readonly Player owner;

    public RevealPlayedTrinketAnimation(GameAction gameAction, Player owner) : base(gameAction)
    {
        this.owner = owner;
    }

    public override void Play(Action onFinish = null)
    {
        base.Play(onFinish);

        if (View.Instance == null)
        {
            CallCallback();
            return;
        }

        List<AnimationAction> reveals = View.Instance.AnimationHandler.TakeQueuedAnimations(
            animationAction => animationAction is RevealTrinketAnimation reveal && reveal.Owner == owner,
            animationAction => animationAction is MoveCardAnimation);

        if (reveals.Count == 0)
        {
            CallCallback();
            return;
        }

        PlayNext(reveals, 0);
    }

    void PlayNext(List<AnimationAction> reveals, int index)
    {
        if (index >= reveals.Count)
        {
            CallCallback();
            return;
        }

        reveals[index].Play(() => PlayNext(reveals, index + 1));
    }
}
