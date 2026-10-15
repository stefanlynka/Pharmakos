using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the 3D River Styx area from the CardRewardArea: clones its altar, carpets, floor, and lights into StyxArea,
/// points the StyxCamera the same way, lays out the card grid / ritual / trinket containers and flood anchor for that
/// view, and builds the Styx instruction text and button on the StyxScreen. Safe to run again; it replaces what it
/// created before. Positions and scales are starting points to tweak in the scene.
/// </summary>
public static class StyxAreaSetupEditor
{
    const string UndoName = "Setup Styx Area";
    const string ButtonPrefabPath = "Assets/Resources/Prefabs/UI/Buttons/ViewButton.prefab";
    const string RitualPrefabPath = "Prefabs/View/ViewRitual3";
    const string TrinketPrefabPath = "Prefabs/View/ViewBuff";

    static readonly string[] DecorNames = { "Altar", "Carpets", "BackgroundFloor", "Lights" };
    const string RitualContainerName = "RitualContainer";
    const string TrinketContainerName = "TrinketContainer";
    const string FloodAnchorName = "FloodAnchor";
    const string TextName = "StyxText";
    const string ButtonName = "StyxContinueButton";

    // Base CardView spacing at scale 1
    const float BaseCardWidth = 7.64f;
    const float BaseCardHeight = 9.17f;
    const float BaseCardMargin = 1.32f;
    const float ViewAspect = 16f / 9f;

    [MenuItem("Pharmakos/Setup Styx Area")]
    public static void SetupStyxArea()
    {
        Transform areas = FindSceneTransform("3DAreas");
        Transform styxArea = areas != null ? areas.Find("StyxArea") : null;
        Transform rewardArea = areas != null ? areas.Find("CardRewardArea") : null;
        if (styxArea == null || rewardArea == null)
        {
            EditorUtility.DisplayDialog(UndoName, "Couldn't find 3DAreas/StyxArea and 3DAreas/CardRewardArea in the open scene.", "OK");
            return;
        }

        StyxScreenHandler screenHandler = FindSceneComponent<StyxScreenHandler>();
        if (screenHandler == null)
        {
            EditorUtility.DisplayDialog(UndoName, "Couldn't find the StyxScreen (StyxScreenHandler) in the open scene.", "OK");
            return;
        }

        Undo.SetCurrentGroupName(UndoName);
        int undoGroup = Undo.GetCurrentGroup();

        RemoveChildren(styxArea, DecorNames);
        RemoveChildren(styxArea, RitualContainerName, TrinketContainerName, FloodAnchorName);
        foreach (string name in DecorNames)
        {
            Transform source = rewardArea.Find(name);
            if (source != null) CloneInto(source, styxArea);
            else Debug.LogWarning(UndoName + ": CardRewardArea has no child named " + name);
        }

        Camera rewardCamera = FindRewardCamera(rewardArea);
        Camera styxCamera = FindStyxCamera(styxArea, screenHandler);
        if (rewardCamera == null || styxCamera == null)
        {
            EditorUtility.DisplayDialog(UndoName, "Couldn't find the CardRewardCamera or the StyxCamera.", "OK");
            return;
        }
        CopyCamera(rewardCamera, rewardArea, styxCamera, styxArea);

        ViewFrame frame = BuildViewFrame(rewardArea, rewardCamera, styxArea, styxCamera);

        StyxHandler handler = styxArea.GetComponent<StyxHandler>();
        if (handler == null) handler = Undo.AddComponent<StyxHandler>(styxArea.gameObject);
        Undo.RecordObject(handler, UndoName);

        handler.StyxCamera = styxCamera;
        handler.StyxScreenHandler = screenHandler;
        handler.CardScroller = SetupCardGrid(styxArea, frame, out float cardScale);
        handler.RitualContainer = SetupRitualContainer(styxArea, frame, cardScale, handler);
        handler.TrinketContainer = SetupTrinketContainer(styxArea, frame, cardScale, handler);
        handler.FloodAnchor = SetupFloodAnchor(styxArea, frame);
        EditorUtility.SetDirty(handler);

        SetupScreen(screenHandler, handler, styxCamera);

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(styxArea.gameObject.scene);
        Selection.activeGameObject = styxArea.gameObject;

        EditorUtility.DisplayDialog(UndoName,
            "StyxArea rebuilt from CardRewardArea. Enable StyxArea and the StyxCamera to preview it, tweak the container positions and StyxHandler layout values, then save the scene.",
            "OK");
    }

