using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MeshHighlightEffect))]
public class MeshHighlightEffectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var effect = (MeshHighlightEffect)target;
        SerializedProperty settingsProperty = serializedObject.FindProperty("settings");
        var settings = settingsProperty.objectReferenceValue as MeshHighlightSettings;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Settings Preset", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = settings != null;
            if (GUILayout.Button("Pull from Settings"))
            {
                PullFromSettings(effect, settings);
            }

            if (GUILayout.Button("Push to Settings"))
            {
                PushToSettings(effect, settings);
            }

            GUI.enabled = true;
        }

        if (GUILayout.Button("Save as New Settings..."))
        {
            SaveAsNewSettings(effect, settingsProperty);
        }

        if (settings == null)
        {
            EditorGUILayout.HelpBox(
                "Assign a Mesh Highlight Settings asset to use Pull/Push. Save as New Settings always works.",
                MessageType.Info);
        }
    }

    static void PullFromSettings(MeshHighlightEffect effect, MeshHighlightSettings settings)
    {
        Undo.RecordObject(effect, "Pull Mesh Highlight Settings");
        effect.ApplySettings(settings);
        EditorUtility.SetDirty(effect);
    }

    static void PushToSettings(MeshHighlightEffect effect, MeshHighlightSettings settings)
    {
        Undo.RecordObject(settings, "Push Mesh Highlight Settings");
        effect.CopyFieldsToSettings(settings);
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
    }

    void SaveAsNewSettings(MeshHighlightEffect effect, SerializedProperty settingsProperty)
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Mesh Highlight Settings",
            "MeshHighlightSettings",
            "asset",
            "Choose where to save the settings asset.");

        if (string.IsNullOrEmpty(path))
            return;

        var asset = CreateInstance<MeshHighlightSettings>();
        effect.CopyFieldsToSettings(asset);
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();

        Undo.RecordObject(effect, "Assign Mesh Highlight Settings");
        settingsProperty.objectReferenceValue = asset;
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(effect);

        EditorGUIUtility.PingObject(asset);
        Selection.activeObject = asset;
    }
}
