using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ViewRitual : ViewTarget
{
    public Ritual Ritual;
    public TextMeshPro BloodText;
    public TextMeshPro BonesText;
    public TextMeshPro CropsText;
    public TextMeshPro ScrollsText;

    public TextMeshPro Title;
    public TextMeshPro Description;

    public MeshRenderer Highlight;

    public GameObject SummaryObject;
    public TextMeshPro SummaryText;

    // This function is called when the mouse enters the Collider.
    void OnMouseEnter()
    {
        if (SummaryObject != null && Ritual != null && Ritual.ReminderText != string.Empty)
        {
            // Show the Summary
            SummaryObject.SetActive(true);
            // Show ReminderText
            SummaryText.text = Controller.Instance.TextHandler.ProcessText(Ritual.ReminderText);
        }
    }

    // This function is called when the mouse exits the Collider.
    void OnMouseExit()
    {
        if (SummaryObject != null)
        {
            SummaryObject.SetActive(false); // Disable the GameObject.
        }
    }
    public void Init(Ritual ritual, bool clickable = true)
    {
        Ritual = ritual;
        Target = Ritual;

        OnClick = RitualClicked;
        Refresh();

        BoxCollider collider = GetComponent<BoxCollider>();
        if (collider != null) collider.enabled = clickable;
    }

    public void Refresh()
    {
        gameObject.SetActive(Ritual != null);

        if (Ritual == null)
        {
            Title.text = "";
            Description.text = "";
            return;
        }
        Title.text = Ritual.Name;
        Description.text = Ritual.Description;

        BloodText.text = Ritual.GetCost(OfferingType.Blood).ToString();
        BonesText.text = Ritual.GetCost(OfferingType.Bone).ToString();
        CropsText.text = Ritual.GetCost(OfferingType.Crop).ToString();
        ScrollsText.text = Ritual.GetCost(OfferingType.Scroll).ToString();
    }

    public void SetHighlight(bool active)
    {
        Highlight.enabled = active;
    }

    public void RitualClicked(ViewTarget viewTarget)
    {
        ViewEventHandler.Instance.FireRitualClicked(viewTarget);
    }
    public void UpdateRitual()
    {
        bool highlight = false;
        if (View.Instance.SelectionHandler.SelectedRitual != null) // If Selected
        {
            if (View.Instance.SelectionHandler.SelectedRitual == this) highlight = true;
        }
        else if (Ritual != null && Ritual.Owner.IsHuman && Ritual.CanPlay() && View.Instance.IsInteractible) // If Playable
        {
            highlight = true;
        }

        SetHighlight(highlight);
    }
}