    // The table plane cards rest on (from a CardReward slot) and where the camera's centre ray meets it, in StyxArea space
    struct ViewFrame
    {
        public Vector3 Center;
        public Quaternion TableRotation;
        public float Width;
        public float Height;
        public Vector3 CameraPosition;
        public Quaternion CameraRotation;
        public float CenterDistance;

        public Vector3 Right => TableRotation * Vector3.right;
        public Vector3 Up => TableRotation * Vector3.up;
        public Vector3 Forward => CameraRotation * Vector3.forward;

        public Vector3 OnTable(float xFraction, float yFraction)
        {
            return Center + Right * (xFraction * Width) + Up * (yFraction * Height);
        }
    }

    static ViewFrame BuildViewFrame(Transform rewardArea, Camera rewardCamera, Transform styxArea, Camera styxCamera)
    {
        Vector3 cameraPosition = styxArea.InverseTransformPoint(styxCamera.transform.position);
        Quaternion cameraRotation = Quaternion.Inverse(styxArea.rotation) * styxCamera.transform.rotation;
        Vector3 forward = cameraRotation * Vector3.forward;

        Vector3 planePoint = cameraPosition + forward * 50f;
        Quaternion tableRotation = cameraRotation;

        CardReward slot = rewardArea.GetComponentInChildren<CardReward>(true);
        if (slot != null)
        {
            planePoint = rewardArea.InverseTransformPoint(slot.transform.position);
            tableRotation = Quaternion.Inverse(rewardArea.rotation) * slot.transform.rotation;
        }
        else
        {
            Debug.LogWarning(UndoName + ": no CardReward slot in CardRewardArea; laying the table out facing the camera.");
        }

        Vector3 normal = tableRotation * Vector3.forward;
        float facing = Vector3.Dot(forward, normal);
        float distance = Mathf.Abs(facing) > 0.01f ? Vector3.Dot(planePoint - cameraPosition, normal) / facing : 50f;
        if (distance <= 0f) distance = 50f;

        float height = styxCamera.orthographic
            ? 2f * styxCamera.orthographicSize
            : 2f * distance * Mathf.Tan(styxCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);

        return new ViewFrame
        {
            Center = cameraPosition + forward * distance,
            TableRotation = tableRotation,
            Width = height * ViewAspect,
            Height = height,
            CameraPosition = cameraPosition,
            CameraRotation = cameraRotation,
            CenterDistance = distance,
        };
    }

