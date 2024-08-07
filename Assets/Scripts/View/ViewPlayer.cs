using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

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

}
