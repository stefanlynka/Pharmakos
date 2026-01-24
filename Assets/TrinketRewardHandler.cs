using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrinketRewardHandler : MonoBehaviour
{
    public List<ViewBuff> ViewBuffs = new List<ViewBuff>();
    public ViewRitual MajorRitual;
    public ViewRitual MinorRitual;

    private List<Trinket> trinkets;

    public void Load()
    { 
        // Load Trinkets
        trinkets = Controller.Instance.ProgressionHandler.GetRandomTrinkets();

        ViewBuffs[0].SetBuffData(trinkets[0].MyEffect, trinkets[0].GetDescriptionData());
        ViewBuffs[1].SetBuffData(trinkets[1].MyEffect, trinkets[1].GetDescriptionData());
        ViewBuffs[2].SetBuffData(trinkets[2].MyEffect, trinkets[2].GetDescriptionData());

        foreach (ViewBuff viewBuff in ViewBuffs)
        {
            viewBuff.SetHighlight(false);
            viewBuff.SetOnClick(ViewBuffClicked);
        }

        ViewBuffs[0].SetHighlight(true);

        // Load Rituals, just for reference
        MajorRitual.Init(Controller.Instance.HumanPlayerDetails.MajorRituals[0]);
        MinorRitual.Init(Controller.Instance.HumanPlayerDetails.MinorRituals[0]);
    }

    private Trinket GetSelectedTrinket()
    {
        for (int i =0; i < 3; i++)
        {
            ViewBuff viewBuff = ViewBuffs[i];
            if (viewBuff.IsHighlighted()) return trinkets[i];
        }

        return null;
    }

    public void Continue()
    {
        // Add trinket
        Controller.Instance.AddTrinket(GetSelectedTrinket());

        View.Instance.Clear();

        //Controller.Instance.StartNextLevel();
        Controller.Instance.GoToCardGainRewardScreen();
        gameObject.SetActive(false);
    }

    public void ViewBuffClicked(ViewBuff clickedViewBuff)
    {
        foreach (ViewBuff viewBuff in ViewBuffs)
        {
            viewBuff.SetHighlight(false);

            if (viewBuff == clickedViewBuff)
            {
                viewBuff.SetHighlight(true);
            }
        }
    }
}
