using System;
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
    public MeshHighlightEffect MeshHighlightEffect;

    public void Load(Player player)
    {
        Player = player;
        Target = player;

        PlayerChanged(0);

        player.OnHealthChange -= PlayerChanged;
        player.OnHealthChange += PlayerChanged;

        ViewResources.Init(Player);

        HandHandler.Scale = View.CombatCardScale;
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
        HandHandler.UpdateKeywordReminders();
        ViewMajorRitual.UpdateRitual();
        ViewMinorRitual.UpdateRitual();
    }

    public void SetHighlight(bool active)
    {
        if (MeshHighlightEffect != null)
        {
            if (ViewPlayerPortrait != null && ViewPlayerPortrait.Highlight != null)
                ViewPlayerPortrait.Highlight.enabled = false;

            MeshHighlightEffect.SetActive(active);
            return;
        }

        if (ViewPlayerPortrait != null && ViewPlayerPortrait.Highlight != null)
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
    public void SetHeartStrings(int current, int max)
    {
        ViewPlayerPortrait.SetHeartStrings(current, max);
    }
    public int GetHealth()
    {
        return ViewPlayerPortrait.GetHealth();
    }
    public void FlareBuff(StaticPlayerEffect effect)
    {
        if (effect == null)
            return;

        foreach (ViewBuff buff in Buffs)
        {
            if (buff == null || buff.PlayerEffect == null || !buff.ParentObject.activeSelf)
                continue;

            if (!buff.IsBuffCompatible(effect))
                continue;

            buff.FlareHighlight();
            return;
        }
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

    // Same fire brushstroke flurry (and FlameStart sound) used when a card is sacrificed at a temple.
    // Holds until the strokes have finished so the caller can wait before leaving the fight.
    // The portrait art is tilted, so the flurry is parented to that sprite and lifted along its facing.
    public void PlayDeathFlames(Action onComplete)
    {
        BrushStrokeFlurryVfx fire = VfxLibrary.FireBrushStrokes();
        fire.QueueHold = fire.Duration + fire.StrokeLifetime;

        SpriteRenderer portrait = ViewPlayerPortrait != null ? ViewPlayerPortrait.PortraitRenderer : null;
        if (portrait != null)
        {
            fire.AnchorOverride = portrait.transform;
            fire.Lift = 0.2f;
            if (portrait.sprite != null)
            {
                Bounds bounds = portrait.sprite.bounds;
                fire.AreaCenter = bounds.center;
                fire.AreaSize = new Vector2(bounds.size.x, bounds.size.y);
            }
        }

        fire.Play(new VfxContext(null, this), onComplete);
    }
}
