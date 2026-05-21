using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ViewPlayerPortrait : MonoBehaviour
{
    public MeshRenderer Highlight;
    public GameObject HealthObject;
    public TextMeshPro HealthText;
    public GameObject DamageIcon;
    public TextMeshPro DamageText;
    public SpriteRenderer PortraitRenderer;
    private int health = 0;
    public GameObject HeartStringHolder;
    public List<GameObject> HeartStrings = new List<GameObject>();

    private Player player;

    public void Load(Player player)
    {
        this.player = player;

        SetHealthVisible(true);

        PortraitRenderer.sprite = Resources.Load<Sprite>("Images/Portraits/" + player.PlayerDetails.PortraitName);

        if (player.CurrentHeartStrings > 0)
        {
            SetHeartStrings(player.CurrentHeartStrings, Player.MaxHeartStrings);
        }
        else if (HeartStringHolder != null)
        {
            HeartStringHolder.SetActive(false);
        }
    }

    public void SetHeartStrings(int current, int max)
    {
        if (HeartStringHolder != null)
        {
            HeartStringHolder.SetActive(current > 0);
        }

        for (int i = 0; i < HeartStrings.Count; i++)
        {
            if (HeartStrings[i] != null)
            {
                HeartStrings[i].SetActive(i < current);
            }
        }
    }

    public void ChangeHealth(int change)
    {
        health += change;
        HealthText.text = health.ToString();
    }
    public void SetHealth(int newHealth)
    {
        health = newHealth;
        HealthText.text = health.ToString();
    }
    public int GetHealth()
    {
        return health;
    }
    public void SetHealthVisible(bool value)
    {
        HealthObject.SetActive(value);
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
