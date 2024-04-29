using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ritual : ITarget
{
    public Player Owner;
    public Dictionary<OfferingType, int> Costs = new Dictionary<OfferingType, int>();
    public string Description = "No Desc";

    public Ritual(Player player)
    {
        Owner = player;
    }
    public virtual List<ITarget> GetTargets()
    {
        var targets = new List<ITarget>();

        return targets;
    }

    public bool CanPlay()
    {
        return Owner.CanPlayRitual(this);
    }

    public virtual void Play(ITarget target)
    {

    }
}
