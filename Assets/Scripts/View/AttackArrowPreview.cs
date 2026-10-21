using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Streams of 3D arrows for attacks, spell targeting, and ritual targeting.
/// Hovering an attacker shows one arc to each legal target.
/// Selecting an attacker, a spell, or a ritual aims one arc at the board point under the cursor.
/// Aphrodite Minor also arcs from a hovered legal follower to the enemy it would take with it.
/// </summary>
public class AttackArrowPreview
{
    const string ArrowResourcePath = "Prefabs/Misc/3DArrow";
    const float ArrowSpeed = 4.5f;
    const float ArrowVisualScale = 2f;
    const float ArrowSpacing = 2.8f * ArrowVisualScale;
    const float TargetArrowLength = 1.65f;
    public const float ArcHeight = 2.4f;
    public const float SpellArcHeight = 6f;
    const float HideDepth = 1.75f;
    const float AppearBelowTop = 2f;
    const float MinPathLength = 0.8f;
    const int MaxArrows = 48;
    const int ArcSamples = 16;

    GameObject arrowPrefab;
    bool loggedMissingPrefab;
    bool calibrated;
    float arrowScale = 1f;
    float arrowWorldLength = TargetArrowLength;
    Vector3 tipLocal = Vector3.left;
    Vector3 faceLocal = Vector3.forward;

    readonly List<ArrowStream> streams = new List<ArrowStream>();
    readonly Stack<Transform> pool = new Stack<Transform>();

    int shownSourceId = int.MinValue;
    int shownTargetSignature;
    bool showingAim;

    class ArrowStream
    {
        public readonly List<Transform> Arrows = new List<Transform>();
        public readonly float[] ArcDistance = new float[ArcSamples + 1];
        public readonly float[] SecondArcDistance = new float[ArcSamples + 1];
        public float Distance;
    }

    struct StreamAim
    {
        public Vector3 End;
        public bool HasWaypoint;
        public Vector3 Waypoint;
    }

    struct Flight
    {
        public Vector3 LeadStart;
        public Vector3 Launch;
        public Vector3 Control;
        public Vector3 FirstOut;
        public Vector3 Waypoint;
        public Vector3 SecondIn;
        public Vector3 SecondOut;
        public Vector3 Middle;
        public Vector3 TailEnd;
        public Vector3 ExitDirection;
        public bool HasWaypoint;
        public float LeadLength;
        public float ArcLength;
        public float SecondArcLength;
        public float TailLength;
        public float TotalLength;
    }

    readonly List<StreamAim> hoverAims = new List<StreamAim>();
    readonly List<float> emptySlots = new List<float>();
    readonly List<Vector3> emptySlotPoints = new List<Vector3>();

    public void Tick()
    {
        if (!CanShow())
        {
            Hide();
            return;
        }

        if (!View.Instance.IsInteractible)
        {
            Hide();
            return;
        }

        SelectionHandler selection = View.Instance.SelectionHandler;
        if (selection.AttackingFollower != null)
        {
            ShowAim(selection.AttackingFollower);
            return;
        }

        if (selection.IsAimingSpell())
        {
            ShowSpellAim(selection.HeldCard as ViewSpell);
            return;
        }

        if (selection.SelectedRitual != null && selection.PotentialRitualTargets.Count > 0)
        {
            ShowRitualAim(selection.SelectedRitual);
            return;
        }

        if (selection.IsSelectingTarget())
        {
            Hide();
            return;
        }

        ViewFollower hovered = GetHoveredHumanAttacker(selection);
        if (hovered == null)
        {
            Hide();
            return;
        }

        List<ITarget> targets = hovered.Follower.GetAttackTargets();
        if (targets == null || targets.Count == 0)
        {
            Hide();
            return;
        }

        ShowHover(hovered, targets);
    }

    public void Clear()
    {
        Hide();

        while (pool.Count > 0)
        {
            Transform arrow = pool.Pop();
            if (arrow != null)
                Object.Destroy(arrow.gameObject);
        }
    }

    bool CanShow()
    {
        if (View.Instance == null || Controller.Instance == null)
            return false;
        if (!Controller.Instance.GameRunning || Controller.Instance.GamePaused)
            return false;
        if (ContentScrollView.BlocksGameInput || TutorialPresenter.IsBlocking)
            return false;
        if (View.Instance.MenuSelectionHandler != null && View.Instance.MenuSelectionHandler.IsActive)
            return false;
        if (GetCamera() == null)
            return false;

        if (arrowPrefab == null)
        {
            arrowPrefab = Resources.Load<GameObject>(ArrowResourcePath);
            if (arrowPrefab == null)
            {
                if (!loggedMissingPrefab)
                {
                    loggedMissingPrefab = true;
                    Debug.LogError("Missing attack arrow prefab at Resources/" + ArrowResourcePath);
                }
                return false;
            }
        }

        return true;
    }

