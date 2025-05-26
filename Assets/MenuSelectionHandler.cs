using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuSelectionHandler : MonoBehaviour
{
    private LayerMask targetLayer = 64; // only layer 6

    public ViewTarget CurrentHover = null;

    private void Update()
    {
        UpdateSelections();
    }

    public void UpdateSelections()
    {
        UpdateTargetUnderMouse();
        HandleMouseInputs();
    }

    public void UpdateTargetUnderMouse()
    {
        Physics.SyncTransforms();

        CurrentHover = null;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); // HOWTO Raycast
        if (Physics.Raycast(ray, out RaycastHit hitData, 1000, targetLayer) && hitData.collider.gameObject.TryGetComponent(out ViewTarget viewTarget))
        {
            CurrentHover = viewTarget;
        }
        //if (CurrentHover != null) Debug.LogError("CurrentHover: " +  CurrentHover.gameObject.name);
        Debug.DrawRay(ray.origin, ray.direction * 100, Color.red);
    }
    private void HandleMouseInputs()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (CurrentHover != null)
            {
                CurrentHover.OnClick?.Invoke(CurrentHover);
            }
        }
    }
}