    static ViewCardScroller SetupCardGrid(Transform styxArea, ViewFrame frame, out float cardScale)
    {
        ViewCardScroller scroller = styxArea.GetComponentInChildren<ViewCardScroller>(true);
        if (scroller == null)
        {
            GameObject gridObject = new GameObject("StyxCardGrid");
            Undo.RegisterCreatedObjectUndo(gridObject, UndoName);
            gridObject.transform.SetParent(styxArea, false);
            scroller = gridObject.AddComponent<ViewCardScroller>();
        }

        const int Columns = 6;
        const int VisibleRows = 3;
        float gridWidth = frame.Width * 0.48f;
        float gridHeight = frame.Height * 0.6f;
        cardScale = Mathf.Min(
            gridWidth / (Columns * BaseCardWidth + (Columns - 1) * BaseCardMargin),
            gridHeight / (VisibleRows * BaseCardHeight + (VisibleRows - 1) * BaseCardMargin));

        float cardWidth = BaseCardWidth * cardScale;
        float cardHeight = BaseCardHeight * cardScale;
        float margin = BaseCardMargin * cardScale;
        float laidOutWidth = Columns * cardWidth + (Columns - 1) * margin;

        Undo.RecordObject(scroller, UndoName);
        scroller.CardScale = cardScale;
        scroller.CardWidth = cardWidth;
        scroller.CardHeight = cardHeight;
        scroller.CardMargin = margin;
        // The scroller adds columns until they reach Width, so stop just short of the last one
        scroller.Width = laidOutWidth - cardWidth * 0.5f;
        scroller.Height = gridHeight;
        scroller.scrollIncrement = cardHeight * 0.5f;
        scroller.scrollSpeed = cardHeight * 4f;
        EditorUtility.SetDirty(scroller);

        // Its origin is the centre of the top-left card
        Transform grid = scroller.transform;
        Undo.RecordObject(grid, UndoName);
        grid.localPosition = frame.OnTable(0f, 0.32f)
            + frame.Right * (-laidOutWidth * 0.5f + cardWidth * 0.5f)
            - frame.Up * (cardHeight * 0.5f);
        grid.localRotation = frame.TableRotation;
        grid.localScale = Vector3.one;

        return scroller;
    }

    static Transform SetupRitualContainer(Transform styxArea, ViewFrame frame, float cardScale, StyxHandler handler)
    {
        Transform container = CreateContainer(RitualContainerName, styxArea, frame.OnTable(-0.37f, -0.02f), frame.TableRotation);

        Vector2 size = MeasurePrefab(RitualPrefabPath, null);
        if (size.x > 0f && size.y > 0f)
        {
            float scale = BaseCardHeight * cardScale * 1.15f / size.y;
            const int Columns = 2;
            float maxWidth = frame.Width * 0.22f;
            if (Columns * size.x * scale * 1.1f > maxWidth)
                scale = maxWidth / (Columns * size.x * 1.1f);

            handler.RitualScale = scale;
            handler.RitualColumns = Columns;
            handler.RitualSpacing = size * scale * 1.1f;
        }

        return container;
    }

    static Transform SetupTrinketContainer(Transform styxArea, ViewFrame frame, float cardScale, StyxHandler handler)
    {
        Transform container = CreateContainer(TrinketContainerName, styxArea, frame.OnTable(0.37f, -0.02f), frame.TableRotation);

        Vector2 size = MeasurePrefab(TrinketPrefabPath, root =>
        {
            ViewBuff buff = root.GetComponent<ViewBuff>();
            if (buff == null) return;
            if (buff.ParentObject != null) buff.ParentObject.SetActive(true);
            if (buff.SummaryObject != null) buff.SummaryObject.SetActive(false);
            if (buff.MultiplierObject != null) buff.MultiplierObject.SetActive(false);
        });
        if (size.x > 0f && size.y > 0f)
        {
            float scale = BaseCardWidth * cardScale * 0.55f / Mathf.Max(size.x, size.y);
            handler.TrinketScale = scale;
            handler.TrinketColumns = 2;
            handler.TrinketSpacing = size * scale * 1.35f;
        }

        return container;
    }

    static ViewTarget SetupFloodAnchor(Transform styxArea, ViewFrame frame)
    {
        // Well in front of the table so the water draws over everything on it
        Vector3 position = frame.CameraPosition + frame.Forward * (frame.CenterDistance * 0.35f);
        Transform anchor = CreateContainer(FloodAnchorName, styxArea, position, frame.CameraRotation);
        return Undo.AddComponent<ViewTarget>(anchor.gameObject);
    }

