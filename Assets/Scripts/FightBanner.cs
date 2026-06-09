using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FightBanner : MonoBehaviour
{
    public RectTransform RectTransform;

    public TextMeshProUGUI BannerText;

    public void SetText(string fightName)
    {
        BannerText.text = fightName;
    }
}
