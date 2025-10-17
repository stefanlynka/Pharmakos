using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class OfferingLabel : MonoBehaviour
{
    public GameObject SummaryObject;
    public TextMeshPro SummaryText;
    public TextMeshPro OfferingName;
    public TextMeshPro OfferingAmount;
    public SpriteRenderer Icon;

    // This function is called when the mouse enters the Collider.
    void OnMouseEnter()
    {
        if (SummaryObject != null)
        {
            SummaryObject.SetActive(true); // Enable the GameObject.
        }
    }

    // This function is called when the mouse exits the Collider.
    void OnMouseExit()
    {
        if (SummaryObject != null)
        {
            SummaryObject.SetActive(false); // Disable the GameObject.
        }
    }
}