    void ShowHover(ViewFollower attacker, List<ITarget> targets)
    {
        int signature = TargetSignature(targets);
        if (showingAim || shownSourceId != attacker.Follower.ID || shownTargetSignature != signature)
        {
            Hide();
            showingAim = false;
            shownSourceId = attacker.Follower.ID;
            shownTargetSignature = signature;
        }

        Camera camera = GetCamera();
        Vector3 boardNormal = BoardNormalTowardCamera(attacker.transform, camera);
        if (!TryGetCardLaunch(attacker, out Vector3 launch, out _))
        {
            Hide();
            return;
        }

        hoverAims.Clear();
        for (int i = 0; i < targets.Count; i++)
        {
            if (!TryGetTargetOccluder(targets[i], out Vector3 middle, out _, out _))
                continue;

            if (targets[i] is Player && HasFollowerAttackTarget(targets) && TryGetEmptyAttackSlots(attacker, emptySlotPoints))
            {
                for (int s = 0; s < emptySlotPoints.Count; s++)
                {
                    hoverAims.Add(new StreamAim
                    {
                        End = middle,
                        HasWaypoint = true,
                        Waypoint = emptySlotPoints[s]
                    });
                }
            }
            else
            {
                hoverAims.Add(new StreamAim { End = middle });
            }
        }

        SetStreamCount(hoverAims.Count);
        for (int i = 0; i < hoverAims.Count; i++)
        {
            StreamAim aim = hoverAims[i];
            AdvanceStream(streams[i], attacker.transform, launch, aim.End, boardNormal, aim.HasWaypoint, aim.Waypoint, ArcHeight, false);
        }
    }

    void ShowAim(ViewFollower attacker)
    {
        if (attacker == null || attacker.Follower == null)
        {
            Hide();
            return;
        }

        Camera camera = GetCamera();
        if (camera == null)
        {
            Hide();
            return;
        }

        Vector3 boardNormal = BoardNormalTowardCamera(attacker.transform, camera);
        if (!TryGetCursorOnBoard(camera, attacker.transform.position, boardNormal, out Vector3 boardPoint))
        {
            Hide();
            return;
        }

        if (!showingAim || shownSourceId != attacker.Follower.ID)
        {
            Hide();
            showingAim = true;
            shownSourceId = attacker.Follower.ID;
            shownTargetSignature = 0;
        }

        if (!TryGetCardLaunch(attacker, out Vector3 launch, out _))
        {
            Hide();
            return;
        }

        SetStreamCount(1);
        AdvanceStream(streams[0], attacker.transform, launch, boardPoint, boardNormal, false, default, ArcHeight, false);
    }

    void ShowSpellAim(ViewSpell spell)
    {
        if (spell == null)
        {
            Hide();
            return;
        }

        Camera camera = GetCamera();
        if (camera == null)
        {
            Hide();
            return;
        }

        if (View.Instance.SelectionHandler.IsHoveringOverThisCard(spell))
        {
            Hide();
            return;
        }

        Vector3 boardNormal = BoardNormalTowardCamera(spell.transform, camera);
        Vector3 planePoint = View.Instance.Player1.BattleRow != null
            ? View.Instance.Player1.BattleRow.transform.position
            : spell.transform.position;
        if (!TryGetCursorOnBoard(camera, planePoint, boardNormal, out Vector3 boardPoint))
        {
            Hide();
            return;
        }

        int spellId = spell.Card != null ? spell.Card.ID : 0;
        if (!showingAim || shownSourceId != spellId)
        {
            Hide();
            showingAim = true;
            shownSourceId = spellId;
            shownTargetSignature = 0;
        }

        if (!TryGetCardLaunch(spell, out Vector3 launch, out _))
        {
            Hide();
            return;
        }

        SetStreamCount(1);
        AdvanceStream(streams[0], spell.transform, launch, boardPoint, boardNormal, false, default, SpellArcHeight, true);
    }

    void ShowRitualAim(ViewRitual ritual)
    {
        if (ritual == null)
        {
            Hide();
            return;
        }

        Camera camera = GetCamera();
        if (camera == null)
        {
            Hide();
            return;
        }

        if (View.Instance.SelectionHandler.CurrentHover == ritual)
        {
            Hide();
            return;
        }

        Vector3 boardNormal = BoardNormalTowardCamera(ritual.transform, camera);
        Vector3 planePoint = View.Instance.Player1.BattleRow != null
            ? View.Instance.Player1.BattleRow.transform.position
            : ritual.transform.position;
        if (!TryGetCursorOnBoard(camera, planePoint, boardNormal, out Vector3 boardPoint))
        {
            Hide();
            return;
        }

        if (!TryGetRitualLaunch(ritual, out Vector3 launch, out _))
        {
            Hide();
            return;
        }

        int ritualId = ritual.Ritual != null ? ritual.Ritual.ID : ritual.GetInstanceID();
        if (!showingAim || shownSourceId != ritualId)
        {
            Hide();
            showingAim = true;
            shownSourceId = ritualId;
            shownTargetSignature = 0;
        }

        bool showElopement = TryGetAphroditeElopement(out ViewFollower eloper, out Vector3 elopeLaunch, out Vector3 partnerMiddle);
        SetStreamCount(showElopement ? 2 : 1);
        AdvanceStream(streams[0], ritual.transform, launch, boardPoint, boardNormal, false, default, SpellArcHeight, true);
        if (showElopement)
        {
            Vector3 elopeNormal = BoardNormalTowardCamera(eloper.transform, camera);
            AdvanceStream(streams[1], eloper.transform, elopeLaunch, partnerMiddle, elopeNormal, false, default, ArcHeight, false);
        }
    }

