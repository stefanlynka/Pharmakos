using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TempleScreenHandler : MonoBehaviour
{
    public TextMeshProUGUI InstructionText;
    public List<string> Instructions = new List<string>()
    {
        "The Gods Demand A Sacrifice",
        "The Gods Demand Another Sacrifice",
        "The Gods Demand A Final Sacrifice",
    };

    [Tooltip("Optional — wired at runtime if left empty.")]
    public TempleHandler TempleHandler;
    public Button AcceptSacrificeButton;
    public Button RefuseSacrificeButton;

    public void ResetForTemple()
    {
        if (TempleHandler == null) TempleHandler = TempleHandler.Instance;
        SetInstructionIndex(0);
        SetSacrificeButtonsInteractable(true);
    }

    public void SetInstructionIndex(int index)
    {
        if (Instructions == null || Instructions.Count == 0 || InstructionText == null) return;
        int clamped = Mathf.Clamp(index, 0, Instructions.Count - 1);
        InstructionText.text = Instructions[clamped];
    }

    public void SetSacrificeButtonsInteractable(bool interactable)
    {
        if (AcceptSacrificeButton != null) AcceptSacrificeButton.interactable = interactable;
        if (RefuseSacrificeButton != null) RefuseSacrificeButton.interactable = interactable;
    }

    public void RefuseSacrifice()
    {
        if (TempleHandler == null) TempleHandler = TempleHandler.Instance;
        TempleHandler?.RefuseSacrifice();
    }

    public void AcceptSacrifice()
    {
        if (TempleHandler == null) TempleHandler = TempleHandler.Instance;
        TempleHandler?.AcceptSacrifice();
    }

    public void ProgressToRitualScreen()
    {
        if (TempleHandler == null) TempleHandler = TempleHandler.Instance;
        TempleHandler?.EndTemple();

        ScreenHandler.Instance.HideScreen(ScreenName.Temple, true);
        Controller.Instance.GoToRitualRewardScreen();
    }
}
