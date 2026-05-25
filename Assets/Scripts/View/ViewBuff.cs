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

    [Tooltip("Which side of this buff the hover summary popup appears on.")]
    public PopupPosition SummaryPosition = PopupPosition.Above;

    private Action<ViewBuff> OnClick;

    public StaticPlayerEffect PlayerEffect;

    private bool isHighlighted = false;
    private int amount = 0;
    private bool summaryForced = false;
    private string summaryDescription = string.Empty;

    public void SetBuffData(StaticPlayerEffect playerEffect, PlayerEffectDescriptionData descriptionData)
    {
        PlayerEffect = playerEffect;
        Icon.sprite = descriptionData.Icon;
        summaryDescription = descriptionData.Description;

        if (SummaryText != null)
            SummaryText.text = summaryDescription;

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

    public void SetSummaryShown(bool value)
    {
        if (summaryForced)
            return;

        if (value)
            ShowHoverSummary();
        else
            HideHoverSummary();
    }

    public void SetSummaryForced(bool value)
    {
        summaryForced = value;

        if (summaryForced)
            HideHoverSummary();

        if (SummaryObject != null)
            SummaryObject.SetActive(value);
    }

    void OnMouseEnter()
    {
        if (summaryForced)
            return;

        ShowHoverSummary();
    }

    void OnMouseExit()
    {
        if (summaryForced)
            return;

        HideHoverSummary();
    }

    void ShowHoverSummary()
    {
        if (string.IsNullOrEmpty(summaryDescription))
            return;

        PopupScreenHandler.Instance?.ShowTextPopup(summaryDescription, transform, SummaryPosition);
    }

    void HideHoverSummary()
    {
        PopupScreenHandler.Instance?.HideTextPopup(transform);
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

    void OnDisable()
    {
        HideHoverSummary();
    }
}
