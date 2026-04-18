using UnityEditor;
using UnityEngine;

namespace DynamicSmokeSystem {
    [CustomEditor(typeof(DynamicSmokeSystemController))]
    public class DynamicSmokeSystemControllerEditor : Editor {
        public override void OnInspectorGUI() {
            DrawDefaultInspector();

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(!Application.isPlaying)) {
                if (GUILayout.Button("Rebuild All Clouds")) {
                    DynamicSmokeSystemController.RebuildAll();
                }
            }
        }
    }
}
