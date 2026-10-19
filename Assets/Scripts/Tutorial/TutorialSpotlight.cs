using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Turns live combat objects into the normalized screen rects the tutorial cover cuts out.
/// </summary>
public static class TutorialSpotlight
{
    const float Pad = 0.012f;
    static RectTransform endTurn;

    public static bool TryResolve(TutorialFocus focus, out Rect hole)
    {
        hole = default;
        if (focus == TutorialFocus.None)
            return false;

        if (focus == TutorialFocus.EndTurn)
            return TryEndTurnRect(out hole);

        if (!TryWorldRect(focus, out Rect screen))
            return false;

        hole = PadRect(screen);
        return hole.width > 0.005f && hole.height > 0.005f;
    }

    public static bool IsPointerOverEndTurn()
    {
        if (EventSystem.current == null || !TryGetEndTurn(out RectTransform rect))
            return false;

        Camera camera = null;
        Canvas canvas = rect.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            camera = canvas.worldCamera;

        return RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, camera);
    }

    static bool TryWorldRect(TutorialFocus focus, out Rect hole)
    {
        hole = default;
        Camera camera = Camera.main;
        if (camera == null || View.Instance == null || View.Instance.Player1 == null)
            return false;

        var targets = new List<Transform>();
        Collect(focus, targets);
        if (targets.Count == 0)
            return false;

        bool any = false;
        float minX = 1f, minY = 1f, maxX = 0f, maxY = 0f;
        for (int i = 0; i < targets.Count; i++)
        {
            if (!TryRendererBounds(targets[i], out Bounds bounds))
                continue;

            Encapsulate(camera, bounds, ref minX, ref minY, ref maxX, ref maxY);
            any = true;
        }

        if (!any)
            return false;

        hole = Rect.MinMaxRect(minX, minY, maxX, maxY);
        return true;
    }

    static void Collect(TutorialFocus focus, List<Transform> targets)
    {
        ViewPlayer player = View.Instance.Player1;
        ViewPlayer enemy = View.Instance.Player2;
        switch (focus)
        {
            case TutorialFocus.Gold:
                Add(targets, player.ViewResources != null ? player.ViewResources.GoldLabel : null);
                break;
            case TutorialFocus.Hand:
                AddCards(targets, player.HandHandler);
                break;
            case TutorialFocus.PlayerFollowers:
                AddFollowers(targets, player.BattleRow);
                break;
            case TutorialFocus.EnemyPlayer:
                Add(targets, enemy != null ? enemy.ViewPlayerPortrait : null);
                break;
            case TutorialFocus.PlayerHealth:
                Add(targets, player.ViewPlayerPortrait);
                break;
            case TutorialFocus.Offerings:
                AddOffering(targets, player, OfferingType.Blood);
                AddOffering(targets, player, OfferingType.Bone);
                AddOffering(targets, player, OfferingType.Crop);
                AddOffering(targets, player, OfferingType.Scroll);
                break;
            case TutorialFocus.Blood:
                AddOffering(targets, player, OfferingType.Blood);
                break;
            case TutorialFocus.Bone:
                AddOffering(targets, player, OfferingType.Bone);
                break;
            case TutorialFocus.Crop:
                AddOffering(targets, player, OfferingType.Crop);
                break;
            case TutorialFocus.Scroll:
                AddOffering(targets, player, OfferingType.Scroll);
                break;
            case TutorialFocus.Rituals:
                Add(targets, player.ViewMinorRitual);
                Add(targets, player.ViewMajorRitual);
                break;
            case TutorialFocus.ReadyRitual:
                AddReadyRitual(targets, player.ViewMinorRitual);
                AddReadyRitual(targets, player.ViewMajorRitual);
                if (targets.Count == 0)
                {
                    Add(targets, player.ViewMinorRitual);
                    Add(targets, player.ViewMajorRitual);
                }
                break;
        }
    }

    static void AddOffering(List<Transform> targets, ViewPlayer player, OfferingType type)
    {
        if (player.ViewResources == null)
            return;

        OfferingLabel label = type switch
        {
            OfferingType.Gold => player.ViewResources.GoldLabel,
            OfferingType.Blood => player.ViewResources.BloodLabel,
            OfferingType.Bone => player.ViewResources.BonesLabel,
            OfferingType.Crop => player.ViewResources.CropsLabel,
            OfferingType.Scroll => player.ViewResources.ScrollsLabel,
            _ => null,
        };
        Add(targets, label);
    }

    static void AddReadyRitual(List<Transform> targets, ViewRitual ritual)
    {
        if (ritual != null && ritual.Ritual != null && ritual.Ritual.CanPlay())
            Add(targets, ritual);
    }

    static void AddCards(List<Transform> targets, ViewHandHandler hand)
    {
        if (hand == null)
            return;

        for (int i = 0; i < hand.ViewCards.Count; i++)
            Add(targets, hand.ViewCards[i]);
    }

    static void AddFollowers(List<Transform> targets, ViewBattleRow row)
    {
        if (row == null)
            return;

        for (int i = 0; i < row.Followers.Count; i++)
            Add(targets, row.Followers[i]);

        if (targets.Count == 0)
            Add(targets, row);
    }

    static void Add(List<Transform> targets, Component component)
    {
        if (component != null)
            targets.Add(component.transform);
    }

    static bool TryRendererBounds(Transform root, out Bounds bounds)
    {
        bounds = default;
        if (root == null)
            return false;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        bool any = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null || !renderers[i].enabled)
                continue;

            if (!any)
            {
                bounds = renderers[i].bounds;
                any = true;
            }
            else
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }

        return any;
    }

    static void Encapsulate(Camera camera, Bounds bounds, ref float minX, ref float minY, ref float maxX, ref float maxY)
    {
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;
        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 corner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                    Vector3 screen = camera.WorldToScreenPoint(corner);
                    if (screen.z < 0f)
                        continue;

                    float nx = screen.x / Mathf.Max(1f, UnityEngine.Screen.width);
                    float ny = screen.y / Mathf.Max(1f, UnityEngine.Screen.height);
                    minX = Mathf.Min(minX, nx);
                    minY = Mathf.Min(minY, ny);
                    maxX = Mathf.Max(maxX, nx);
                    maxY = Mathf.Max(maxY, ny);
                }
            }
        }
    }

    static Rect PadRect(Rect hole)
    {
        float xMin = Mathf.Clamp01(hole.xMin - Pad);
        float yMin = Mathf.Clamp01(hole.yMin - Pad);
        float xMax = Mathf.Clamp01(hole.xMax + Pad);
        float yMax = Mathf.Clamp01(hole.yMax + Pad);
        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    static bool TryEndTurnRect(out Rect hole)
    {
        hole = default;
        if (!TryGetEndTurn(out RectTransform rect))
            return false;

        Camera camera = null;
        Canvas canvas = rect.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            camera = canvas.worldCamera;

        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        float minX = 1f, minY = 1f, maxX = 0f, maxY = 0f;
        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 screen = camera != null ? camera.WorldToScreenPoint(corners[i]) : corners[i];
            float nx = screen.x / Mathf.Max(1f, UnityEngine.Screen.width);
            float ny = screen.y / Mathf.Max(1f, UnityEngine.Screen.height);
            minX = Mathf.Min(minX, nx);
            minY = Mathf.Min(minY, ny);
            maxX = Mathf.Max(maxX, nx);
            maxY = Mathf.Max(maxY, ny);
        }

        hole = PadRect(Rect.MinMaxRect(minX, minY, maxX, maxY));
        return true;
    }

    static bool TryGetEndTurn(out RectTransform rect)
    {
        if (endTurn != null && endTurn.gameObject.activeInHierarchy)
        {
            rect = endTurn;
            return true;
        }

        endTurn = null;
        TMP_Text[] labels = Object.FindObjectsOfType<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i] == null || !labels[i].gameObject.activeInHierarchy || labels[i].text != "End Turn")
                continue;

            RectTransform parent = labels[i].transform.parent as RectTransform;
            endTurn = parent != null ? parent : labels[i].rectTransform;
            rect = endTurn;
            return true;
        }

        rect = null;
        return false;
    }
}
