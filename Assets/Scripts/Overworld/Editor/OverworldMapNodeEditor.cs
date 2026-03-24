using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OverworldMapNode))]
public class OverworldMapNodeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var node = (OverworldMapNode)target;
        EditorGUILayout.Space(4);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Snap position from Q/R"))
            node.SnapPositionFromAxial();
        if (GUILayout.Button("Capture Q/R from position"))
            node.CaptureAxialFromPosition();
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Snap (capture then apply)"))
            node.SnapPositionFromAxialAndCapture();
    }

}
