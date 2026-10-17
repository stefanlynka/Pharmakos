using System.Collections.Generic;
using UnityEngine;

public class TrinketRewardHandler : MonoBehaviour
{
    public List<ViewBuff> ViewBuffs = new List<ViewBuff>();

    List<Trinket> trinkets = new List<Trinket>();
    bool continuing;

    public void Load()
    {
        continuing = false;
        trinkets = Controller.Instance.ProgressionHandler.GetRandomShopTrinkets(ViewBuffs.Count);

        for (int i = 0; i < ViewBuffs.Count; i++)
        {
            ViewBuff viewBuff = ViewBuffs[i];
            if (viewBuff == null)
                continue;

            bool hasTrinket = i < trinkets.Count && trinkets[i] != null;
            viewBuff.SetHighlight(false);
            viewBuff.SetVisible(hasTrinket);
            if (!hasTrinket)
            {
                viewBuff.SetOnClick(null);
                continue;
            }

            viewBuff.SetBuffData(trinkets[i].MyEffect, trinkets[i].GetDescriptionData());
            viewBuff.SetOnClick(ViewBuffClicked);
        }

        if (trinkets.Count > 0 && ViewBuffs.Count > 0 && ViewBuffs[0] != null)
            ViewBuffs[0].SetHighlight(true);
    }

    Trinket GetSelectedTrinket()
    {
        for (int i = 0; i < ViewBuffs.Count && i < trinkets.Count; i++)
        {
            ViewBuff viewBuff = ViewBuffs[i];
            if (viewBuff != null && viewBuff.IsHighlighted())
                return trinkets[i];
        }

        return trinkets.Count > 0 ? trinkets[0] : null;
    }

    public void Continue()
    {
        if (continuing)
            return;

        continuing = true;

        Trinket selected = GetSelectedTrinket();
        if (selected != null)
            Controller.Instance.AddTrinket(selected.MakeBaseCopy());

        Controller.Instance.ContinueFromTrinketReward();
    }

    public void Hide()
    {
        foreach (ViewBuff viewBuff in ViewBuffs)
        {
            if (viewBuff == null)
                continue;

            viewBuff.SetHighlight(false);
            viewBuff.SetOnClick(null);
            viewBuff.SetVisible(false);
        }

        gameObject.SetActive(false);
    }

    public void ViewBuffClicked(ViewBuff clickedViewBuff)
    {
        if (continuing || clickedViewBuff == null)
            return;

        foreach (ViewBuff viewBuff in ViewBuffs)
        {
            if (viewBuff == null)
                continue;

            viewBuff.SetHighlight(viewBuff == clickedViewBuff);
        }
    }
}