    void AdvanceStream(ArrowStream stream, Transform facing, Vector3 launch, Vector3 end, Vector3 boardNormal, bool hasWaypoint, Vector3 waypoint, float arcHeight, bool keepArcHeight)
    {
        if (!TryBuildFlight(launch, end, boardNormal, hasWaypoint, waypoint, arcHeight, keepArcHeight, out Flight flight))
        {
            ClearStream(stream);
            return;
        }

        // Distances are measured forward from the card. The hidden lead grows as the
        // shot gets shallower; anchoring there dragged every arrow backward.
        FillArcDistances(stream, ref flight);
        stream.Distance = Mathf.Repeat(stream.Distance + ArrowSpeed * Time.deltaTime, ArrowSpacing);

        float reach = flight.ArcLength + flight.SecondArcLength + flight.TailLength;
        float phase = stream.Distance;
        int first = Mathf.FloorToInt((-flight.LeadLength - phase) / ArrowSpacing) + 1;
        int last = Mathf.FloorToInt((reach - phase) / ArrowSpacing);
        if (last < first)
        {
            ClearStream(stream);
            return;
        }

        if (last - first + 1 > MaxArrows)
            first = last - MaxArrows + 1;

        int count = last - first + 1;
        EnsureArrowCount(stream, count);
        Vector3 cardUp = facing != null && facing.up.sqrMagnitude > 0.0001f ? facing.up : Vector3.up;
        for (int n = 0; n < count; n++)
        {
            float fromLaunch = phase + (first + n) * ArrowSpacing;
            PlaceArrow(stream.Arrows[n], stream, flight, fromLaunch + flight.LeadLength, boardNormal, cardUp);
        }
    }

    bool TryBuildFlight(Vector3 launch, Vector3 end, Vector3 boardNormal, bool hasWaypoint, Vector3 waypoint, float arcHeight, bool keepArcHeight, out Flight flight)
    {
        flight = default;
        if (hasWaypoint)
            return TryBuildWaypointFlight(launch, waypoint, end, boardNormal, out flight);

        // One arc from a fixed point on the source to the target.
        // The exit direction is the arc's own initial tangent, so the arrow is
        // aimed at that target as soon as it appears.
        Vector3 toTarget = end - launch;
        if (toTarget.sqrMagnitude < MinPathLength * MinPathLength)
            return false;

        float bow = keepArcHeight ? arcHeight : Mathf.Min(arcHeight, toTarget.magnitude * 0.35f);
        Vector3 control = (launch + end) * 0.5f + boardNormal * bow;
        Vector3 exitDir = control - launch;
        if (exitDir.sqrMagnitude < 0.0001f)
            return false;
        exitDir.Normalize();

        float towardCamera = Vector3.Dot(exitDir, boardNormal);
        float lead = arrowWorldLength + 0.45f;
        if (towardCamera > 0.05f)
            lead = Mathf.Max(lead, (HideDepth + arrowWorldLength * 0.35f) / towardCamera);

        Vector3 arriveDir = end - control;
        if (arriveDir.sqrMagnitude < 0.0001f)
            arriveDir = toTarget;
        arriveDir.Normalize();

        flight.Launch = launch;
        flight.LeadStart = launch - exitDir * lead;
        flight.Control = control;
        flight.Middle = end;
        flight.TailEnd = end + arriveDir * (arrowWorldLength + 0.2f);
        flight.ExitDirection = exitDir;

        float hiddenLead = ArrowSpacing + arrowWorldLength * 0.5f;
        if (lead < hiddenLead)
        {
            lead = hiddenLead;
            flight.LeadStart = launch - exitDir * lead;
        }

        flight.LeadLength = lead;
        flight.ArcLength = EstimateLength(launch, control, end);
        flight.TailLength = Vector3.Distance(end, flight.TailEnd);
        flight.TotalLength = flight.LeadLength + flight.ArcLength + flight.TailLength;
        return flight.ArcLength > 0.05f;
    }

    bool TryBuildWaypointFlight(Vector3 launch, Vector3 waypoint, Vector3 end, Vector3 boardNormal, out Flight flight)
    {
        flight = default;
        Vector3 toSlot = waypoint - launch;
        Vector3 toPlayer = end - waypoint;
        if (toSlot.sqrMagnitude < 0.05f || toPlayer.sqrMagnitude < 0.05f)
            return false;

        Vector3 flatIn = Vector3.ProjectOnPlane(toSlot, boardNormal);
        Vector3 flatOut = Vector3.ProjectOnPlane(toPlayer, boardNormal);
        if (flatIn.sqrMagnitude < 0.0001f)
            flatIn = toSlot;
        if (flatOut.sqrMagnitude < 0.0001f)
            flatOut = toPlayer;

        Vector3 through = flatIn.normalized + flatOut.normalized;
        if (through.sqrMagnitude < 0.04f)
            through = flatOut;
        through.Normalize();

        float handleIn = Mathf.Clamp(toSlot.magnitude * 0.45f, 0.45f, 8f);
        float handleOut = Mathf.Clamp(toPlayer.magnitude * 0.45f, 0.45f, 8f);
        Vector3 exitDir = (flatIn.normalized + boardNormal * 0.35f).normalized;
        Vector3 endTangent = (flatOut.normalized - boardNormal * 0.45f).normalized;

        flight.HasWaypoint = true;
        flight.Launch = launch;
        flight.Waypoint = waypoint;
        flight.Middle = end;
        flight.ExitDirection = exitDir;
        flight.Control = launch + exitDir * handleIn;
        flight.FirstOut = waypoint - through * handleIn;
        flight.SecondIn = waypoint + through * handleOut;
        flight.SecondOut = end - endTangent * handleOut;
        flight.TailEnd = end + endTangent * (arrowWorldLength + 0.2f);

        float towardCamera = Vector3.Dot(exitDir, boardNormal);
        float lead = arrowWorldLength + 0.45f;
        if (towardCamera > 0.05f)
            lead = Mathf.Max(lead, (HideDepth + arrowWorldLength * 0.35f) / towardCamera);
        lead = Mathf.Max(lead, ArrowSpacing + arrowWorldLength * 0.5f);

        flight.LeadStart = launch - exitDir * lead;
        flight.LeadLength = lead;
        flight.ArcLength = 1f;
        flight.SecondArcLength = 1f;
        flight.TailLength = Vector3.Distance(end, flight.TailEnd);
        flight.TotalLength = flight.LeadLength + flight.ArcLength + flight.SecondArcLength + flight.TailLength;
        return true;
    }

