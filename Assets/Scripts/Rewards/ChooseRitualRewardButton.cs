using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChooseRitualRewardButton : ViewTarget
{
    public MeshRenderer Highlight;

    public Ritual Ritual;
    public int RewardIndex = 0; // 0:FirstReward 1:SecondReward
    public int ReplacementIndex = 0; // 0:Top 1:Bottom
    public bool Selected = false;

    public void Init(Ritual ritual, int rewardIndex, int replacementIndex, Action<ViewTarget> onClick)
    {
        Ritual = ritual;
        RewardIndex = rewardIndex;
        ReplacementIndex = replacementIndex;

        OnClick = onClick;
    }

    public void SetHighlight(bool active)
    {
        Highlight.enabled = active;
        Selected = active;
    }


}
