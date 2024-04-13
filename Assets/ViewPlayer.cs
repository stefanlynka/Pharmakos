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

    public void Load(Player player)
    {
        Player = player;
        Target = player;

        PlayerChanged();

        player.OnChange -= PlayerChanged;
        player.OnChange += PlayerChanged;
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