    void FillArcDistances(ArrowStream stream, ref Flight flight)
    {
        if (flight.HasWaypoint)
        {
            flight.ArcLength = FillCurve(stream.ArcDistance, flight.Launch, flight.Control, flight.FirstOut, flight.Waypoint, true);
            flight.SecondArcLength = FillCurve(stream.SecondArcDistance, flight.Waypoint, flight.SecondIn, flight.SecondOut, flight.Middle, true);
        }
        else
        {
            flight.ArcLength = FillCurve(stream.ArcDistance, flight.Launch, flight.Control, default, flight.Middle, false);
            flight.SecondArcLength = 0f;
        }

        flight.TotalLength = flight.LeadLength + flight.ArcLength + flight.SecondArcLength + flight.TailLength;
    }

    static float FillCurve(float[] distances, Vector3 start, Vector3 control, Vector3 secondControl, Vector3 end, bool cubic)
    {
        Vector3 previous = start;
        distances[0] = 0f;
        for (int i = 1; i <= ArcSamples; i++)
        {
            float t = i / (float)ArcSamples;
            Vector3 point = cubic
                ? Cubic(start, control, secondControl, end, t)
                : Quadratic(start, control, end, t);
            distances[i] = distances[i - 1] + Vector3.Distance(previous, point);
            previous = point;
        }

        return Mathf.Max(0.05f, distances[ArcSamples]);
    }

    void PlaceArrow(Transform arrow, ArrowStream stream, Flight flight, float distance, Vector3 boardNormal, Vector3 cardUp)
    {
        distance = Mathf.Clamp(distance, 0f, flight.TotalLength);

        Vector3 tangent = SampleTangent(stream, flight, distance);
        if (tangent.sqrMagnitude < 0.0001f)
            return;

        Vector3 face = Vector3.ProjectOnPlane(boardNormal, tangent);
        if (face.sqrMagnitude < 0.04f)
            face = Vector3.ProjectOnPlane(cardUp, tangent);
        if (face.sqrMagnitude < 0.0001f)
            return;

        arrow.position = SamplePosition(stream, flight, distance);
        arrow.localScale = Vector3.one * arrowScale;
        arrow.rotation = AlignRotation(tipLocal, tangent, faceLocal, face);
    }

    static Vector3 SamplePosition(ArrowStream stream, Flight flight, float distance)
    {
        if (distance < flight.LeadLength)
        {
            float t = flight.LeadLength > 0.0001f ? distance / flight.LeadLength : 0f;
            return Vector3.Lerp(flight.LeadStart, flight.Launch, t);
        }

        distance -= flight.LeadLength;
        if (distance < flight.ArcLength)
        {
            float t = ArcParameter(stream.ArcDistance, distance);
            if (flight.HasWaypoint)
                return Cubic(flight.Launch, flight.Control, flight.FirstOut, flight.Waypoint, t);
            return Quadratic(flight.Launch, flight.Control, flight.Middle, t);
        }

        distance -= flight.ArcLength;
        if (flight.HasWaypoint && distance < flight.SecondArcLength)
        {
            float t = ArcParameter(stream.SecondArcDistance, distance);
            return Cubic(flight.Waypoint, flight.SecondIn, flight.SecondOut, flight.Middle, t);
        }

        if (flight.HasWaypoint)
            distance -= flight.SecondArcLength;
        float tail = flight.TailLength > 0.0001f ? Mathf.Clamp01(distance / flight.TailLength) : 1f;
        return Vector3.Lerp(flight.Middle, flight.TailEnd, tail);
    }

    static Vector3 SampleTangent(ArrowStream stream, Flight flight, float distance)
    {
        if (distance < flight.LeadLength)
            return flight.ExitDirection;

        distance -= flight.LeadLength;
        if (distance < flight.ArcLength)
        {
            float t = ArcParameter(stream.ArcDistance, distance);
            if (flight.HasWaypoint)
                return CubicTangent(flight.Launch, flight.Control, flight.FirstOut, flight.Waypoint, t);
            return QuadraticTangent(flight.Launch, flight.Control, flight.Middle, t);
        }

        distance -= flight.ArcLength;
        if (flight.HasWaypoint && distance < flight.SecondArcLength)
        {
            float t = ArcParameter(stream.SecondArcDistance, distance);
            return CubicTangent(flight.Waypoint, flight.SecondIn, flight.SecondOut, flight.Middle, t);
        }

        return flight.TailEnd - flight.Middle;
    }

    static float ArcParameter(float[] distances, float distance)
    {
        int lo = 0;
        int hi = ArcSamples - 1;
        while (lo < hi)
        {
            int mid = (lo + hi) >> 1;
            if (distances[mid + 1] < distance)
                lo = mid + 1;
            else
                hi = mid;
        }

        float span = distances[lo + 1] - distances[lo];
        float u = span > 0.0001f ? Mathf.Clamp01((distance - distances[lo]) / span) : 0f;
        return (lo + u) / ArcSamples;
    }

