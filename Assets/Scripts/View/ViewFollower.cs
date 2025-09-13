using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ViewFollower : ViewCard
{
    public TextMeshPro AttackText;
    public TextMeshPro HealthText;

    public SpriteRenderer SkullRenderer;

    public GameObject ReminderTextBox;

    public Follower Follower;

    public SpriteRenderer SummaryIcon;
    public SpriteRenderer SummaryLeftArrow;
    public SpriteRenderer SummaryRightArrow;

    public override void Load(Card cardData, Action<ViewTarget> onClick = null)
    {
        base.Load(cardData, onClick);
        Follower = cardData as Follower;
        if (Follower == null) return;

        Target = Follower;
        AttackText.text = Follower.GetCurrentAttack().ToString();
        HealthText.text = Follower.CurrentHealth.ToString();

        SummaryIcon.sprite = CardHandler.GetSummaryIcon(Follower);
        SummaryIcon.enabled = SummaryIcon.sprite != null;
        SummaryLeftArrow.enabled = Follower.AffectsAdjacent;
        SummaryRightArrow.enabled = Follower.AffectsAdjacent;

        Follower.OnChange -= CardChanged;
        Follower.OnChange += CardChanged;
        //Follower.OnRemove -= CardRemoved;
        //Follower.OnRemove += CardRemoved;
    }

    // Currently Unused
    private void CardRemoved()
    {
        //View.Instance.RemoveViewCard(this);
    }

    private void CardChanged()
    {
        AttackText.text = Follower.GetCurrentAttack().ToString();
        HealthText.text = Follower.CurrentHealth.ToString();
    }

    public override void SetDescriptiveMode(bool value)
    {
        if (Card == null) return;

        ReminderTextBox.SetActive(Card.Text != string.Empty);

        base.SetDescriptiveMode(value);
    }
}
