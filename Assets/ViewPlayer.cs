using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewPlayer : ViewTarget
{
    public Player Player;
    public MeshRenderer Highlight;
    public ViewResources ViewResources;

    public void Load(Player player)
    {
        Player = player;
        Target = player;
    }

    public void SetHighlight(bool active)
    {
        Highlight.enabled = active;
    }

}
