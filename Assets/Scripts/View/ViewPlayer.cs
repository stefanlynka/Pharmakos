using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ViewPlayer : ViewTarget
{
    public ViewPlayerPortrait ViewPlayerPortrait;

    public Player Player;
    public ViewResources ViewResources;

    public ViewHandHandler HandHandler;

    public ViewBattleRow BattleRow;

    public ViewRitual ViewMinorRitual;
    public ViewRitual ViewMajorRitual;

    public List<ViewBuff> Buffs = new List<ViewBuff>();

    public void Load(Player player)
    {
        Player = player;
        Target = player;

        PlayerChanged(0);

        player.OnHealthChange -= PlayerChanged;
        player.OnHealthChange += PlayerChanged;

        ViewResources.Init(Player);

        BattleRow.Setup(Player.BattleRow);

        ViewMinorRitual.Init(Player.MinorRitual);
        ViewMajorRitual.Init(Player.MajorRitual);

        SetHealth(player.Health);

        ViewPlayerPortrait.Load(player);
    }

    public void Clear()
    {
        HandHandler.Clear();
        BattleRow.Clear();

        ViewMinorRitual.Init(null);
        ViewMajorRitual.Init(null);

        ClearBuffs();
    }

    public void UpdatePlayer()
    {
        HandHandler.UpdateHand();
        BattleRow.UpdateRow();
        ViewMajorRitual.UpdateRitual();
        ViewMinorRitual.UpdateRitual();
    }

    public void SetHighlight(bool active)
    {
        ViewPlayerPortrait.Highlight.enabled = active;
    }

    private void PlayerChanged(int healthChange)
    {
        //HealthText.text = Player.Health.ToString();
    }
    public void ChangeHealth(int change)
    {
        ViewPlayerPortrait.ChangeHealth(change);
    }
    public void SetHealth(int newHealth)
    {
        ViewPlayerPortrait.SetHealth(newHealth);
    }
    public int GetHealth()
    {
        return ViewPlayerPortrait.GetHealth();
    }
    public void ClearBuffs()
    {
        foreach (ViewBuff buff in Buffs)
        {
            buff.SetVisible(false);
        }
    }
    public void AddBuff(StaticPlayerEffect playerEffect)
    {
        foreach (ViewBuff buff in Buffs)
        {
            if (buff.ParentObject.activeSelf)
            {
                if (buff.IsBuffCompatible(playerEffect))
                {
                    buff.IncreaseAmount();
                    return;
                }
            }
            else
            {
                PlayerEffectDescriptionData descriptionData = playerEffect.GetDescriptionData();
                buff.SetBuffData(playerEffect, descriptionData);
                buff.SetVisible(true);
                return;
            }
        }
    }

    public void ShowDamage(int damage)
    {
        ViewPlayerPortrait.ShowDamage(damage);
    }
    public void HideDamage()
    {
        ViewPlayerPortrait.HideDamage();
    }
}
