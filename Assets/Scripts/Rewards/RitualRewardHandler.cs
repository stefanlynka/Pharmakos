using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RitualRewardHandler : MonoBehaviour
{
    public Camera RitualRewardCamera;

    [Tooltip("Player slots. Whatever sits here on Continue becomes the player's rituals.")]
    public RitualHolder CurrentTopRitual;
    public RitualHolder CurrentBottomRitual;

    public RitualHolder TopRitualReward;
    public RitualHolder BottomRitualReward;

    [Header("Drag")]
    [Tooltip("How far a picked-up ritual rises along its holder's local Z, in holder units.")]
    [SerializeField] float dragLiftHeight = 2f;
    [Tooltip("How quickly a dragged ritual catches up to the cursor.")]
    [SerializeField] float dragFollowSharpness = 25f;

    List<Ritual> possibleRewards = new List<Ritual>();

    ViewRitual draggedRitual;
    RitualHolder dragOrigin;
    Vector3 dragGrabOffset;
    ViewRitual hoveredRitual;
    bool isContinuing;

    void OnDisable()
    {
        ResetInteraction();
        DeactivateMenuSelection();
    }

    void Update()
    {
        if (isContinuing) return;
        if (Controller.Instance != null && Controller.Instance.GamePaused) return;

        if (draggedRitual != null)
            UpdateDrag();
        else
            UpdateHover();
    }

    public void Load(int levelCompleted, Ritual topRitual, Ritual bottomRitual)
    {
        Load(levelCompleted, topRitual, bottomRitual, null, null);
    }

    public void Load(int levelCompleted, Ritual topRitual, Ritual bottomRitual, Ritual offeredTopReward, Ritual offeredBottomReward)
    {
        isContinuing = false;
        ResetInteraction();

        CurrentTopRitual.Load(topRitual, RitualPressed);
        CurrentBottomRitual.Load(bottomRitual, RitualPressed);

        if (offeredTopReward != null && offeredBottomReward != null)
        {
            TopRitualReward.Load(offeredTopReward, RitualPressed);
            BottomRitualReward.Load(offeredBottomReward, RitualPressed);
        }
        else
        {
            System.Type type1 = topRitual != null ? topRitual.GetType() : null;
            System.Type type2 = bottomRitual != null ? bottomRitual.GetType() : null;
            possibleRewards = Controller.Instance.ProgressionHandler.GetPossibleRitualRewards(); //CardHandler.GetPossibleRitualRewards(levelCompleted);
            possibleRewards.RemoveAll(ritual => ritual.GetType() == type1 || ritual.GetType() == type2);

            int randomIndex = Controller.Instance.MetaRNG.Next(0, possibleRewards.Count);
            TopRitualReward.Load(possibleRewards[randomIndex], RitualPressed);
            possibleRewards.RemoveAt(randomIndex);

            randomIndex = Controller.Instance.MetaRNG.Next(0, possibleRewards.Count);
            BottomRitualReward.Load(possibleRewards[randomIndex], RitualPressed);
            possibleRewards.RemoveAt(randomIndex);
        }

        ActivateMenuSelection();
    }

    void ActivateMenuSelection()
    {
        if (View.Instance?.MenuSelectionHandler == null) return;

        Camera selectionCamera = RitualRewardCamera;
        if (selectionCamera == null)
            selectionCamera = GetComponentInChildren<Camera>(true);
        if (selectionCamera == null
            && ScreenHandler.Instance != null
            && ScreenHandler.Instance.TryGetScreen(ScreenName.RitualRewards, out Screen screen))
            selectionCamera = screen.Camera;

        View.Instance.MenuSelectionHandler.Activate(selectionCamera, () => isContinuing);
    }

    void DeactivateMenuSelection()
    {
        if (View.Instance?.MenuSelectionHandler != null)
            View.Instance.MenuSelectionHandler.Deactivate();
    }

    public void SetRitualsAndContinue()
    {
        if (isContinuing) return;

        ResetInteraction();
        isContinuing = true;

        Controller.Instance.SetRituals(CurrentTopRitual.ViewRitual.Ritual, CurrentBottomRitual.ViewRitual.Ritual);

        Controller.Instance.OnOverworldRitualNodeRewardsComplete();

        Sequence moveSequence = new Sequence();
        moveSequence.Add(new Tween(Wait, 0, 1, 0.8f));
        moveSequence.Add(new SequenceAction(Cleanup));
        moveSequence.Start();
    }
    private void Wait(float progress)
    {

    }
    private void Cleanup()
    {
        ResetInteraction();
        DeactivateMenuSelection();

        gameObject.SetActive(false);
    }

    IEnumerable<RitualHolder> Holders()
    {
        yield return CurrentTopRitual;
        yield return CurrentBottomRitual;
        yield return TopRitualReward;
        yield return BottomRitualReward;
    }

    RitualHolder HolderOf(ViewRitual viewRitual)
    {
        if (viewRitual == null) return null;

        foreach (RitualHolder holder in Holders())
        {
            if (holder != null && holder.ViewRitual == viewRitual)
                return holder;
        }
        return null;
    }

    void SetAllGlows(bool active)
    {
        foreach (RitualHolder holder in Holders())
        {
            if (holder != null)
                holder.SetGlow(active);
        }
    }

    void UpdateHover()
    {
        ViewRitual hovered = null;
        if (MenuSelectionHandler.Instance != null && MenuSelectionHandler.Instance.IsActive)
            hovered = MenuSelectionHandler.Instance.CurrentHover as ViewRitual;

        RitualHolder hoveredHolder = HolderOf(hovered);
        if (hoveredHolder == null)
            hovered = null;

        if (hovered != hoveredRitual)
        {
            hoveredRitual = hovered;
            if (hoveredRitual != null)
                PlayHoverSound();
        }

        foreach (RitualHolder holder in Holders())
        {
            if (holder != null)
                holder.SetGlow(holder == hoveredHolder);
        }
    }

    void RitualPressed(ViewTarget target)
    {
        if (isContinuing || draggedRitual != null) return;

        ViewRitual viewRitual = target as ViewRitual;
        RitualHolder holder = HolderOf(viewRitual);
        if (holder == null || viewRitual.Ritual == null) return;

        if (!TryGetDragPoint(holder, out Vector3 point)) return;

        holder.StopSliding();
        draggedRitual = viewRitual;
        dragOrigin = holder;
        hoveredRitual = viewRitual;
        dragGrabOffset = Vector3.ProjectOnPlane(viewRitual.transform.position - point, holder.transform.forward);

        ViewRitual.HoverSummariesSuppressed = true;
        SetAllGlows(true);
    }

    void UpdateDrag()
    {
        if (!Input.GetMouseButton(0))
        {
            EndDrag();
            return;
        }

        if (!TryGetDragPoint(dragOrigin, out Vector3 point)) return;

        Transform dragged = draggedRitual.transform;
        float follow = 1f - Mathf.Exp(-dragFollowSharpness * Time.deltaTime);
        dragged.position = Vector3.Lerp(dragged.position, point + dragGrabOffset, follow);
    }

    void EndDrag()
    {
        RitualHolder target = FindDropTarget(draggedRitual.transform.position);
        if (target != null && target != dragOrigin)
        {
            ViewRitual displaced = target.ViewRitual;
            target.Receive(draggedRitual);
            dragOrigin.Receive(displaced);
        }
        else
            dragOrigin.SlideToRest();

        draggedRitual = null;
        dragOrigin = null;
        ViewRitual.HoverSummariesSuppressed = false;
        SetAllGlows(false);
    }

    RitualHolder FindDropTarget(Vector3 point)
    {
        RitualHolder closest = null;
        float closestDistance = float.MaxValue;

        foreach (RitualHolder holder in Holders())
        {
            if (holder == null || !holder.IsOverOutline(point)) continue;

            float distance = (holder.RestPosition - point).sqrMagnitude;
            if (distance < closestDistance)
            {
                closest = holder;
                closestDistance = distance;
            }
        }
        return closest;
    }

    /// <summary>Where the cursor meets the plane a picked-up ritual floats on, lifted above the holder's rest Z.</summary>
    bool TryGetDragPoint(RitualHolder holder, out Vector3 point)
    {
        point = Vector3.zero;

        Camera camera = MenuSelectionHandler.Instance != null ? MenuSelectionHandler.Instance.GetSelectionCamera() : RitualRewardCamera;
        if (camera == null) return false;

        Vector3 liftedRest = holder.RestPosition + holder.transform.TransformVector(new Vector3(0f, 0f, -dragLiftHeight));
        Plane dragPlane = new Plane(holder.transform.forward, liftedRest);
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        if (!dragPlane.Raycast(ray, out float distance)) return false;

        point = ray.GetPoint(distance);
        return true;
    }

    void ResetInteraction()
    {
        if (draggedRitual != null && dragOrigin != null)
            dragOrigin.SnapToRest();

        draggedRitual = null;
        dragOrigin = null;
        hoveredRitual = null;
        ViewRitual.HoverSummariesSuppressed = false;
        SetAllGlows(false);
    }

    void PlayHoverSound()
    {
        if (View.Instance == null || View.Instance.AudioHandler == null) return;

        View.Instance.AudioHandler.PlaySoundEffect(AudioHandler.SoundEffectType.Bump);
    }
}
