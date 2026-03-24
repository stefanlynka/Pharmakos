using UnityEditor;
using UnityEngine;

public static class OverworldSetupEditor
{
    [MenuItem("Pharmakos/Setup Overworld Map")]
    public static void SetupOverworld()
    {
        var controllerGo = new GameObject("OverworldMapController");
        Undo.RegisterCreatedObjectUndo(controllerGo, "Create Overworld");
        controllerGo.transform.position = new Vector3(0, 100, 0);

        var gridGo = new GameObject("OverworldHexGrid");
        Undo.RegisterCreatedObjectUndo(gridGo, "Create Overworld");
        gridGo.transform.SetParent(controllerGo.transform, false);
        gridGo.transform.localPosition = Vector3.zero;

        var grid = gridGo.AddComponent<OverworldHexGrid>();
        grid.HexSize = 1f;

        var mapController = controllerGo.AddComponent<OverworldMapController>();
        mapController.Grid = grid;

        var markerGo = new GameObject("PlayerMarker");
        Undo.RegisterCreatedObjectUndo(markerGo, "Create Overworld");
        markerGo.transform.SetParent(controllerGo.transform, false);
        markerGo.transform.localPosition = new Vector3(0, 2, 0);
        var markerPrimitive = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        markerPrimitive.name = "MarkerVisual";
        markerPrimitive.transform.SetParent(markerGo.transform, false);
        markerPrimitive.transform.localScale = new Vector3(0.3f, 0.5f, 0.3f);
        markerPrimitive.transform.localPosition = Vector3.zero;
        Object.DestroyImmediate(markerPrimitive.GetComponent<Collider>());
        mapController.PlayerMarker = markerGo.transform;

        CreateNode(gridGo, 0, 0, true);
        CreateNode(gridGo, 1, 0, false);
        CreateNode(gridGo, 0, 1, false);
        CreateNode(gridGo, 1, -1, false);

        var screenGo = new GameObject("OverworldScreen");
        Undo.RegisterCreatedObjectUndo(screenGo, "Create Overworld");
        var canvas = screenGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var cg = screenGo.AddComponent<CanvasGroup>();
        cg.alpha = 1;
        cg.interactable = true;
        cg.blocksRaycasts = true;
        var overworldScreen = screenGo.AddComponent<OverworldScreen>();
        overworldScreen.Name = ScreenName.Overworld;
        overworldScreen.UIHolder = cg;
        overworldScreen.MapController = mapController;

        controllerGo.transform.SetParent(screenGo.transform);

        var screenHandler = Object.FindObjectOfType<ScreenHandler>();
        if (screenHandler != null)
        {
            Undo.RecordObject(screenHandler, "Add Overworld Screen");
            var so = new SerializedObject(screenHandler);
            var list = so.FindProperty("Screens");
            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = overworldScreen;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        var controller = Object.FindObjectOfType<Controller>();
        if (controller != null)
        {
            Undo.RecordObject(controller, "Assign OverworldMapController");
            var so = new SerializedObject(controller);
            so.FindProperty("OverworldMapController").objectReferenceValue = mapController;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        Selection.activeGameObject = controllerGo;
        EditorUtility.DisplayDialog("Overworld Setup", "Overworld map created. The grid is at Y=100. Add more nodes as children of OverworldHexGrid and use the Inspector buttons to snap them to the hex grid.", "OK");
    }

    private static void CreateNode(GameObject parent, int q, int r, bool isStart)
    {
        var go = new GameObject($"Node_{q}_{r}");
        Undo.RegisterCreatedObjectUndo(go, "Create Node");
        go.transform.SetParent(parent.transform, false);
        var node = go.AddComponent<OverworldMapNode>();
        node.Q = q;
        node.R = r;
        node.IsStart = isStart;
        node.SnapPositionFromAxial();
    }
}
