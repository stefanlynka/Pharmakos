using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewOfferingCost : MonoBehaviour
{
    public ViewCostItem GoldCost;
    public List<ViewCostItem> CostItems = new List<ViewCostItem>();

    public void Load(Dictionary<OfferingType, int> costs)
    {
        for (int i = 0; i < CostItems.Count; i++)
        {
            CostItems[i].gameObject.SetActive(false);
        }

        int index = 0;
        foreach (KeyValuePair<OfferingType, int> kvp in costs)
        {
            if (kvp.Key == OfferingType.Gold)
            {
                GoldCost.Load(OfferingType.Gold, kvp.Value);
            }
            else if (kvp.Value > 0)
            {
                CostItems[index].gameObject.SetActive(true);
                CostItems[index].Load(kvp.Key, kvp.Value);
                index++;
            }
        }
    }
    
}
