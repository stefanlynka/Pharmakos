using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewDiscard : MonoBehaviour
{
    public MeshRenderer Highlight;
    public bool isHuman = false;

    private LayerMask zoneLayer = 128; // only layer 7

    // Update is called once per frame
    void Update()
    {
        if (!isHuman) return;

        if (View.Instance.SelectionHandler.IsHoldingCard())
        {
            Highlight.enabled = true;
        }
        else
        {
            Highlight.enabled = false;
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
