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
        if (Ritual == null)
        {
            Title.text = "";
            Description.text = "";
            return;
        }
        Title.text = Ritual.Name;
        Description.text = Ritual.Description;

        BloodText.text = Ritual.Costs[OfferingType.Blood].ToString();
        BonesText.text = Ritual.Costs[OfferingType.Bone].ToString();
        CropsText.text = Ritual.Costs[OfferingType.Crop].ToString();
        ScrollsText.text = Ritual.Costs[OfferingType.Scroll].ToString();
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
        else if (Ritual != null && Ritual.Owner.IsHuman && Ritual.CanPlay()) // If Playable
        {
            highlight = true;
        }

        SetHighlight(highlight);
    }
}
