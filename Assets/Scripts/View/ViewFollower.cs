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

    public GameObject DamageIcon;
    public TextMeshPro DamageText;

    private int attack = 0;
    private int health = 0;

    public override void Load(Card cardData, Action<ViewTarget> onClick = null)
    {
        base.Load(cardData, onClick);
        Follower = cardData as Follower;
        if (Follower == null) return;

        Target = Follower;
        SetStats(Follower.BaseAttack, Follower.BaseHealth);

        SummaryIcon.sprite = CardHandler.GetSummaryIcon(Follower);
        SummaryIcon.enabled = SummaryIcon.sprite != null;
        SummaryLeftArrow.enabled = Follower.AffectsAdjacent;
        SummaryRightArrow.enabled = Follower.AffectsAdjacent;

        DamageIcon.SetActive(false);

        Follower.OnChange -= CardChanged;
        Follower.OnChange += CardChanged;
        //Follower.OnRemove -= CardRemoved;
        //Follower.OnRemove += CardRemoved;
        //attack = Follower.GetCurrentAttack();
        //health = Follower.CurrentHealth;
    }

    // Currently Unused
    private void CardRemoved()
    {
        //View.Instance.RemoveViewCard(this);
    }

    private void CardChanged()
    {
        //AttackText.text = Follower.GetCurrentAttack().ToString();
        //HealthText.text = Follower.CurrentHealth.ToString();
    }

    public void ShowMaxStats()
    {
        attack = Follower.GetCurrentAttack();
        health = Follower.MaxHealth;

        AttackText.text = attack.ToString();
        HealthText.text = health.ToString();
    }

    public void SetStats(int newAttack, int newHealth)
    {
        attack = newAttack;
        AttackText.text = attack.ToString();
        health = newHealth;
        HealthText.text = health.ToString();
    }
    public void ChangeStats(int attackChange, int healthChange)
    {
        attack += attackChange;
        AttackText.text = attack.ToString();

        health += healthChange;
        HealthText.text = health.ToString();
    }
    public void ChangeHealth(int change)
    {
        health += change;
        HealthText.text = health.ToString();
    }

    public override void SetDescriptiveMode(bool value)
    {
        if (Card == null) return;

        ReminderTextBox.SetActive(Card.Text != string.Empty);

        base.SetDescriptiveMode(value);
    }

    public void ShowDamage(int damage)
    {
        DamageIcon.SetActive(true);
        DamageText.text = damage.ToString();
    }
    public void HideDamage()
    {
        DamageIcon.SetActive(false);
    }
}
