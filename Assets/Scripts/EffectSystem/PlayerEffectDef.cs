using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public abstract class PlayerEffect
{
    public abstract PlayerEffect DeepCopy(Player newOwner);

    public abstract void Apply();
    public abstract void Unapply(); // Currently not used. PlayerEffects are remade when deep copied so no need for now.

    public abstract PlayerEffectDescriptionData GetDescriptionData();
}

public abstract class RitualPlayerEffect : PlayerEffect
{
    public Player Owner;
    public Player Target;

    public override PlayerEffectDescriptionData GetDescriptionData()
    {
        PlayerEffectDescriptionData descriptionData = new PlayerEffectDescriptionData();
        string iconName = GetType().Name.TrimEnd("EffectDef");
        descriptionData.Icon = Resources.Load<Sprite>($"Images/Icons/Rituals/{iconName}");
        descriptionData.BuffIconHolder = Target;
        descriptionData.Description = GetDescription();

        return descriptionData;
    }
    protected virtual string GetDescription()
    {
        return "No Desc Found";
    }
}

public class PlayerEffectDescriptionData
{
    public Player BuffIconHolder; // Who has the buff icon appear next to them
    public string Description;
    public Sprite Icon;
}