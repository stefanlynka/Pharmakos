using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen reveal shown the first time the player defeats the Fates or Gate boss,
/// presenting the permanently unlocked Styx trinket. Lives on the TrinketUnlockScreen
/// GameObject; UI is built at runtime under the screen's CanvasGroup.
/// </summary>
public class TrinketUnlockHandler : MonoBehaviour
{
    Action onContinue;

    RectTransform uiRoot;
    Image iconImage;
    TextMeshProUGUI nameText;
    TextMeshProUGUI descriptionText;

    public void Show(Trinket trinket, Action onContinueCallback)
    {
        onContinue = onContinueCallback;
        BuildUIIfNeeded();

        PlayerEffectDescriptionData descriptionData = trinket.GetDescriptionData();
        iconImage.sprite = descriptionData != null ? descriptionData.Icon : null;
        iconImage.color = iconImage.sprite != null ? Color.white : StyxUI.IconFallbackColor;
        if (iconImage.sprite != null) iconImage.preserveAspect = true;

        nameText.text = trinket.Name;
        descriptionText.text = trinket.Description;
    }

    void BuildUIIfNeeded()
    {
        if (uiRoot != null) return;

        uiRoot = StyxUI.CreateStretchedRect(transform, "TrinketUnlockUI");

        Image background = StyxUI.CreatePanel(uiRoot, "Background", StyxUI.BackgroundColor);
        StyxUI.Stretch(background.rectTransform);

        TextMeshProUGUI header = StyxUI.CreateText(uiRoot, "Header", "A Styx Trinket Has Been Unlocked", 44, TextAlignmentOptions.Center);
        StyxUI.SetAnchored(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -90), new Vector2(1200, 70));

        Image iconFrame = StyxUI.CreatePanel(uiRoot, "IconFrame", StyxUI.RowColor);
        StyxUI.SetAnchored(iconFrame.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 70), new Vector2(190, 190));

        iconImage = StyxUI.CreateIcon(iconFrame.transform, "Icon", null);
        StyxUI.Stretch(iconImage.rectTransform);
        iconImage.rectTransform.offsetMin = new Vector2(12, 12);
        iconImage.rectTransform.offsetMax = new Vector2(-12, -12);

        nameText = StyxUI.CreateText(uiRoot, "TrinketName", "", 36, TextAlignmentOptions.Center);
        StyxUI.SetAnchored(nameText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -70), new Vector2(900, 50));

        descriptionText = StyxUI.CreateText(uiRoot, "TrinketDescription", "", 26, TextAlignmentOptions.Center);
        descriptionText.color = StyxUI.DimTextColor;
        StyxUI.SetAnchored(descriptionText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -140), new Vector2(900, 90));

        TextMeshProUGUI hint = StyxUI.CreateText(uiRoot, "Hint",
            "You can choose to start future runs with this trinket.", 22, TextAlignmentOptions.Center);
        hint.color = StyxUI.DimTextColor;
        StyxUI.SetAnchored(hint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -210), new Vector2(900, 40));

        Button continueButton = StyxUI.CreateButton(uiRoot, "ContinueButton", "Continue", 28, Continue);
        StyxUI.SetAnchored(((RectTransform)continueButton.transform), new Vector2(0.5f, 0f), new Vector2(0, 60), new Vector2(260, 70));
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
