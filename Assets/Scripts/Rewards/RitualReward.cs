using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RitualReward : MonoBehaviour
{
    public ViewRitual ViewRitual;
    public ChooseRitualRewardButton ChooseTopRitualButton;
    public ChooseRitualRewardButton ChooseBottomRitualButton;

    public int RewardIndex = 0;

    public void Load(Ritual ritual, int rewardIndex, Action<ViewTarget> onClick)
    {
        RewardIndex = rewardIndex;

        ViewRitual.Init(ritual);

        ChooseTopRitualButton.Init(ritual, rewardIndex, 0, onClick);
        ChooseBottomRitualButton.Init(ritual, rewardIndex, 1, onClick);
    }
}
