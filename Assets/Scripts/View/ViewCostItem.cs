using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ViewCostItem : MonoBehaviour
{
    public TextMeshPro CostText;
    public SpriteRenderer OutlineRenderer;

    OfferingType offeringType;
    int offeringCost;

    public void Load(OfferingType type, int cost)
    {
        offeringType = type;
        offeringCost = cost;

        OutlineRenderer.sprite = OfferingHandler.Instance.GetOfferingSprite(type);
        CostText.text = cost.ToString();
    }

}