    static void SetupScreen(StyxScreenHandler screenHandler, StyxHandler handler, Camera styxCamera)
    {
        Transform screenRoot = screenHandler.transform;

        Transform oldUI = screenRoot.Find("StyxUI");
        if (oldUI != null && oldUI.gameObject.activeSelf)
        {
            Undo.RecordObject(oldUI.gameObject, UndoName);
            oldUI.gameObject.SetActive(false);
        }

        RemoveChildren(screenRoot, TextName, ButtonName);

        Undo.RecordObject(screenHandler, UndoName);
        screenHandler.StyxHandler = handler;

        Transform shopText = FindShopText();
        if (shopText != null)
        {
            GameObject textObject = Object.Instantiate(shopText.gameObject, screenRoot, false);
            textObject.name = TextName;
            Undo.RegisterCreatedObjectUndo(textObject, UndoName);
            textObject.SetActive(true);

            CanvasGroup group = textObject.GetComponent<CanvasGroup>();
            if (group == null) group = textObject.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            TextMeshProUGUI text = FindLabel(textObject.transform, "Text");
            if (text != null) text.text = screenHandler.OfferingText;

            screenHandler.InstructionText = text;
            screenHandler.InstructionGroup = group;
        }
        else
        {
            Debug.LogWarning(UndoName + ": couldn't find ShopScreen/ShopText to clone; assign StyxScreenHandler.InstructionText yourself.");
        }

        GameObject buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPrefabPath);
        if (buttonPrefab != null)
        {
            GameObject buttonObject = (GameObject)PrefabUtility.InstantiatePrefab(buttonPrefab, screenRoot);
            buttonObject.name = ButtonName;
            Undo.RegisterCreatedObjectUndo(buttonObject, UndoName);

            RectTransform rect = (RectTransform)buttonObject.transform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 40f);
            rect.sizeDelta = new Vector2(280f, 64f);

            TextMeshProUGUI label = FindLabel(buttonObject.transform, "Label");
            if (label != null) label.text = screenHandler.CrossButtonText;

