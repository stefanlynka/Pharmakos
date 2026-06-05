using System;
using UnityEngine;

/// <summary>
/// Single ViewTarget on pooled CardView instances. Routes input to whichever
/// ViewCard subtype is active (ViewFollower or ViewSpell).
/// </summary>
public class CardViewRaycastTarget : ViewTarget
{
    ViewCard activeCard;

    public ViewCard ActiveCard => activeCard;

    public static ViewCard ResolveViewCard(ViewTarget target)
    {
        if (target is ViewCard viewCard)
            return viewCard;

        if (target is CardViewRaycastTarget proxy)
            return proxy.ActiveCard;

        return null;
    }

    public void SetActiveCard(ViewCard card)
    {
        activeCard = card;
        Target = card != null ? card.Target : null;
        OnClick = card != null
            ? (Action<ViewTarget>)(_ => card.OnClick?.Invoke(card))
            : null;
    }

    public void Clear()
    {
        SetActiveCard(null);
    }
}
