using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CardHighlightEffect))]
public class CardHighlightEffectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var effect = (CardHighlightEffect)target;
        SerializedProperty settingsProperty = serializedObject.FindProperty("settings");
        var settings = settingsProperty.objectReferenceValue as MeshHighlightSettings;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Settings Preset", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = settings != null;
            if (GUILayout.Button("Pull from Settings"))
            {
                Undo.RecordObject(effect, "Pull Card Highlight Settings");
                effect.ApplySettings(settings);
                EditorUtility.SetDirty(effect);
            }

            if (GUILayout.Button("Push to Settings"))
            {
                Undo.RecordObject(settings, "Push Card Highlight Settings");
                effect.CopyFieldsToSettings(settings);
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }

            GUI.enabled = true;
        }

        if (settings == null)
        {
            EditorGUILayout.HelpBox(
                "Assign a Mesh Highlight Settings asset to use Pull/Push.",
                MessageType.Info);
        }
    }
}
