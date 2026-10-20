using UnityEngine;
using UnityEngine.UI;

public class StartMenuHandler : MonoBehaviour
{
    public GameObject NewRunButton;
    public GameObject ContinueButton;
    public GameObject AbandonButton;
    public GameObject ConfirmPanel;

    void Awake()
    {
        Wire(NewRunButton, OnNewRun);
        Wire(ContinueButton, OnContinue);
        Wire(AbandonButton, OnAbandon);
        if (ConfirmPanel == null) return;

        foreach (Button button in ConfirmPanel.GetComponentsInChildren<Button>(true))
        {
            if (button.name == "ConfirmAbandon")
                Wire(button.gameObject, OnAbandonConfirm);
            else if (button.name == "CancelAbandon")
                Wire(button.gameObject, OnAbandonCancel);
        }
    }

    void OnEnable()
    {
        Refresh();
    }

    static void Wire(GameObject buttonObject, UnityEngine.Events.UnityAction action)
    {
        if (buttonObject == null) return;

        Button button = buttonObject.GetComponent<Button>();
        if (button == null) return;

        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(action);
    }

    public void Refresh()
    {
        bool hasSave = RunSave.HasSave();
        if (NewRunButton != null)
            NewRunButton.SetActive(!hasSave);
        if (ContinueButton != null)
            ContinueButton.SetActive(hasSave);
        if (AbandonButton != null)
            AbandonButton.SetActive(hasSave);
        if (ConfirmPanel != null)
            ConfirmPanel.SetActive(false);
    }

    public void OnNewRun()
    {
        if (Controller.Instance != null)
            Controller.Instance.GoToStarterBundleScreen();
    }

    public void OnContinue()
    {
        if (Controller.Instance != null)
            Controller.Instance.ContinueRun();
    }

    public void OnAbandon()
    {
        if (ConfirmPanel != null)
            ConfirmPanel.SetActive(true);
    }

    public void OnAbandonConfirm()
    {
        if (Controller.Instance != null)
            Controller.Instance.AbandonRun();
        Refresh();
    }

    public void OnAbandonCancel()
    {
        if (ConfirmPanel != null)
            ConfirmPanel.SetActive(false);
    }
}
