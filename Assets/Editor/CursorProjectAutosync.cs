#if UNITY_EDITOR
using UnityEditor;
using Unity.CodeEditor;

public class CursorProjectAutosync : AssetPostprocessor
{
    // Automatically regenerates solution files when C# scripts are added, deleted, or moved
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        bool needsSync = false;

        foreach (string asset in importedAssets)
        {
            if (asset.EndsWith(".cs") || asset.EndsWith(".asmdef")) { needsSync = true; break; }
        }
        foreach (string asset in deletedAssets)
        {
            if (asset.EndsWith(".cs") || asset.EndsWith(".asmdef")) { needsSync = true; break; }
        }

        if (needsSync)
        {
            CodeEditor.Editor.CurrentCodeEditor.SyncAll();
        }
    }

    // Adds a manual trigger to the top menu bar (Alt + Shift + S shortcut)
    [MenuItem("Tools/Sync Project Files %#s")]
    public static void ManualSync()
    {
        CodeEditor.Editor.CurrentCodeEditor.SyncAll();
        UnityEngine.Debug.Log("Unity project files successfully regenerated for Cursor.");
    }
}
#endif