            screenHandler.ContinueButton = buttonObject.GetComponent<Button>();
            screenHandler.ContinueButtonLabel = label;
        }
        else
        {
            Debug.LogWarning(UndoName + ": missing " + ButtonPrefabPath + "; assign StyxScreenHandler.ContinueButton yourself.");
        }

        EditorUtility.SetDirty(screenHandler);

        Screen screen = screenHandler.GetComponent<Screen>();
        if (screen != null && screen.Camera != styxCamera)
        {
            Undo.RecordObject(screen, UndoName);
            screen.Camera = styxCamera;
            EditorUtility.SetDirty(screen);
        }
    }

    static Camera FindRewardCamera(Transform rewardArea)
    {
        Transform named = rewardArea.Find("CardRewardCamera");
        if (named != null && named.TryGetComponent(out Camera camera)) return camera;
        return rewardArea.GetComponentInChildren<Camera>(true);
    }

    static Camera FindStyxCamera(Transform styxArea, StyxScreenHandler screenHandler)
    {
        Transform named = styxArea.Find("StyxCamera");
        if (named != null && named.TryGetComponent(out Camera camera)) return camera;

        Screen screen = screenHandler.GetComponent<Screen>();
        if (screen != null && screen.Camera != null) return screen.Camera;

        return styxArea.GetComponentInChildren<Camera>(true);
    }

    // Matches the reward camera's pose (relative to its area) and lens; the Styx camera keeps its own depth and enabled state
    static void CopyCamera(Camera source, Transform sourceArea, Camera target, Transform targetArea)
    {
        Undo.RecordObject(target.transform, UndoName);
        target.transform.position = targetArea.TransformPoint(sourceArea.InverseTransformPoint(source.transform.position));
        target.transform.rotation = targetArea.rotation * (Quaternion.Inverse(sourceArea.rotation) * source.transform.rotation);

        Undo.RecordObject(target, UndoName);
        target.clearFlags = source.clearFlags;
        target.backgroundColor = source.backgroundColor;
        target.orthographic = source.orthographic;
        target.orthographicSize = source.orthographicSize;
        target.fieldOfView = source.fieldOfView;
        target.nearClipPlane = source.nearClipPlane;
        target.farClipPlane = source.farClipPlane;
        target.cullingMask = source.cullingMask;
        EditorUtility.SetDirty(target);
    }

    static Transform CreateContainer(string name, Transform parent, Vector3 localPosition, Quaternion localRotation)
    {
        GameObject container = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(container, UndoName);
        container.transform.SetParent(parent, false);
        container.transform.localPosition = localPosition;
        container.transform.localRotation = localRotation;
        container.transform.localScale = Vector3.one;
        return container.transform;
    }

    static void CloneInto(Transform source, Transform targetArea)
    {
        GameObject clone = Object.Instantiate(source.gameObject);
        clone.name = source.name;
        Undo.RegisterCreatedObjectUndo(clone, UndoName);
        clone.transform.SetParent(targetArea, false);
        clone.transform.localPosition = source.localPosition;
        clone.transform.localRotation = source.localRotation;
        clone.transform.localScale = source.localScale;
        clone.SetActive(source.gameObject.activeSelf);
    }

    static void RemoveChildren(Transform parent, params string[] names)
    {
        List<GameObject> toRemove = new List<GameObject>();
        foreach (Transform child in parent)
        {
            if (System.Array.IndexOf(names, child.name) >= 0)
                toRemove.Add(child.gameObject);
        }
        foreach (GameObject child in toRemove)
            Undo.DestroyObjectImmediate(child);
    }

    // Size of the prefab's renderers on its local X/Y plane at scale 1, measured on a temporary copy
    static Vector2 MeasurePrefab(string resourcesPath, System.Action<GameObject> prepare)
    {
        GameObject prefab = Resources.Load<GameObject>(resourcesPath);
        if (prefab == null)
        {
            Debug.LogWarning(UndoName + ": missing Resources/" + resourcesPath + "; keeping the StyxHandler layout defaults.");
            return Vector2.zero;
        }

        GameObject temp = Object.Instantiate(prefab);
        temp.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            temp.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            temp.transform.localScale = Vector3.one;
            prepare?.Invoke(temp);

            bool hasBounds = false;
            Bounds bounds = default;
            foreach (Renderer renderer in temp.GetComponentsInChildren<Renderer>(false))
            {
                if (renderer.GetComponent<TMP_Text>() != null || renderer.GetComponent<TMP_SubMesh>() != null) continue;
                Bounds rendererBounds = renderer.bounds;
                if (rendererBounds.size.sqrMagnitude <= 0f) continue;

                if (!hasBounds) { bounds = rendererBounds; hasBounds = true; }
                else bounds.Encapsulate(rendererBounds);
            }
            return hasBounds ? new Vector2(bounds.size.x, bounds.size.y) : Vector2.zero;
        }
        finally
        {
            Object.DestroyImmediate(temp);
        }
    }

    static Transform FindShopText()
    {
        ShopScreenHandler shopScreen = FindSceneComponent<ShopScreenHandler>();
        if (shopScreen == null) return null;

        foreach (Transform child in shopScreen.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "ShopText") return child;
        }
        return null;
    }

    static TextMeshProUGUI FindLabel(Transform root, string preferredName)
    {
        TextMeshProUGUI[] labels = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI label in labels)
        {
            if (label.name == preferredName) return label;
        }
        return labels.Length > 0 ? labels[0] : null;
    }

    static Transform FindSceneTransform(string name)
    {
        foreach (Transform transform in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (transform.name == name && transform.parent == null && IsSceneObject(transform.gameObject))
                return transform;
        }
        foreach (Transform transform in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (transform.name == name && IsSceneObject(transform.gameObject))
                return transform;
        }
        return null;
    }

    static T FindSceneComponent<T>() where T : Component
    {
        foreach (T component in Resources.FindObjectsOfTypeAll<T>())
        {
            if (IsSceneObject(component.gameObject)) return component;
        }
        return null;
    }

    static bool IsSceneObject(GameObject gameObject)
    {
        return !EditorUtility.IsPersistent(gameObject)
            && gameObject.scene.IsValid()
            && (gameObject.hideFlags & HideFlags.HideInHierarchy) == 0;
    }
}
