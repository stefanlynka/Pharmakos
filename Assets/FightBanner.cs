using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FightBanner : MonoBehaviour
{
    public RectTransform RectTransform;

    public TextMeshProUGUI BannerText;

    public void SetText(int fightNum)
    {
        BannerText.text = "Fight " + fightNum.ToString();
    }
}
