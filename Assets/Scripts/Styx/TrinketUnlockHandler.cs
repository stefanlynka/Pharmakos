using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen reveal shown the first time the player defeats the Fates or Gate boss,
/// presenting the permanently unlocked Styx trinket. The layout is authored in the
/// scene under the TrinketUnlockScreen GameObject; this handler only fills in the
/// trinket icon, name, and description.
/// </summary>
public class TrinketUnlockHandler : MonoBehaviour
{
    [Header("Scene References")]
    public Image IconImage;
    public TextMeshProUGUI NameText;
    public TextMeshProUGUI DescriptionText;
    public Button ContinueButton;

    Action onContinue;

    void Awake()
    {
        ContinueButton.onClick.AddListener(Continue);
    }

    public void Show(Trinket trinket, Action onContinueCallback)
    {
        onContinue = onContinueCallback;

        PlayerEffectDescriptionData descriptionData = trinket.GetDescriptionData();
        IconImage.sprite = descriptionData != null ? descriptionData.Icon : null;
        IconImage.color = IconImage.sprite != null ? Color.white : StyxUI.IconFallbackColor;

        NameText.text = trinket.Name;
        DescriptionText.text = trinket.Description;
    }

    public void Continue()
    {
        Action callback = onContinue;
        onContinue = null;
        if (callback == null) return;

        if (Controller.Instance != null)
            Controller.Instance.ContinueFromTrinketUnlockScreen(callback);
        else
            callback.Invoke();
    }
}
