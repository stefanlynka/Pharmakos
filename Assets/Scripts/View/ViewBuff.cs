using System;
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
    public GameObject HighlightObject;

    public GameObject MultiplierObject;
    public TextMeshPro MultiplierText;

    private Action<ViewBuff> OnClick;

    public StaticPlayerEffect PlayerEffect;

    private bool isHighlighted = false;
    private int amount = 0;
    public void SetBuffData(StaticPlayerEffect playerEffect, PlayerEffectDescriptionData descriptionData)
    {
        PlayerEffect = playerEffect;
        Icon.sprite = descriptionData.Icon;
        SummaryText.text = descriptionData.Description;

        SetAmount(1);

        SetHighlight(false);
    }
    public void SetOnClick(Action<ViewBuff> onClick)
    {
        OnClick = onClick;
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

    private void OnMouseDown()
    {
        if (OnClick != null)
        {
            OnClick.Invoke(this);
        }
    }

    public void SetHighlight(bool value)
    {
        isHighlighted = value;

        HighlightObject.SetActive(value);
    }
    public bool IsHighlighted()
    {
        return isHighlighted;
    }

    public bool IsBuffCompatible(StaticPlayerEffect newEffect)
    {
        return PlayerEffect.GetType() == newEffect.GetType() 
            && PlayerEffect.Owner == newEffect.Owner
            && PlayerEffect.TargetPlayer == newEffect.TargetPlayer;
    }

    public void SetAmount(int newAmount)
    {
        amount = newAmount;

        MultiplierObject.SetActive(amount > 1);
        MultiplierText.text = "X" + amount.ToString();
    }
    public void IncreaseAmount()
    {
        SetAmount(amount+1);
    }
}
