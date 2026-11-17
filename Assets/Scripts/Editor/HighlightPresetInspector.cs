using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Shared inspector for CardHighlightEffect / MeshHighlightEffect. When a preset is assigned it is what plays,
/// so the preset is edited inline and the component's own glow fields (a runtime mirror of it) are hidden.
/// </summary>
static class HighlightPresetInspector
{
    public static void Draw(
        SerializedObject serializedObject,
        Func<string, bool> isPresetDrivenField,
        ref Editor cachedPresetEditor,
        MeshHighlightSettings livePreset,
        Action<MeshHighlightSettings> copyFieldsToPreset)
    {
        serializedObject.Update();

        SerializedProperty settingsProperty = serializedObject.FindProperty("settings");
        var preset = settingsProperty.objectReferenceValue as MeshHighlightSettings;

        SerializedProperty iterator = serializedObject.GetIterator();
        bool enterChildren = true;
        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (iterator.name == "m_Script")
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.PropertyField(iterator);
                continue;
            }

            if (preset != null && isPresetDrivenField(iterator.name))
                continue;

            EditorGUILayout.PropertyField(iterator, true);
        }

        serializedObject.ApplyModifiedProperties();

        if (Application.isPlaying && livePreset != null && livePreset != preset)
        {
            EditorGUILayout.HelpBox(
                $"Playing \"{livePreset.name}\" right now (chosen by highlight state via HighlightPalette). " +
                "The preset above is the default.",
                MessageType.Info);
        }

        if (preset == null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "No preset: the fields above are used as-is. Save them as a preset to share the look.",
                MessageType.Info);

            if (GUILayout.Button("Save as New Preset..."))
                SaveAsNewPreset(serializedObject, settingsProperty, copyFieldsToPreset);
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Preset: {preset.name}", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Edits here change the preset asset, and so every glow that uses it.",
            MessageType.None);

        Editor.CreateCachedEditor(preset, null, ref cachedPresetEditor);
        using (new EditorGUI.IndentLevelScope())
            cachedPresetEditor.OnInspectorGUI();
    }

    static void SaveAsNewPreset(
        SerializedObject serializedObject,
        SerializedProperty settingsProperty,
        Action<MeshHighlightSettings> copyFieldsToPreset)
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Highlight Preset",
            "HighlightPreset",
            "asset",
            "Choose where to save the preset.",
            "Assets/Resources/Content/HighlightSettings");

        if (string.IsNullOrEmpty(path))
            return;

        var asset = ScriptableObject.CreateInstance<MeshHighlightSettings>();
        copyFieldsToPreset(asset);
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();

        settingsProperty.objectReferenceValue = asset;
        serializedObject.ApplyModifiedProperties();
        EditorGUIUtility.PingObject(asset);
    }
}
