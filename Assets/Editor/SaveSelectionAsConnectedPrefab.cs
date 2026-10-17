using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SaveSelectionAsConnectedPrefab
{
    const string MenuPath = "Pharmakos/Save Selection As Connected Prefab";

    [MenuItem(MenuPath)]
    static void Save()
    {
        GameObject selected = Selection.activeGameObject;
        if (Selection.gameObjects.Length != 1 || selected == null)
        {
            EditorUtility.DisplayDialog("Save As Connected Prefab", "Select one scene object.", "OK");
            return;
        }

        if (PrefabStageUtility.GetCurrentPrefabStage() != null || EditorUtility.IsPersistent(selected))
        {
            EditorUtility.DisplayDialog(
                "Save As Connected Prefab",
                "Open the scene and select an object in the Hierarchy.",
                "OK");
            return;
        }

        if (PrefabUtility.IsPartOfPrefabInstance(selected))
        {
            EditorUtility.DisplayDialog(
                "Save As Connected Prefab",
                "This object is already inside a prefab instance. Select the scene object above it.",
                "OK");
            return;
        }

        string path = EditorUtility.SaveFilePanelInProject(
            "Save Connected Prefab",
            selected.name,
            "prefab",
            "Save outside a Resources folder. The scene object stays in place and becomes an instance of this prefab.",
            "Assets");

        if (string.IsNullOrEmpty(path))
            return;

        GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(selected, path, InteractionMode.UserAction);
        if (prefab == null)
        {
            EditorUtility.DisplayDialog("Save As Connected Prefab", "Unity could not save the prefab.", "OK");
            return;
        }

        EditorSceneManager.MarkSceneDirty(selected.scene);
        Debug.Log($"Saved {selected.name} as a connected prefab at {path}. Save the scene.", prefab);
    }
}
