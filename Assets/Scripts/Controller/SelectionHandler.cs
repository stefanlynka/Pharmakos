using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class SelectionHandler
{
    private LayerMask targetLayer = 64; // only layer 6

    // For picked up card in hand
    public ViewTarget CurrentHover = null;
    public ViewCard HeldCard = null;
    private float timeSinceCardInHandPickedUp = 0;
    public float heldCardZ = 20;
    public Vector3 HeldScale = Vector3.one;
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
        UpdateTargetUnderMouse();
        HandleMouseInputs();
        UpdateHeldCard();
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

        if (HeldCard != null) timeSinceCardInHandPickedUp += Time.deltaTime;
        if (AttackingFollower != null) timeSinceAttackerPickedUp += Time.deltaTime;
        if (SelectedRitual != null) timeSinceRitualSelected += Time.deltaTime;
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
        if (viewCard == null || !viewCard.Card.CanPlay()) return;

        ViewFollower viewFollower = target as ViewFollower;
        ViewSpell viewSpell = target as ViewSpell;
        if (viewFollower != null)
        {
            timeSinceCardInHandPickedUp = 0;
            //viewFollower.SetHighlight(true);
            HeldCard = viewFollower;
            HeldCard.transform.localScale = HeldScale;
            View.Instance.Player1.HandHandler.RemoveCard(HeldCard);
        }
        else if (viewSpell != null && viewSpell.Spell.HasPlayableTargets())
        {
            timeSinceCardInHandPickedUp = 0;
            HeldCard = viewSpell;
            //viewSpell.SetHighlight(true);
            viewSpell.EnterTargetMode();
            HeldCard.transform.localScale = HeldScale;
            View.Instance.Player1.HandHandler.RemoveCard(HeldCard);
            CurrentTargets = viewSpell.Spell.GetTargets();
            View.Instance.HighlightTargets(CurrentTargets);
        }
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
        if (viewFollower != null && View.Instance.Player1.BattleRow.IsMouseOverThis())
        {
            // Try place on battlefield
            int placementIndex = View.Instance.Player1.BattleRow.GetPlacementIndex();
            Controller.Instance.Player1.TryPlayFollower(viewFollower.Follower, placementIndex);
            //viewFollower = View.Instance
            //View.Instance.PlayerBattleRow.AddFollower(viewFollower, placementIndex);
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
    public bool IsHoldingCard()
    {
        return HeldCard != null || AttackingFollower != null || SelectedRitual != null;
    }
    public bool IsHoldingFollower()
    {
        return HeldCard != null && HeldCard is ViewFollower;
    }

    public bool IsHoveringOverThisCard(ViewCard card)
    {
        return CurrentHover == card;
    }
}