    void SetStreamCount(int count)
    {
        while (streams.Count < count)
        {
            ArrowStream stream = new ArrowStream();
            stream.Distance = streams.Count * ArrowSpacing;
            streams.Add(stream);
        }

        while (streams.Count > count)
        {
            int last = streams.Count - 1;
            ClearStream(streams[last]);
            streams.RemoveAt(last);
        }
    }

    void Hide()
    {
        SetStreamCount(0);
        showingAim = false;
        shownSourceId = int.MinValue;
        shownTargetSignature = 0;
    }

    void EnsureArrowCount(ArrowStream stream, int count)
    {
        while (stream.Arrows.Count < count)
        {
            Transform arrow = Borrow();
            if (arrow == null)
                return;
            stream.Arrows.Add(arrow);
        }

        while (stream.Arrows.Count > count)
        {
            int last = stream.Arrows.Count - 1;
            Release(stream.Arrows[last]);
            stream.Arrows.RemoveAt(last);
        }
    }

    void ClearStream(ArrowStream stream)
    {
        for (int i = 0; i < stream.Arrows.Count; i++)
            Release(stream.Arrows[i]);
        stream.Arrows.Clear();
    }

    Transform Borrow()
    {
        Transform arrow;
        if (pool.Count > 0)
        {
            arrow = pool.Pop();
            if (arrow == null)
                return Borrow();
        }
        else
        {
            GameObject instance = Object.Instantiate(arrowPrefab, View.Instance.transform);
            instance.name = "AttackArrow";
            arrow = instance.transform;
            Calibrate(arrow);
        }

        arrow.gameObject.SetActive(true);
        arrow.localScale = Vector3.one * arrowScale;
        Renderer[] renderers = arrow.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderers[i].receiveShadows = false;
        }
        return arrow;
    }

    void Release(Transform arrow)
    {
        if (arrow == null)
            return;

        arrow.gameObject.SetActive(false);
        pool.Push(arrow);
    }

    void Calibrate(Transform arrow)
    {
        if (calibrated)
            return;

        Quaternion rotation = arrow.rotation;
        Vector3 scale = arrow.localScale;
        arrow.rotation = Quaternion.identity;
        arrow.localScale = Vector3.one;

        Renderer renderer = arrow.GetComponentInChildren<Renderer>();
        Vector3 size = renderer != null ? renderer.localBounds.size : Vector3.zero;
        if (renderer == null || size.sqrMagnitude < 0.0001f)
        {
            arrow.rotation = rotation;
            arrow.localScale = scale;
            return;
        }

        int longAxis = LargestAxis(size);
        int thinAxis = SmallestAxis(size);
        if (longAxis == thinAxis)
            thinAxis = (longAxis + 1) % 3;

        // The mesh comes to a point on the negative end of its long axis.
        Vector3 meshTip = Vector3.zero;
        meshTip[longAxis] = -1f;
        Vector3 meshFace = Vector3.zero;
        meshFace[thinAxis] = 1f;

        tipLocal = arrow.InverseTransformDirection(renderer.transform.TransformDirection(meshTip)).normalized;
        faceLocal = arrow.InverseTransformDirection(renderer.transform.TransformDirection(meshFace)).normalized;

        Vector3 lossy = renderer.transform.lossyScale;
        float renderedLength = Mathf.Max(
            Mathf.Abs(size.x * lossy.x),
            Mathf.Abs(size.y * lossy.y),
            Mathf.Abs(size.z * lossy.z));
        arrowScale = (renderedLength > 0.4f && renderedLength < 4f
            ? 1f
            : TargetArrowLength / Mathf.Max(0.001f, renderedLength)) * ArrowVisualScale;
        arrowWorldLength = Mathf.Max(0.2f, renderedLength * arrowScale);
        calibrated = true;

        arrow.rotation = rotation;
        arrow.localScale = scale;
    }

    static bool TryGetAphroditeElopement(out ViewFollower source, out Vector3 launch, out Vector3 partnerMiddle)
    {
        source = null;
        launch = default;
        partnerMiddle = default;

        SelectionHandler selection = View.Instance.SelectionHandler;
        if (selection.SelectedRitual == null || selection.SelectedRitual.Ritual is not AphroditeMinor)
            return false;

        ViewFollower hovered = FindHoveredFollower(View.Instance.Player1.BattleRow, selection);
        if (hovered == null)
            hovered = FindHoveredFollower(View.Instance.Player2.BattleRow, selection);
        if (hovered == null || hovered.Follower == null)
            return false;
        if (!selection.PotentialRitualTargets.Contains(hovered.Follower))
            return false;

        Follower partner = hovered.Follower.GetClosestEnemy();
        if (partner == null)
            return false;
        if (!TryGetCardLaunch(hovered, out launch, out _))
            return false;
        if (!TryGetTargetOccluder(partner, out partnerMiddle, out _, out _))
            return false;

        source = hovered;
        return true;
    }

    static ViewFollower GetHoveredHumanAttacker(SelectionHandler selection)
    {
        ViewFollower hovered = FindHoveredFollower(View.Instance.Player1.BattleRow, selection);
        if (hovered == null)
            hovered = FindHoveredFollower(View.Instance.Player2.BattleRow, selection);
        if (hovered == null || hovered.Follower == null || hovered.Follower.Owner == null)
            return null;
        if (!hovered.Follower.Owner.IsHuman || !hovered.Follower.CanAttack())
            return null;

        return hovered;
    }

    static ViewFollower FindHoveredFollower(ViewBattleRow row, SelectionHandler selection)
    {
        if (row == null)
            return null;

        for (int i = 0; i < row.Followers.Count; i++)
        {
            ViewFollower follower = row.Followers[i];
            if (follower == null)
                continue;
            if (selection.IsHoveringOverThisCard(follower))
                return follower;
        }

        return null;
    }

    static bool TryGetTargetOccluder(ITarget target, out Vector3 middle, out Bounds bounds, out bool hasBounds)
    {
        middle = default;
        bounds = default;
        hasBounds = false;
        if (target == null || View.Instance == null)
            return false;

        ViewTarget view = View.Instance.GetViewTargetByID(target.GetID());
        if (view == null)
            return false;

        if (view is ViewFollower follower)
        {
            middle = follower.CardCollider != null
                ? follower.transform.TransformPoint(follower.CardCollider.center)
                : follower.transform.position;
            if (follower.CardCollider != null)
            {
                bounds = follower.CardCollider.bounds;
                hasBounds = true;
            }
            return true;
        }

        if (view is ViewPlayer player && player.ViewPlayerPortrait != null)
        {
            if (TryGetRendererBounds(player.ViewPlayerPortrait.transform, out bounds))
            {
                middle = bounds.center;
                hasBounds = true;
                return true;
            }

            middle = player.ViewPlayerPortrait.transform.position;
            return true;
        }

        middle = view.transform.position;
        return true;
    }

    static bool TryGetCardLaunch(ViewCard card, out Vector3 launch, out Vector3 up)
    {
        launch = default;
        up = Vector3.up;
        if (card == null)
            return false;

        up = card.transform.up;
        if (up.sqrMagnitude < 0.0001f)
            up = Vector3.up;
        up.Normalize();
        launch = TopOf(card) - up * AppearBelowTop;
        return true;
    }

    static bool TryGetRitualLaunch(ViewRitual ritual, out Vector3 launch, out Vector3 up)
    {
        launch = default;
        up = Vector3.up;
        if (ritual == null)
            return false;

        up = ritual.transform.up;
        if (up.sqrMagnitude < 0.0001f)
            up = Vector3.up;
        up.Normalize();

        bool found = false;
        Bounds bounds = default;
        Renderer[] renderers = ritual.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
                continue;
            if (renderer.GetComponent<TMPro.TMP_Text>() != null)
                continue;
            if (renderer.bounds.size.sqrMagnitude <= 0.0001f)
                continue;

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (!found)
        {
            launch = ritual.transform.position;
            return true;
        }

        Vector3 extents = bounds.extents;
        float half = Mathf.Abs(extents.x * up.x) + Mathf.Abs(extents.y * up.y) + Mathf.Abs(extents.z * up.z);
        launch = bounds.center + up * half - up * AppearBelowTop;
        return true;
    }

    static Vector3 TopOf(ViewCard card)
    {
        if (card.CardCollider == null)
            return card.transform.position;

        Vector3 localTop = card.CardCollider.center + Vector3.up * (card.CardCollider.size.y * 0.5f);
        return card.transform.TransformPoint(localTop);
    }

    static Vector3 BoardNormalTowardCamera(Transform card, Camera camera)
    {
        Vector3 normal = card.forward;
        if (normal.sqrMagnitude < 0.0001f)
            normal = Vector3.forward;

        Vector3 toCamera = camera.transform.position - card.position;
        if (Vector3.Dot(normal, toCamera) < 0f)
            normal = -normal;

        return normal.normalized;
    }

    static bool TryGetCursorOnBoard(Camera camera, Vector3 planePoint, Vector3 planeNormal, out Vector3 point)
    {
        point = default;
        Plane plane = new Plane(planeNormal, planePoint);
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        if (!plane.Raycast(ray, out float distance))
            return false;

        point = ray.GetPoint(distance);
        return true;
    }

    static bool TryGetRendererBounds(Transform root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        bool found = false;
        bounds = default;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null || !renderers[i].enabled)
                continue;
            if (renderers[i].bounds.size.sqrMagnitude <= 0.0001f)
                continue;

            if (!found)
            {
                bounds = renderers[i].bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }

        return found;
    }

    static Camera GetCamera()
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

    static int TargetSignature(List<ITarget> targets)
    {
        int signature = targets.Count;
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] == null)
                continue;
            signature = signature * 31 + targets[i].GetID();
        }
        return signature;
    }

    static int LargestAxis(Vector3 size)
    {
        int axis = 0;
        if (size.y > size[axis]) axis = 1;
        if (size.z > size[axis]) axis = 2;
        return axis;
    }

    static int SmallestAxis(Vector3 size)
    {
        int axis = 0;
        if (size.y < size[axis]) axis = 1;
        if (size.z < size[axis]) axis = 2;
        return axis;
    }

    static Vector3 Cubic(Vector3 start, Vector3 controlIn, Vector3 controlOut, Vector3 end, float t)
    {
        float u = 1f - t;
        return (u * u * u * start)
            + (3f * u * u * t * controlIn)
            + (3f * u * t * t * controlOut)
            + (t * t * t * end);
    }

    static Vector3 CubicTangent(Vector3 start, Vector3 controlIn, Vector3 controlOut, Vector3 end, float t)
    {
        float u = 1f - t;
        return (3f * u * u * (controlIn - start))
            + (6f * u * t * (controlOut - controlIn))
            + (3f * t * t * (end - controlOut));
    }

    bool TryGetEmptyAttackSlots(ViewFollower attacker, List<Vector3> points)
    {
        points.Clear();
        Follower follower = attacker.Follower;
        if (follower == null || follower.HasStaticEffect(StaticEffect.RangedAttacker))
            return false;

        Player enemy = follower.Owner != null ? follower.Owner.GetOtherPlayer() : null;
        if (enemy == null || View.Instance == null)
            return false;

        ViewBattleRow row = enemy.IsHuman ? View.Instance.Player1.BattleRow : View.Instance.Player2.BattleRow;
        if (row == null)
            return false;

        float position = follower.GetPositionInBattleRow();
        if (follower.HasStaticEffect(StaticEffect.LowVision))
            return false;

        enemy.BattleRow.CollectUnoccupiedPositionsInRange(position, emptySlots);
        bool evenAttacker = Mathf.Abs(position % 1f) == 0.5f;
        bool evenDefender = enemy.BattleRow.Followers.Count % 2 == 0;
        bool aligned = evenAttacker == evenDefender;
        if (emptySlots.Count >= (aligned ? 3 : 2))
            return false;

        if (aligned)
            KeepMiddleOfAdjacentGaps(position);

        for (int i = 0; i < emptySlots.Count; i++)
        {
            if (TryGetSlotCenter(row, emptySlots[i], attacker, out Vector3 center))
                points.Add(center);
        }

        return points.Count > 0;
    }

    void KeepMiddleOfAdjacentGaps(float middle)
    {
        bool hasMiddle = false;
        bool hasLeft = false;
        bool hasRight = false;
        for (int i = 0; i < emptySlots.Count; i++)
        {
            if (Mathf.Abs(emptySlots[i] - middle) < 0.01f)
                hasMiddle = true;
            else if (Mathf.Abs(emptySlots[i] - (middle - 1f)) < 0.01f)
                hasLeft = true;
            else if (Mathf.Abs(emptySlots[i] - (middle + 1f)) < 0.01f)
                hasRight = true;
        }

        if (!hasMiddle)
            return;
        if (hasLeft)
            RemoveEmptySlot(middle - 1f);
        if (hasRight)
            RemoveEmptySlot(middle + 1f);
    }

    void RemoveEmptySlot(float position)
    {
        for (int i = emptySlots.Count - 1; i >= 0; i--)
        {
            if (Mathf.Abs(emptySlots[i] - position) < 0.01f)
                emptySlots.RemoveAt(i);
        }
    }

    static bool HasFollowerAttackTarget(List<ITarget> targets)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] is Follower)
                return true;
        }

        return false;
    }

    static bool TryGetSlotCenter(ViewBattleRow row, float logicalPosition, ViewCard reference, out Vector3 center)
    {
        center = default;
        Transform holder = row.UnitHolderTransform != null ? row.UnitHolderTransform : row.transform;
        if (holder == null)
            return false;

        float step = row.CardWidth + row.MaxSpacing;
        if (step < 0.01f)
            step = 1f;

        float index = row.Followers.Count > 0
            ? logicalPosition + 0.5f * (row.Followers.Count - 1)
            : 0f;
        Vector3 local = new Vector3(logicalPosition * step, row.CardY, row.CardZ - row.CardZOffset * index);
        Vector3 root = holder.TransformPoint(local);

        Vector3 centerOffset = Vector3.zero;
        ViewCard offsetSource = row.Followers.Count > 0 ? row.Followers[0] : reference;
        if (offsetSource != null && offsetSource.CardCollider != null)
            centerOffset = offsetSource.transform.TransformVector(offsetSource.CardCollider.center);

        center = root + centerOffset;
        return true;
    }

    static Vector3 Quadratic(Vector3 start, Vector3 control, Vector3 end, float t)
    {
        float u = 1f - t;
        return (u * u * start) + (2f * u * t * control) + (t * t * end);
    }

    static Vector3 QuadraticTangent(Vector3 start, Vector3 control, Vector3 end, float t)
    {
        float u = 1f - t;
        return (2f * u * (control - start)) + (2f * t * (end - control));
    }

    static float EstimateLength(Vector3 start, Vector3 control, Vector3 end)
    {
        const int steps = 8;
        float length = 0f;
        Vector3 previous = start;
        for (int i = 1; i <= steps; i++)
        {
            Vector3 point = Quadratic(start, control, end, i / (float)steps);
            length += Vector3.Distance(previous, point);
            previous = point;
        }
        return length;
    }

    static Quaternion AlignRotation(Vector3 fromTip, Vector3 toTip, Vector3 fromFace, Vector3 toFace)
    {
        // Two FromToRotations twist the nose off the path whenever the roll
        // passes through 0° or 180°. That is the one-frame flick at the top of the arc.
        Vector3 tip = toTip.sqrMagnitude > 0.0001f ? toTip.normalized : Vector3.forward;
        Vector3 face = Vector3.ProjectOnPlane(toFace, tip);
        Vector3 localTip = fromTip.sqrMagnitude > 0.0001f ? fromTip.normalized : Vector3.left;
        Vector3 localFace = Vector3.ProjectOnPlane(fromFace, localTip);
        if (face.sqrMagnitude < 0.0001f || localFace.sqrMagnitude < 0.0001f)
            return Quaternion.FromToRotation(localTip, tip);

        return MapAxes(localTip, localFace.normalized, tip, face.normalized);
    }

    static Quaternion MapAxes(Vector3 fromPrimary, Vector3 fromSecondary, Vector3 toPrimary, Vector3 toSecondary)
    {
        Vector3 fromThird = Vector3.Cross(fromPrimary, fromSecondary);
        Vector3 toThird = Vector3.Cross(toPrimary, toSecondary);
        if (fromThird.sqrMagnitude < 0.0001f || toThird.sqrMagnitude < 0.0001f)
            return Quaternion.FromToRotation(fromPrimary, toPrimary);

        Quaternion from = Quaternion.LookRotation(fromThird, fromSecondary);
        Quaternion to = Quaternion.LookRotation(toThird, toSecondary);
        return to * Quaternion.Inverse(from);
    }

    /// <summary>
    /// One shot from a card to a target, using the same arc as the attack preview.
    /// Lead and tail stay zero until the caller knows the arrow's length.
    /// </summary>
    public struct DirectArc
    {
        public Vector3 LeadStart;
        public Vector3 Launch;
        public Vector3 Control;
        public Vector3 End;
        public Vector3 TailEnd;
        public Vector3 ExitDirection;
        public Vector3 ArriveDirection;
        public Vector3 BoardNormal;
        public float LeadLength;
        public float ArcLength;
        public float TailLength;
        public bool HasTargetBounds;
        public Bounds TargetBounds;
        public float[] distances;

        public float TotalLength => LeadLength + ArcLength + TailLength;

        public bool Sample(float distance, out Vector3 position, out Vector3 tangent)
        {
            position = Launch;
            tangent = ExitDirection.sqrMagnitude > 0.0001f ? ExitDirection : Vector3.forward;
            if (distances == null)
                return false;

            if (distance < LeadLength)
            {
                float t = LeadLength > 0.0001f ? distance / LeadLength : 1f;
                position = Vector3.Lerp(LeadStart, Launch, t);
                return true;
            }

            distance -= LeadLength;
            if (distance < ArcLength)
            {
                float t = ArcParameter(distances, distance);
                position = Quadratic(Launch, Control, End, t);
                tangent = QuadraticTangent(Launch, Control, End, t);
                return tangent.sqrMagnitude > 0.0001f;
            }

            distance -= ArcLength;
            float tail = TailLength > 0.0001f ? Mathf.Clamp01(distance / TailLength) : 1f;
            position = Vector3.Lerp(End, TailEnd, tail);
            tangent = ArriveDirection.sqrMagnitude > 0.0001f ? ArriveDirection : tangent;
            return true;
        }
    }

    public static bool TryBuildShot(ITarget source, ITarget target, float arcHeight, bool keepArcHeight, out DirectArc arc)
    {
        arc = default;
        if (source == null || target == null)
            return false;
        if (!TryGetShotLaunch(source, out Vector3 launch, out Transform facing))
            return false;
        if (!TryGetTargetOccluder(target, out Vector3 end, out Bounds bounds, out bool hasBounds))
            return false;

        Camera camera = GetCamera();
        Vector3 boardNormal = camera != null
            ? BoardNormalTowardCamera(facing, camera)
            : facing.forward;
        if (boardNormal.sqrMagnitude < 0.0001f)
            boardNormal = Vector3.forward;
        else
            boardNormal.Normalize();

        Vector3 toTarget = end - launch;
        if (toTarget.sqrMagnitude < MinPathLength * MinPathLength)
            return false;

        float bow = keepArcHeight ? arcHeight : Mathf.Min(arcHeight, toTarget.magnitude * 0.35f);
        Vector3 control = (launch + end) * 0.5f + boardNormal * bow;
        Vector3 exitDir = control - launch;
        if (exitDir.sqrMagnitude < 0.0001f)
            return false;
        exitDir.Normalize();

        Vector3 arriveDir = end - control;
        if (arriveDir.sqrMagnitude < 0.0001f)
            arriveDir = toTarget;
        arriveDir.Normalize();

        float[] distances = new float[ArcSamples + 1];
        float arcLength = FillCurve(distances, launch, control, default, end, false);
        if (arcLength <= 0.05f)
            return false;

        arc.LeadStart = launch;
        arc.Launch = launch;
        arc.Control = control;
        arc.End = end;
        arc.TailEnd = end;
        arc.ExitDirection = exitDir;
        arc.ArriveDirection = arriveDir;
        arc.BoardNormal = boardNormal;
        arc.ArcLength = arcLength;
        arc.HasTargetBounds = hasBounds;
        arc.TargetBounds = bounds;
        arc.distances = distances;
        return true;
    }

    static bool TryGetShotLaunch(ITarget source, out Vector3 launch, out Transform facing)
    {
        launch = default;
        facing = null;
        if (source == null || View.Instance == null)
            return false;

        ViewTarget view = View.Instance.GetViewTargetByID(source.GetID());
        if (view == null)
            return false;

        facing = view.transform;
        if (view is ViewCard card && TryGetCardLaunch(card, out launch, out _))
            return true;

        if (view is ViewPlayer player && player.ViewPlayerPortrait != null)
        {
            facing = player.ViewPlayerPortrait.transform;
            if (player.ViewPlayerPortrait.PortraitRenderer != null)
            {
                launch = player.ViewPlayerPortrait.PortraitRenderer.bounds.center;
                return true;
            }

            if (TryGetRendererBounds(facing, out Bounds bounds))
            {
                launch = bounds.center;
                return true;
            }

            launch = facing.position;
            return true;
        }

        launch = view.transform.position;
        return true;
    }

    public static Quaternion AlignArrow(Vector3 tipLocal, Vector3 tangent, Vector3 faceLocal, Vector3 face)
    {
        return AlignRotation(tipLocal, tangent, faceLocal, face);
    }
}
