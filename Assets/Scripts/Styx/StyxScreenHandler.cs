using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI for the River Styx node: the instruction text at the top and the single button at the bottom, which crosses
/// the Styx first and then continues back to the overworld. The 3D area and the exchange itself are run by StyxHandler.
/// Built and wired by Pharmakos/Setup Styx Area.
/// </summary>
public class StyxScreenHandler : MonoBehaviour
{
    public StyxHandler StyxHandler;

    [Header("Scene References")]
    public TextMeshProUGUI InstructionText;
    public CanvasGroup InstructionGroup;
    public Button ContinueButton;
    public TextMeshProUGUI ContinueButtonLabel;

    [Header("Text")]
    [TextArea] public string OfferingText = "You must leave what you have taken,\nand take what you have left.";
    [TextArea] public string ChoosingText = "This is what you will bring to the Final Boss.\nChoose up to two rituals to take with you.";
    public string CrossButtonText = "Cross the Styx";
    public string ContinueButtonText = "Continue";

    public float TextFadeDuration = 0.4f;

    Coroutine textFade;

    void Awake()
    {
        if (ContinueButton != null)
            ContinueButton.onClick.AddListener(ButtonClicked);
    }

    public void BeginStyx()
    {
        if (StyxHandler == null)
            StyxHandler = FindObjectOfType<StyxHandler>(true);

        if (StyxHandler == null)
        {
            Debug.LogError("StyxScreenHandler: no StyxHandler found in the scene.");
            Controller.Instance.StartNextLevel();
            return;
        }

        StyxHandler.BeginStyx();
    }

    public void ShowOffering()
    {
        SetInstruction(OfferingText, instant: true);
        SetButton(CrossButtonText, true);
    }

    public void ShowFlooding()
    {
        FadeInstruction(0f);
        SetButton(null, false);
    }

    public void ShowChoosing()
    {
        SetInstruction(ChoosingText, instant: false);
        SetButton(ContinueButtonText, true);
    }

    public void ShowLeaving()
    {
        if (ContinueButton != null) ContinueButton.interactable = false;
    }

    void ButtonClicked()
    {
        if (StyxHandler == null) return;

        if (StyxHandler.Phase == StyxHandler.StyxPhase.Offering)
            StyxHandler.CrossStyx();
        else if (StyxHandler.Phase == StyxHandler.StyxPhase.Choosing)
            StyxHandler.Continue();
    }

    void SetButton(string label, bool visible)
    {
        if (ContinueButton == null) return;

        if (label != null && ContinueButtonLabel != null)
            ContinueButtonLabel.text = label;

        ContinueButton.interactable = visible;
        ContinueButton.gameObject.SetActive(visible);
    }

    void SetInstruction(string text, bool instant)
    {
        if (InstructionText != null)
            InstructionText.text = text;

        if (instant)
        {
            StopTextFade();
            if (InstructionGroup != null) InstructionGroup.alpha = 1f;
        }
        else
        {
            FadeInstruction(1f);
        }
    }

    void FadeInstruction(float targetAlpha)
    {
        if (InstructionGroup == null) return;

        StopTextFade();
        if (!isActiveAndEnabled || TextFadeDuration <= 0f)
        {
            InstructionGroup.alpha = targetAlpha;
            return;
        }
        textFade = StartCoroutine(FadeInstructionRoutine(targetAlpha));
    }

    IEnumerator FadeInstructionRoutine(float targetAlpha)
    {
        float startAlpha = InstructionGroup.alpha;
        float elapsed = 0f;
        while (elapsed < TextFadeDuration)
        {
            elapsed += Time.deltaTime;
            InstructionGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / TextFadeDuration);
            yield return null;
        }
        InstructionGroup.alpha = targetAlpha;
        textFade = null;
    }

    void StopTextFade()
    {
        if (textFade == null) return;
        StopCoroutine(textFade);
        textFade = null;
    }
}
