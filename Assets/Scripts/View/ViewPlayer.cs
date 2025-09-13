using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ViewPlayer : ViewTarget
{
    public Player Player;
    public MeshRenderer Highlight;
    public ViewResources ViewResources;

    public TextMeshPro HealthText;

    public ViewHandHandler HandHandler;

    public ViewBattleRow BattleRow;

    public ViewRitual ViewMinorRitual;
    public ViewRitual ViewMajorRitual;

    public List<ViewBuff> Buffs = new List<ViewBuff>();

    public void Load(Player player)
    {
        Player = player;
        Target = player;

        PlayerChanged();

        player.OnHealthChange -= PlayerChanged;
        player.OnHealthChange += PlayerChanged;

        ViewResources.Init(Player);

        BattleRow.Setup(Player.BattleRow);

        ViewMinorRitual.Init(Player.MinorRitual);
        ViewMajorRitual.Init(Player.MajorRitual);
    }

    public void Clear()
    {
        HandHandler.Clear();
        BattleRow.Clear();

        ViewMinorRitual.Init(null);
        ViewMajorRitual.Init(null);
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
        Highlight.enabled = active;
    }

    private void PlayerChanged()
    {
        HealthText.text = Player.Health.ToString();
    }

    public void ClearBuffs()
    {
        foreach (ViewBuff buff in Buffs)
        {
            buff.gameObject.SetActive(false);
        }
    }
    public void AddBuff(PlayerEffect playerEffect)
    {
        foreach (ViewBuff buff in Buffs)
        {
            if (!buff.gameObject.activeSelf)
            {
                PlayerEffectDescriptionData descriptionData = playerEffect.GetDescriptionData();
                buff.Icon.sprite = descriptionData.Icon;
                buff.SummaryText.text = descriptionData.Description;
                buff.gameObject.SetActive(true);
                return;
            }
        }
    }
}
