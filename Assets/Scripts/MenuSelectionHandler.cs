using System;
using UnityEngine;

/// <summary>
/// Raycast-based click handling for non-combat screens (starter bundle, temple sacrifice, rewards, etc.).
/// Activate while a menu screen is showing; combat uses <see cref="SelectionHandler"/> instead.
/// </summary>
public class MenuSelectionHandler : MonoBehaviour
{
    public static MenuSelectionHandler Instance { get; private set; }

    const int TargetLayerIndex = 6;
    readonly LayerMask targetLayer = 1 << TargetLayerIndex;

    Camera _selectionCamera;
    Func<bool> _blockInput;

    public bool IsActive { get; private set; }
    public ViewTarget CurrentHover { get; private set; }

    // The card whose hover popups (keyword reminders, referenced card) are showing, and where they hang.
    ViewCard _popupCard;
    Transform _popupAnchor;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        enabled = false;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <param name="camera">Camera used for screen-point rays (e.g. altar / event camera). Falls back to Camera.main.</param>
    /// <param name="blockInput">When this returns true, clicks are ignored (e.g. during animations).</param>
    public void Activate(Camera camera = null, Func<bool> blockInput = null)
    {
        IsActive = true;
        _selectionCamera = camera;
        _blockInput = blockInput;
        enabled = true;
    }

    public void Deactivate()
    {
        HideCardPopups();
        IsActive = false;
        _selectionCamera = null;
        _blockInput = null;
        CurrentHover = null;
        enabled = false;
    }

    void Update()
    {
        if (!IsActive) return;
        UpdateSelections();
    }

    public void UpdateSelections()
    {
        if (!IsActive) return;
        if ((Controller.Instance != null && Controller.Instance.GamePaused)
            || ContentScrollView.BlocksGameInput || TutorialPresenter.BlocksGameInput)
        {
            HideCardPopups();
            return;
        }
        if (OverworldMapView.BlocksGameInput)
        {
            CurrentHover = null;
            HideCardPopups();
            return;
        }
        if (GetSelectionCamera() == null) return;

        UpdateTargetUnderMouse();
        UpdateCardPopups();
        HandleMouseInputs();
    }

    /// <summary>
    /// Menu cards are already in full-text mode, but a hovered follower still gets the same popups as a card
    /// raised in the hand: its keyword reminders and any card it names.
    /// </summary>
    void UpdateCardPopups()
    {
        ViewCard hovered = CardViewRaycastTarget.ResolveViewCard(CurrentHover);
        if (hovered != null && !hovered.gameObject.activeInHierarchy)
            hovered = null;

        if (hovered == _popupCard && (hovered == null || _popupAnchor != null))
            return;

        HideCardPopups();
        if (hovered == null || !(hovered.Card is Follower follower) || PopupScreenHandler.Instance == null)
            return;

        _popupCard = hovered;
        _popupAnchor = hovered.transform;
        Camera camera = GetSelectionCamera();
        PopupScreenHandler.Instance.ShowHandKeywordReminders(follower, _popupAnchor, PopupSlot.One, camera);
        PopupScreenHandler.Instance.ShowFollowerReferencedCard(follower, _popupAnchor, PopupSlot.One, camera);
    }

    void HideCardPopups()
    {
        if (_popupCard is null && _popupAnchor is null)
            return;

        if (PopupScreenHandler.Instance != null)
        {
            PopupScreenHandler.Instance.HideHandKeywordReminders(PopupSlot.One);
            if (_popupAnchor != null)
                PopupScreenHandler.Instance.HideFollowerReferencedCard(_popupAnchor);
            else
                PopupScreenHandler.Instance.HideOrphanedFollowerReferencedCard();
        }

        _popupCard = null;
        _popupAnchor = null;
    }

    void UpdateTargetUnderMouse()
    {
        CurrentHover = null;

        Camera camera = GetSelectionCamera();
        if (camera == null) return;

        Physics.SyncTransforms();

        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hitData, 1000f, targetLayer))
            return;

        GameObject hitObject = hitData.collider.gameObject;
        ViewTarget hitTarget = null;
        if (hitObject.TryGetComponent(out CardViewRaycastTarget cardViewTarget))
            hitTarget = cardViewTarget;
        else if (hitObject.TryGetComponent(out ViewTarget viewTarget))
            hitTarget = viewTarget;
        else if (hitObject.GetComponentInParent<CardViewRaycastTarget>() is CardViewRaycastTarget parentProxy)
            hitTarget = parentProxy;
        else if (hitObject.GetComponentInParent<ViewTarget>() is ViewTarget parentTarget)
            hitTarget = parentTarget;

        ViewCard viewCard = CardViewRaycastTarget.ResolveViewCard(hitTarget);
        CurrentHover = viewCard != null ? viewCard : hitTarget;
    }

    void HandleMouseInputs()
    {
        if (_blockInput != null && _blockInput()) return;
        if (!Input.GetMouseButtonDown(0)) return;
        if (InputGuard.IsBlocked) return;

        if (CurrentHover != null)
        {
            InputGuard.BlockFor(InputGuard.ClickBlockDuration);
            CurrentHover.OnClick?.Invoke(CurrentHover);
        }
    }

    /// <summary>Camera used for menu screen-point rays; falls back to <see cref="Camera.main"/>.</summary>
    public Camera GetSelectionCamera()
    {
        if (_selectionCamera != null && _selectionCamera.isActiveAndEnabled)
            return _selectionCamera;

        if (ScreenHandler.Instance != null
            && ScreenHandler.Instance.TryGetCurrentScreen(out Screen screen)
            && screen != null
            && screen.Camera != null
            && screen.Camera.isActiveAndEnabled)
            return screen.Camera;

        Camera main = Camera.main;
        if (main != null && main.isActiveAndEnabled)
            return main;

        return null;
    }
}
