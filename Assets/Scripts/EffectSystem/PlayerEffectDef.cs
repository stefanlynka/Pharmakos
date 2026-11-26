using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerEffect
{
    public abstract PlayerEffect DeepCopy(Player newOwner);

    public abstract void Apply();
    public abstract void Unapply(); // Currently not used. PlayerEffects are remade when deep copied so no need for now.

    public abstract PlayerEffectDescriptionData GetDescriptionData();
}

public abstract class StaticPlayerEffect : PlayerEffect
{
    public static event Action<StaticPlayerEffect> Triggered;

    public Player Owner;
    public Player TargetPlayer;

    /// <summary>
    /// Call when the effect acts without an action of its own (e.g. changes an offering directly): flares its icon
    /// and adds it to the play history. Effects that act through actions set the action's SourceEffect instead.
    /// </summary>
    public void MarkTriggered()
    {
        if (Owner?.GameState == null || Owner.GameState.IsSimulated)
            return;

        PlayHistoryHandler history = PlayHistoryHandler.For(Owner.GameState);
        history?.AddTriggeredRecord(history.GetEffectRecord(this));

        Triggered?.Invoke(this);
    }

    /// <summary>Flares the icon only: an action with this SourceEffect is animating (its history is already recorded).</summary>
    public void FlareTriggered()
    {
        if (Owner?.GameState == null || Owner.GameState.IsSimulated)
            return;

        Triggered?.Invoke(this);
    }

    public override PlayerEffectDescriptionData GetDescriptionData()
    {
        PlayerEffectDescriptionData descriptionData = new PlayerEffectDescriptionData();
        string iconName = GetType().Name.TrimEnd("EffectDef");
        descriptionData.Icon = Resources.Load<Sprite>($"Images/Icons/Rituals/{iconName}");
        descriptionData.BuffIconHolder = TargetPlayer;
        descriptionData.Description = GetDescription();

        return descriptionData;
    }
    protected virtual string GetDescription()
    {
        return "No Desc Found";
    }

    public StaticPlayerEffect MakeBaseCopy()
    {
        // Get the type of the calling class
        Type callingType = this.GetType();

        // Create a new instance of the calling class
        return (StaticPlayerEffect)Activator.CreateInstance(callingType);
    }
}
public abstract class TrinketPlayerEffect : StaticPlayerEffect
{
    public override PlayerEffectDescriptionData GetDescriptionData()
    {
        PlayerEffectDescriptionData descriptionData = new PlayerEffectDescriptionData();
        string iconName = GetType().Name.TrimEnd("TrinketEffectDef");
        descriptionData.Icon = Resources.Load<Sprite>($"Images/Icons/Trinkets/{iconName}");
        descriptionData.BuffIconHolder = TargetPlayer;
        descriptionData.Description = GetDescription();

        return descriptionData;
    }
}

public class PlayerEffectDescriptionData
{
    public Player BuffIconHolder; // Who has the buff icon appear next to them
    public string Description;
    public Sprite Icon;
    public Card HoverCard;
}