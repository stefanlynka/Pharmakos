using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewDiscard : MonoBehaviour
{
    [Tooltip("Glows the bowl while a card is held (brighter over it). When set, the old flat Highlight is unused.")]
    public DiscardBowl Bowl;
    [Tooltip("Old flat outline, used only when no Bowl is assigned.")]
    public MeshRenderer Highlight;
    public bool isHuman = false;

    private LayerMask zoneLayer = 128; // only layer 7

    // Update is called once per frame
    void Update()
    {
        if (!isHuman) return;

        bool holding = View.Instance.SelectionHandler.IsHoldingCard();
        if (Bowl != null)
        {
            Bowl.SetHighlight(holding, holding && IsMouseOverThis());
            if (Highlight != null)
                Highlight.enabled = false;
        }
        else if (Highlight != null)
        {
            Highlight.enabled = holding;
        }
    }

    public bool IsMouseOverThis()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hitData, 1000, zoneLayer))
        {
            if (hitData.collider.gameObject == gameObject)
            {
                return true;
            }
        }
        return false;
    }
}
