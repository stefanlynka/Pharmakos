using System.Collections.Generic;
using UnityEditor;

[CustomEditor(typeof(MeshHighlightEffect))]
public class MeshHighlightEffectEditor : Editor
{
    // Everything except these mirrors the preset at runtime, so it's hidden while a preset is assigned.
    static readonly HashSet<string> ComponentOwnedFields = new HashSet<string>
    {
        "targetRoot", "glowMaterialTemplate", "settings", "explicitRenderers",
    };

    Editor presetEditor;

    public override void OnInspectorGUI()
    {
        var effect = (MeshHighlightEffect)target;
        HighlightPresetInspector.Draw(
            serializedObject,
            name => !ComponentOwnedFields.Contains(name),
            ref presetEditor,
            null,
            effect.CopyFieldsToSettings);
    }

    void OnDisable()
    {
        if (presetEditor != null)
            DestroyImmediate(presetEditor);
    }
}
