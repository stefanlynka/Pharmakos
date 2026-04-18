using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TorchFlickerLight))]
public class TorchFlickerLightEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        var flicker = (TorchFlickerLight)target;
        if (GUILayout.Button("Recalculate baseline"))
        {
            Undo.RecordObject(flicker, "Recalculate torch baseline");
            flicker.RecalculateBaseline();
            EditorUtility.SetDirty(flicker);
        }
    }
}
