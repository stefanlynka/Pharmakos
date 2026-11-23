using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Places a group of related popups (for example a ritual summary, the cards it names and
/// their keyword reminders) as one rigid block beside an anchor.
/// <para>
/// Placing each popup on its own lets an early piece take the only side with room and push
/// the rest off-screen. Here the whole block is tried on each side of the anchor, clamped
/// onto the screen as a unit, and the side where it fits without covering the anchor wins.
/// </para>
/// Items are added in a local frame that grows outward to the right from x = 0, with the
/// first item (the head) centred on y = 0. Placing on the left mirrors the block, so the
/// head always sits next to the anchor and later items extend away from it.
/// </summary>
public class PopupGroupLayout
{
    readonly List<Rect> _local = new List<Rect>();
    readonly List<Rect> _placed = new List<Rect>();

    public int Count => _local.Count;

    public void Clear()
    {
        _local.Clear();
        _placed.Clear();
    }

    /// <summary>Adds an item in the local frame and returns its index.</summary>
    public int Add(Rect localRect)
    {
        _local.Add(localRect);
        return _local.Count - 1;
    }

    /// <summary>The item's rect in container space after <see cref="Place"/>.</summary>
    public Rect GetPlaced(int index) => _placed[index];

    /// <summary>
    /// Chooses the side of <paramref name="anchor"/> for the block and fills the placed rects.
    /// Sides are tried in the usual popup fallback order starting from <paramref name="preferred"/>.
    /// </summary>
    /// <param name="obstacles">Rects the block must not cover. Defaults to the anchor alone;
    /// pass the real pieces when the anchor is a union with empty space in it.</param>
    /// <param name="alignY">Height to centre the head on when placed left or right.
    /// Defaults to the anchor's centre.</param>
    public bool Place(Rect anchor, Rect screen, PopupPosition preferred, float gap,
        IReadOnlyList<Rect> obstacles = null, float? alignY = null)
    {
        _placed.Clear();
        if (_local.Count == 0)
            return false;

        PopupPosition[] order = CandidateOrder(preferred);
        float bestScore = float.MaxValue;
        bool bestMirrored = false;
        Vector2 bestOffset = Vector2.zero;

        for (int i = 0; i < order.Length; i++)
        {
            PopupPosition side = order[i];
            switch (side)
            {
                case PopupPosition.Right:
                    Consider(side, mirrored: false, i * 1000f);
                    break;
                case PopupPosition.Left:
                    Consider(side, mirrored: true, i * 1000f);
                    break;
                default:
                    // Above or below, the block can grow either way. Clamping decides.
                    Consider(side, mirrored: false, i * 1000f);
                    Consider(side, mirrored: true, i * 1000f);
                    break;
            }
        }

        for (int i = 0; i < _local.Count; i++)
            _placed.Add(Offset(Orient(_local[i], bestMirrored), bestOffset));
        return true;

        void Consider(PopupPosition side, bool mirrored, float orderCost)
        {
            Rect head = Orient(_local[0], mirrored);
            Rect bounds = LocalBounds(mirrored);
            float sideY = (alignY ?? anchor.center.y) - head.center.y;
            Vector2 offset;
            switch (side)
            {
                case PopupPosition.Right:
                    offset = new Vector2(anchor.xMax + gap - bounds.xMin, sideY);
                    break;
                case PopupPosition.Left:
                    offset = new Vector2(anchor.xMin - gap - bounds.xMax, sideY);
                    break;
                case PopupPosition.Above:
                    offset = new Vector2(anchor.center.x - head.center.x, anchor.yMax + gap - bounds.yMin);
                    break;
                default:
                    offset = new Vector2(anchor.center.x - head.center.x, anchor.yMin - gap - bounds.yMax);
                    break;
            }

            Rect placedBounds = Offset(bounds, offset);
            Vector2 clampShift = ClampShift(placedBounds, screen);
            offset += clampShift;
            placedBounds = Offset(bounds, offset);

            float anchorOverlap = 0f;
            for (int j = 0; j < _local.Count; j++)
            {
                Rect item = Offset(Orient(_local[j], mirrored), offset);
                if (obstacles == null)
                {
                    anchorOverlap += OverlapArea(item, anchor);
                    continue;
                }

                for (int k = 0; k < obstacles.Count; k++)
                    anchorOverlap += OverlapArea(item, obstacles[k]);
            }

            // Spilling off-screen is worst, then covering the anchor, then leaving the
            // preferred side, then how far the clamp had to drag the block.
            float score = OffScreenAmount(placedBounds, screen) * 1000000f
                + anchorOverlap * 100f
                + orderCost
                + clampShift.magnitude;

            if (score >= bestScore)
                return;

            bestScore = score;
            bestMirrored = mirrored;
            bestOffset = offset;
        }
    }

    Rect LocalBounds(bool mirrored)
    {
        Rect bounds = Orient(_local[0], mirrored);
        for (int i = 1; i < _local.Count; i++)
        {
            Rect r = Orient(_local[i], mirrored);
            bounds = Rect.MinMaxRect(
                Mathf.Min(bounds.xMin, r.xMin),
                Mathf.Min(bounds.yMin, r.yMin),
                Mathf.Max(bounds.xMax, r.xMax),
                Mathf.Max(bounds.yMax, r.yMax));
        }

        return bounds;
    }

    static Rect Orient(Rect rect, bool mirrored) =>
        mirrored ? new Rect(-rect.xMax, rect.y, rect.width, rect.height) : rect;

    static Rect Offset(Rect rect, Vector2 offset) =>
        new Rect(rect.x + offset.x, rect.y + offset.y, rect.width, rect.height);

    // Smallest shift that brings the rect inside the bounds. A rect larger than the bounds
    // keeps its top-left inside, which is where the reading starts.
    static Vector2 ClampShift(Rect rect, Rect bounds)
    {
        if (bounds.width <= 0f || bounds.height <= 0f)
            return Vector2.zero;

        float x = 0f;
        if (rect.xMax > bounds.xMax)
            x = bounds.xMax - rect.xMax;
        if (rect.xMin + x < bounds.xMin)
            x = bounds.xMin - rect.xMin;

        float y = 0f;
        if (rect.yMin < bounds.yMin)
            y = bounds.yMin - rect.yMin;
        if (rect.yMax + y > bounds.yMax)
            y = bounds.yMax - rect.yMax;

        return new Vector2(x, y);
    }

    static float OffScreenAmount(Rect rect, Rect bounds)
    {
        if (bounds.width <= 0f || bounds.height <= 0f)
            return 0f;

        return Mathf.Max(0f, bounds.xMin - rect.xMin)
            + Mathf.Max(0f, rect.xMax - bounds.xMax)
            + Mathf.Max(0f, bounds.yMin - rect.yMin)
            + Mathf.Max(0f, rect.yMax - bounds.yMax);
    }

    static float OverlapArea(Rect a, Rect b)
    {
        if (b.width <= 0f || b.height <= 0f)
            return 0f;

        float x = Mathf.Max(0f, Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin));
        float y = Mathf.Max(0f, Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin));
        return x * y;
    }

    static PopupPosition[] CandidateOrder(PopupPosition preferred)
    {
        switch (preferred)
        {
            case PopupPosition.Above:
                return new[] { PopupPosition.Above, PopupPosition.Below, PopupPosition.Right, PopupPosition.Left };
            case PopupPosition.Below:
                return new[] { PopupPosition.Below, PopupPosition.Above, PopupPosition.Right, PopupPosition.Left };
            case PopupPosition.Left:
                return new[] { PopupPosition.Left, PopupPosition.Right, PopupPosition.Above, PopupPosition.Below };
            default:
                return new[] { PopupPosition.Right, PopupPosition.Left, PopupPosition.Above, PopupPosition.Below };
        }
    }
}
