using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public abstract class Trinket
{
    public string Name;
    public string Description;
    public Player Owner;
    public StaticPlayerEffect MyEffect;

    public bool RepeatTrinket = true;
    public bool AvailableAsStartingTrinket = true;
    public OfferingType RelevantOffering = OfferingType.None;

    public abstract void ApplyEffect(Player owner);

    // revealWithAnimation keeps the icon hidden until the granting play's animation shows it.
    public virtual void ApplyEffect(Player owner, bool revealWithAnimation)
    {
        ApplyEffect(owner);
    }

    public virtual PlayerEffectDescriptionData GetDescriptionData()
    {
        PlayerEffectDescriptionData descriptionData = new PlayerEffectDescriptionData();
        string iconName = GetType().Name.TrimEnd("Trinket");
        descriptionData.Icon = Resources.Load<Sprite>($"Images/Icons/Trinkets/{iconName}");
        descriptionData.BuffIconHolder = Owner;
        descriptionData.Description = Description;
        return descriptionData;
    }

    public Trinket MakeBaseCopy()
    {
        // Get the type of the calling class
        Type callingType = this.GetType();

        // Create a new instance of the calling class
        return (Trinket)Activator.CreateInstance(callingType);
    }
}

public abstract class Trinket<T> : Trinket where T : StaticPlayerEffect
{
    public override void ApplyEffect(Player owner)
    {
        ApplyEffect(owner, false);
    }

    public override void ApplyEffect(Player owner, bool revealWithAnimation)
    {
        Owner = owner;

        // Create an instance of the StaticPlayerEffect using reflection
        MyEffect = (T)Activator.CreateInstance(typeof(T), owner);

        GameAction action = new ApplyTrinketAction(this, owner, revealWithAnimation);
        Owner.GameState.ActionHandler.AddAction(action);
    }
}
// ApplyTrinketAction