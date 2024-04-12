using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Card : ITarget
{
    public Player Owner = null;

    public Dictionary<OfferingType, int> Costs = new Dictionary<OfferingType, int>();

    public string Text = "";

    public virtual void Reset() { }

    public abstract List<ITarget> GetPlayableTargets();

    public virtual void Play(ITarget target) 
    {
        //PayCosts();
    }
    public void PayCosts()
    {
        Owner.PayCosts(this);
    }

    protected Card MakeBaseCopy()
    {
        // Get the type of the calling class
        Type callingType = this.GetType();

        // Create a new instance of the calling class
        return (Card)Activator.CreateInstance(callingType);
    }
}

public interface ITarget
{

}