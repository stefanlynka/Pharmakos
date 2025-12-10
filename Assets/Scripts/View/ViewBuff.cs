using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ViewBuff : MonoBehaviour
{
    public GameObject ParentObject;
    public GameObject SummaryObject;
    public TextMeshPro SummaryText;
    public SpriteRenderer Icon;

    public void SetBuffData(PlayerEffectDescriptionData descriptionData)
    {
        Icon.sprite = descriptionData.Icon;
        SummaryText.text = descriptionData.Description;
    }

    public void SetVisible(bool value)
    {
        ParentObject.SetActive(value);
    }

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
