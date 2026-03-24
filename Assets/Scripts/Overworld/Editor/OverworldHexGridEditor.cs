using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OverworldHexGrid))]
public class OverworldHexGridEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var grid = (OverworldHexGrid)target;
        EditorGUILayout.Space(4);

        if (GUILayout.Button("Snap All Nodes (capture then apply)"))
        {
            var nodes = grid.GetAllNodes();
            foreach (var node in nodes)
            {
                if (node != null)
                {
                    Undo.RecordObject(node, "Snap All Overworld Nodes");
                    Undo.RecordObject(node.transform, "Snap All Overworld Nodes");
                    Undo.RecordObject(node.gameObject, "Snap All Overworld Nodes");
                    node.SnapPositionFromAxialAndCapture();
                    node.gameObject.name = $"Node_{node.Q}_{node.R}";
                }
            }
        }
    }
}
