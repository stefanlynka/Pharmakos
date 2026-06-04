using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectionHandler
{
    private LayerMask targetLayer = 64; // only layer 6

    // For picked up card in hand
    public ViewTarget CurrentHover = null;
    public ViewCard HeldCard = null;
    private float timeSinceCardInHandPickedUp = 0;
    public float heldCardLocalZ = -4f;
    public List<ITarget> CurrentTargets = new List<ITarget>();

    // For picking follower attack target
    public ViewFollower AttackingFollower = null;
    public List<ITarget> AttackableTargets = new List<ITarget>();
    private float timeSinceAttackerPickedUp = 0;

    // For picking ritual targets
    public ViewRitual SelectedRitual = null;
    public List<ITarget> PotentialRitualTargets = new List<ITarget>();
    private float timeSinceRitualSelected = 0;

    public void Setup()
    {
        ViewEventHandler.Instance.ViewTargetInHandClicked += ViewTargetInHandClicked;
        ViewEventHandler.Instance.ViewTargetInPlayClicked += ViewTargetInPlayClicked;
        ViewEventHandler.Instance.RitualClicked += ViewRitualClicked;
        ViewEventHandler.Instance.ClickedAway += ClickedAway;
    }

    public void Clear()
    {
        ViewEventHandler.Instance.ViewTargetInHandClicked -= ViewTargetInHandClicked;
        ViewEventHandler.Instance.ViewTargetInPlayClicked -= ViewTargetInPlayClicked;
        ViewEventHandler.Instance.RitualClicked -= ViewRitualClicked;
        ViewEventHandler.Instance.ClickedAway -= ClickedAway;
    }


    public void UpdateSelections()
    {
        if (Controller.Instance.GamePaused) return;
        if (GetSelectionCamera() == null) return;

        UpdateTargetUnderMouse();
        HandleMouseInputs();
    }

    public void UpdateHeldCardAfterHandLayout()
    {
        if (Controller.Instance.GamePaused) return;
        UpdateHeldCard();
    }

    public void UpdateTargetUnderMouse()
    {
        CurrentHover = null;

        Camera camera = GetSelectionCamera();
        if (camera == null) return;

        Physics.SyncTransforms();

        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hitData, 1000, targetLayer))
        {
            GameObject hitObject = hitData.collider.gameObject;
            if (hitObject.TryGetComponent(out CardViewRaycastTarget cardViewTarget))
                CurrentHover = cardViewTarget;
            else if (hitObject.TryGetComponent(out ViewTarget viewTarget))
                CurrentHover = viewTarget;
        }
        Debug.DrawRay(ray.origin, ray.direction * 100, Color.red);

        if (HeldCard != null) timeSinceCardInHandPickedUp += Time.deltaTime;
        if (AttackingFollower != null) timeSinceAttackerPickedUp += Time.deltaTime;
        if (SelectedRitual != null) timeSinceRitualSelected += Time.deltaTime;
    }
    private void HandleMouseInputs()
    {
        if (!Controller.Instance.Player1.IsMyTurn) return;
        if (Input.GetMouseButtonDown(0))
        {
            if (CurrentHover == null)
            {
                ViewEventHandler.Instance.FireClickedAway();
            }
            else
            {
                CurrentHover.OnClick?.Invoke(CurrentHover);
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (HeldCard != null)
            {
                // If you click quickly, keep the card picked up after mouse up
                // If you click and hold, drop card on mouse up
                if (timeSinceCardInHandPickedUp > 0.1f)
                {
                    DropHeldCard();
                }
            }
            if (AttackingFollower != null)
            {
                if (timeSinceAttackerPickedUp > 0.1f)
                {
                    DropAttacker();
                }
            }
            if (SelectedRitual != null)
            {
                if (timeSinceRitualSelected > 0.1f)
                {
                    DropRitual();
                }
            }
        }
    }

    /// <summary>Combat camera; null when no usable world camera (e.g. after leaving combat).</summary>
    Camera GetSelectionCamera()
    {
        if (ScreenHandler.Instance != null
            && ScreenHandler.Instance.TryGetScreen(ScreenName.Game, out Screen gameScreen)
            && gameScreen.Camera != null
            && gameScreen.Camera.isActiveAndEnabled)
            return gameScreen.Camera;

        Camera main = Camera.main;
        if (main != null && main.isActiveAndEnabled)
            return main;

        return null;
    }

    Transform GetHumanHandTransform()
    {
        return View.Instance.Player1.HandHandler.transform;
    }

    bool TryGetMousePositionOnHandPlane(Camera camera, Transform hand, out Vector3 localPos)
    {
        localPos = default;

        Vector3 planePoint = hand.TransformPoint(new Vector3(0f, 0f, heldCardLocalZ));
        Vector3 planeNormal = hand.forward;
        if (Vector3.Dot(planeNormal, camera.transform.position - planePoint) > 0f)
            planeNormal = -planeNormal;

        Plane dragPlane = new Plane(planeNormal, planePoint);
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        if (!dragPlane.Raycast(ray, out float distance))
            return false;

        localPos = hand.InverseTransformPoint(ray.GetPoint(distance));
        localPos.z = heldCardLocalZ;
        return true;
    }

    void ApplyHeldCardPosition()
    {
        if (HeldCard == null) return;

        Camera camera = GetSelectionCamera();
        if (camera == null) return;

        Transform hand = GetHumanHandTransform();
        HeldCard.transform.SetParent(hand, true);

        if (!TryGetMousePositionOnHandPlane(camera, hand, out Vector3 localPos))
            return;

        HeldCard.transform.localPosition = localPos;
    }

    private void UpdateHeldCard()
    {
        ApplyHeldCardPosition();
    }

    private void ViewTargetInHandClicked(ViewTarget target)
    {
        if (HeldCard != null)
        {
            DropHeldCard();
            return;
        }

        ViewCard viewCard = ResolveViewCard(target);
        if (viewCard == null) return;

        ViewFollower viewFollower = viewCard as ViewFollower;
        ViewSpell viewSpell = viewCard as ViewSpell;
        if (viewFollower != null)
        {
            timeSinceCardInHandPickedUp = 0;
            HeldCard = viewFollower;
            View.Instance.Player1.HandHandler.RemoveCard(HeldCard);
            View.ApplyCombatCardScale(HeldCard.transform);
            ApplyHeldCardPosition();
        }
        else if (viewSpell != null)
        {
            timeSinceCardInHandPickedUp = 0;
            HeldCard = viewSpell;
            if (viewSpell.Spell.CanPlay() && viewSpell.Spell.HasPlayableTargets())
            {
                viewSpell.EnterTargetMode();
                CurrentTargets = viewSpell.Spell.GetTargets();
                View.Instance.HighlightTargets(CurrentTargets);
            }
            View.Instance.Player1.HandHandler.RemoveCard(HeldCard);
            View.ApplyCombatCardScale(HeldCard.transform);
            ApplyHeldCardPosition();
        }
    }

    static ViewCard ResolveViewCard(ViewTarget target)
    {
        if (target is ViewCard viewCard)
            return viewCard;

        if (target is CardViewRaycastTarget proxy)
            return proxy.ActiveCard;

        return null;
    }

    private void ViewTargetInPlayClicked(ViewTarget target)
    {
        //Debug.LogError("In Play Target Clicked");

        if (HeldCard != null) return;

        ViewFollower viewFollower = target as ViewFollower;
        // You haven't selected something, and you're clicking on a friendly follower that can attack
        if (AttackingFollower == null && viewFollower != null && viewFollower.Follower.Owner.IsHuman && viewFollower.Follower.CanAttack())
        {
            timeSinceAttackerPickedUp = 0;
            AttackingFollower = viewFollower;
            AttackableTargets = viewFollower.Follower.GetAttackTargets();
            View.Instance.HighlightTargets(AttackableTargets);
        }

        return;
    }

    private void ViewRitualClicked(ViewTarget target)
    {
        if (HeldCard != null) return;

        ViewRitual viewRitual = target as ViewRitual;
        if (viewRitual == null || viewRitual.Ritual == null) return;

        if (SelectedRitual != null) return;
        if (!viewRitual.Ritual.CanPlay()) return;

        SelectedRitual = viewRitual;
        PotentialRitualTargets = SelectedRitual.Ritual.GetTargets();
        timeSinceRitualSelected = 0;
        View.Instance.HighlightTargets(PotentialRitualTargets);

        Controller.Instance.LightingHandler.DimLights();
        View.Instance.AudioHandler.PlayOther(AudioHandler.OtherSoundType.Rumble);
    }

    private void ClickedAway()
    {
        if (HeldCard == null) return;

        DropHeldCard();
        //DropAttacker();
        //DropRitual();
    }

    private void DropHeldCard()
    {
        ViewFollower viewFollower = HeldCard as ViewFollower;
        ViewSpell viewSpell = HeldCard as ViewSpell;

        // Check if card is being dropped over ViewDiscard
        if (View.Instance.ViewDiscard != null && View.Instance.ViewDiscard.IsMouseOverThis() && HeldCard != null && HeldCard.Card != null)
        {
            // If it's a spell, exit target mode
            if (viewSpell != null) viewSpell.EnterCardMode();

            // Discard the card using DiscardCardAction
            Controller.Instance.Player1.TryDiscardCard(HeldCard.Card);
            HeldCard.SetHighlight(false);
            View.Instance.HighlightTargets(new List<ITarget>());
            CurrentHover = null;
            HeldCard = null;
            CurrentTargets.Clear();
            return;
        }
        else if (viewFollower != null && View.Instance.Player1.BattleRow.IsMouseOverThis() && viewFollower.Follower.CanPlay() && View.Instance.Player1.BattleRow.Followers.Count < Player.MaxFollowerCount)
        {
            // Check if card is being dropped over battlefield
            int placementIndex = View.Instance.Player1.BattleRow.GetPlacementIndex();
            Controller.Instance.Player1.TryPlayFollower(viewFollower.Follower, placementIndex);
            //viewFollower = View.Instance
            //View.Instance.PlayerBattleRow.AddFollower(viewFollower, placementIndex);
        }
        else
        {
            bool foundTarget = false;
            // Check if card is a spell being dropped on a valid target
            
            if (viewSpell != null)
            {
                viewSpell.EnterCardMode();
                View.Instance.HighlightTargets(new List<ITarget>());

                if (CurrentHover != null && CurrentTargets.Contains(CurrentHover.Target))
                {
                    Controller.Instance.Player1.TryPlaySpell(viewSpell.Spell, CurrentHover.Target); // TODO: If spells are broken comment out this line and uncomment lines below
                    //Controller.Instance.Player1.PlayCard(viewSpell.Spell);
                    //viewSpell.Spell.Play(CurrentHover.Target);
                    //View.Instance.RemoveViewCard(viewSpell);
                    foundTarget = true;
                }
            }

            if (!foundTarget) View.Instance.Player1.HandHandler.MoveCardToHand(HeldCard);
        }

        HeldCard.SetHighlight(false);
        CurrentHover = null;
        HeldCard = null;
        CurrentTargets.Clear();
    }

    public void DropAttacker()
    {
        if (CurrentHover != null && AttackableTargets.Contains(CurrentHover.Target))
        {
            GameAction newAction = new PreAttackWithFollowerAction(AttackingFollower.Follower, CurrentHover.Target);
            AttackingFollower.Follower.GameState.ActionHandler.AddAction(newAction);

            //Controller.Instance.Player1.PerformAttack(AttackingFollower.Follower, attackTarget);
        }

        AttackingFollower = null;
        AttackableTargets.Clear();
        View.Instance.HighlightTargets(new List<ITarget>());
    }

    public void DropRitual()
    {
        if (CurrentHover != null && PotentialRitualTargets.Contains(CurrentHover.Target))
        {
            SelectedRitual.Ritual.Play(CurrentHover.Target);
        }
        else
        {
            Controller.Instance.LightingHandler.RestoreLights();
        }
        View.Instance.AudioHandler.StopOther();

        SelectedRitual = null;
        PotentialRitualTargets.Clear();
        View.Instance.HighlightTargets(new List<ITarget>());
    }

    public bool TryGetHeldFollower(out ViewFollower viewFollower)
    {
        viewFollower = null;

        if (HeldCard is ViewFollower)
        {
            viewFollower = HeldCard as ViewFollower;
            return true;
        }

        return false;
    }
    public bool IsSelectingTarget()
    {
        return HeldCard != null || AttackingFollower != null || SelectedRitual != null;
    }
    public bool IsHoldingCard()
    {
        return HeldCard != null;
    }
    public bool IsHoldingFollower()
    {
        return HeldCard != null && HeldCard is ViewFollower;
    }
    public bool IsHoldingPlayableFollower()
    {
        //if (HeldCard == null) return false;

        if (HeldCard is ViewFollower viewFollower)
        {
            return viewFollower.Follower.CanPlay();
        }

        return false;
    }

    public bool IsHoveringOverThisCard(ViewCard card)
    {
        if (CurrentHover is CardViewRaycastTarget proxy)
            return proxy.ActiveCard == card;

        return CurrentHover == card;
    }
}
