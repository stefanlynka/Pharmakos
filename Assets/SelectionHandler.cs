using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class SelectionHandler
{
    private LayerMask targetLayer = 64; // only layer 6

    public ViewTarget CurrentHover = null;
    public ViewCard HeldCard = null;

    private float timeSinceCardInHandPickedUp = 0;
    public float heldCardZ = 20;
    public Vector3 HeldScale = Vector3.one;

    public List<ITarget> CurrentTargets = new List<ITarget>();

    public void Setup()
    {
        ViewEventHandler.Instance.ViewTargetInHandClicked += ViewTargetInHandClicked;
        ViewEventHandler.Instance.ClickedAway += ClickedAway;
    }


    public void UpdateSelections()
    {
        UpdateTargetUnderMouse();
        HandleMouseInputs();
        UpdateHeldCard();
    }

    public void UpdateTargetUnderMouse()
    {
        Physics.SyncTransforms();

        CurrentHover = null;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); // HOWTO Raycast
        if (Physics.Raycast(ray, out RaycastHit hitData, 1000, targetLayer))
        {
            if (hitData.collider.gameObject.TryGetComponent(out ViewTarget viewCard))
            {
                CurrentHover = viewCard;
            }
        }
        //if (CurrentHover != null) Debug.LogError("CurrentHover: " +  CurrentHover.gameObject.name);
        Debug.DrawRay(ray.origin, ray.direction * 100, Color.red);

        if (HeldCard != null) timeSinceCardInHandPickedUp += Time.deltaTime;
    }
    private void HandleMouseInputs()
    {
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
                    // TODO: Try place follower on battlefield
                    DropHeldCard();
                }
            }
        }
    }

    private void UpdateHeldCard()
    {
        if (HeldCard == null) return;

        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = heldCardZ;

        HeldCard.transform.position = mousePosition;
    }

    private void ViewTargetInHandClicked(ViewTarget target)
    {
        if (HeldCard != null)
        {
            DropHeldCard();
            return;
        }
        ViewCard viewCard = target as ViewCard;
        if (viewCard == null || !viewCard.Card.Owner.CanPlayCard(viewCard.Card)) return;

        ViewFollower viewFollower = target as ViewFollower;
        ViewSpell viewSpell = target as ViewSpell;
        if (viewFollower != null)
        {
            timeSinceCardInHandPickedUp = 0;
            //viewFollower.SetHighlight(true);
            HeldCard = viewFollower;
            HeldCard.transform.localScale = HeldScale;
            View.Instance.PlayerHand.RemoveCard(HeldCard);
        }
        else if (viewSpell != null && viewSpell.Spell.HasPlayableTargets())
        {
            timeSinceCardInHandPickedUp = 0;
            HeldCard = viewSpell;
            //viewSpell.SetHighlight(true);
            viewSpell.EnterTargetMode();
            HeldCard.transform.localScale = HeldScale;
            View.Instance.PlayerHand.RemoveCard(HeldCard);
            CurrentTargets = viewSpell.Spell.GetPlayableTargets();
            View.Instance.HighlightTargets(CurrentTargets);
        }
    }

    private void ClickedAway()
    {
        if (HeldCard == null) return;

        DropHeldCard();
    }

    private void DropHeldCard()
    {
        ViewFollower viewFollower = HeldCard as ViewFollower;
        if (viewFollower != null && View.Instance.PlayerBattleRow.IsMouseOverThis())
        {
            // Try place on battlefield
            int placementIndex = View.Instance.PlayerBattleRow.GetPlacementIndex();
            Controller.Instance.Player1.TryPlayFollower(viewFollower.Follower, placementIndex);
            View.Instance.PlayerBattleRow.AddFollower(viewFollower, placementIndex);
        }
        else
        {
            bool foundTarget = false;

            ViewSpell viewSpell = HeldCard as ViewSpell;
            if (viewSpell != null)
            {
                viewSpell.EnterCardMode();
                View.Instance.HighlightTargets(new List<ITarget>());

                if (CurrentHover != null && CurrentTargets.Contains(CurrentHover.Target))
                {
                    Controller.Instance.Player1.PlayCard(viewSpell.Spell);
                    viewSpell.Spell.Play(CurrentHover.Target);
                    View.Instance.ReleaseCard(viewSpell);
                    foundTarget = true;
                }
            }

            if (!foundTarget) View.Instance.PlayerHand.MoveCardToHand(HeldCard);
        }

        HeldCard.SetHighlight(false);
        CurrentHover = null;
        HeldCard = null;
        CurrentTargets.Clear();
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
    public bool IsHoldingCard()
    {
        return HeldCard != null;
    }
    public bool IsHoldingFollower()
    {
        return HeldCard != null && HeldCard is ViewFollower;
    }
}
