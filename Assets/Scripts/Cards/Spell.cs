using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spell : Card, ITarget
{
    public bool HasTargets = true;
    public override List<ITarget> GetPlayableTargets()
    {
        return new List<ITarget>();
    }

    public override void Play(ITarget target)
    {
        base.Play(target);

        Controller.Instance.CurrentPlayer.ChangeOffering(OfferingType.Scroll, 1);
    }

    public bool HasPlayableTargets()
    {
        return !HasTargets || GetPlayableTargets().Count > 0;
    }

}
