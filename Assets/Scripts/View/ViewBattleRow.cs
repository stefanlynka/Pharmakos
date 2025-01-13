using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class ViewBattleRow : MonoBehaviour
{
    public MeshRenderer Highlight;
    public bool isHuman = false;

    private LayerMask zoneLayer = 128; // only layer 7

    public BattleRow BattleRow;

    public List<ViewFollower> Followers = new List<ViewFollower>();

    public Transform UnitHolderTransform;

    public float MaxSpacing = 1;
    public float CardWidth = 0;
    public float HandWidth = 0;
    public float CardY = 0;
    public float CardZ = 0;
    public float CardZOffset = 1;


    public void Setup(BattleRow battleRow)
    {
        BattleRow = battleRow;
    }

    public void Clear()
    {
        List<ViewFollower> followersCopy = new List<ViewFollower>(Followers);
        foreach (ViewFollower viewFollower in followersCopy)
        {
            View.Instance.RemoveCard(viewFollower.Card);
        }

        Followers.Clear();
    }


    // Update is called once per frame
    void Update()
    {
        if (!isHuman) return;

        if (View.Instance.SelectionHandler.TryGetHeldFollower(out ViewFollower viewFollower))
        {
            Highlight.enabled = true;
        }
        else
        {
            Highlight.enabled = false;
        }
    }

    public void UpdateRow()
    {
        RefreshPositions();
    }

    List<float> xPositions = new List<float>();
    private void RefreshPositions()
    {
        int cardCount = Followers.Count;
        if (cardCount == 0) return;

        xPositions.Clear();

        float totalWidth = (cardCount * CardWidth) + (cardCount - 1 * MaxSpacing);
        float X = CardWidth/2 - totalWidth/2;
        for (int i = 0; i < Followers.Count; i++)
        {
            ViewFollower viewFollower = Followers[i];
            Vector3 newPos = new Vector3(X, CardY, CardZ - CardZOffset * i);

            viewFollower.transform.localPosition = newPos;
            xPositions.Add(viewFollower.transform.position.x);

            X += CardWidth + MaxSpacing;

            bool highlighted = false;

            if ((!isHuman && BattleRow.Owner.IsMyTurn) || AnimationHandler.IsAnimating) highlighted = false;
            else if (View.Instance.SelectionHandler.AttackingFollower == viewFollower) highlighted = true; // If it's attacking
            else if (View.Instance.SelectionHandler.AttackableTargets.Contains(viewFollower.Follower)) highlighted = true; // If it's a potential attack target
            else if (isHuman && !View.Instance.SelectionHandler.IsHoldingCard() && viewFollower.Follower.CanAttack()) highlighted = true; // nothing's held and it can attack
            else if (View.Instance.SelectionHandler.IsHoldingCard() && View.Instance.SelectionHandler.CurrentTargets.Contains(viewFollower.Follower)) highlighted = true; // is a potential target of a spell
            else if (View.Instance.SelectionHandler.SelectedRitual != null && View.Instance.SelectionHandler.PotentialRitualTargets.Contains(viewFollower.Follower)) highlighted = true; // Is a potential target of a ritual

            viewFollower.SetHighlight(highlighted);
        }

        if (!View.Instance.SelectionHandler.IsHoldingFollower() || !IsMouseOverThis() || !isHuman)
        {
            return;
        }

        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        int heldCardIndex = 0;

        for (int i = 0; i < xPositions.Count; i++)
        {
            if (mousePosition.x < xPositions[i])
            {
                break;
            }
            else
            {
                heldCardIndex++;
            }
        }

        // Now we make room for the held card
        cardCount++; 

        totalWidth = (cardCount * CardWidth) + (cardCount - 1 * MaxSpacing);
        X = CardWidth / 2 - totalWidth / 2;
        for (int i = 0; i < Followers.Count; i++)
        {
            if (i == heldCardIndex) X += CardWidth + MaxSpacing; // Make space for held card

            ViewFollower viewFollower = Followers[i];
            Vector3 newPos = new Vector3(X, CardY, CardZ - CardZOffset * i);

            viewFollower.transform.localPosition = newPos;
            xPositions.Add(viewFollower.transform.position.x);

            X += CardWidth + MaxSpacing;
        }
    }

    public bool IsMouseOverThis()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); // HOWTO Raycast
        if (Physics.Raycast(ray, out RaycastHit hitData, 1000, zoneLayer))
        {
            if (hitData.collider.gameObject == gameObject)
            {
                return true;
            }
        }
        return false;
    }

    public int GetPlacementIndex()
    {
        int cardCount = Followers.Count;
        if (cardCount == 0) return 0;

        xPositions.Clear();

        float totalWidth = (cardCount * CardWidth) + (cardCount - 1 * MaxSpacing);
        float X = CardWidth / 2 - totalWidth / 2;
        for (int i = 0; i < Followers.Count; i++)
        {
            ViewFollower viewFollower = Followers[i];
            Vector3 newPos = new Vector3(X, CardY, CardZ - CardZOffset * i);

            viewFollower.transform.localPosition = newPos;
            xPositions.Add(viewFollower.transform.position.x);

            X += CardWidth + MaxSpacing;
        }

        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        int heldCardIndex = 0;

        for (int i = 0; i < xPositions.Count; i++)
        {
            if (mousePosition.x < xPositions[i])
            {
                break;
            }
            else
            {
                heldCardIndex++;
            }
        }

        return heldCardIndex;
    }

    public void AddFollower(ViewFollower viewFollower, int index)
    {
        viewFollower.OnClick = FollowerInPlayClicked;
        viewFollower.transform.SetParent(UnitHolderTransform);
        Followers.Insert(index, viewFollower);
    }

    public void TryRemoveFollower(ViewFollower viewFollower)
    {
        Followers.Remove(viewFollower);
        RefreshPositions();
    }
    public void FollowerInPlayClicked(ViewTarget viewTarget)
    {
        ViewEventHandler.Instance.FireViewTargetInPlayClicked(viewTarget);
    }
    public void HighlightTargets(List<ITarget> targets)
    {
        foreach (ViewFollower follower in Followers)
        {
            if (targets.Contains(follower.Follower)) follower.SetHighlight(true);
            else follower.SetHighlight(false);
        }
    }

    public Vector3 GetPotentialFollowerPosition(int index)
    {
        float battleRowOffset = transform.position.z;
        Vector3 position = transform.position;
        int cardCount = Followers.Count + 1;

        float totalWidth = (cardCount * CardWidth) + (cardCount - 1 * MaxSpacing);
        float X = CardWidth / 2 - totalWidth / 2;
        for (int i = 0; i < cardCount; i++)
        {
            if (i == index)
            {
                //X += CardWidth + MaxSpacing; // Make space for held card
                return position + new Vector3(X, CardY, CardZ - CardZOffset * i);
            }

            X += CardWidth + MaxSpacing;
        }

        return position;
    }
}
