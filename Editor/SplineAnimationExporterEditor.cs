using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;
using YanickSenn.Utils.Variables;

namespace YanickSenn.Navigation.Editor {

    [CustomEditor(typeof(SplineAnimationExporter))]
    public class SplineAnimationExporterEditor : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            var exporter = (SplineAnimationExporter)target;

            var splineContainer = exporter.GetComponent<SplineContainer>();
            if (splineContainer == null) {
                EditorGUILayout.HelpBox("Missing SplineContainer on this GameObject.", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space();

            if (exporter.target == null) {
                EditorGUILayout.HelpBox("Assign a Target Definition or create a new one to enable exporting.",
                    MessageType.Info);
                if (!GUILayout.Button("Create & Export Spline Definition", GUILayout.Height(30))) {
                    return;
                }

                string defaultName = "animation_" + exporter.gameObject.name.ToLower().Replace(" ", "_");
                string path = EditorUtility.SaveFilePanelInProject(
                    "Create Spline Animation Definition",
                    defaultName,
                    "asset",
                    "Please enter a file name to save the spline definition to");

                if (string.IsNullOrEmpty(path)) {
                    return;
                }

                var newDefinition = CreateInstance<SplineVariable>();
                newDefinition.Value = new Spline(splineContainer.Spline);

                AssetDatabase.CreateAsset(newDefinition, path);
                AssetDatabase.SaveAssets();

                Undo.RecordObject(exporter, "Assign New Target Definition");
                exporter.target = newDefinition;
                EditorUtility.SetDirty(exporter);

                Debug.Log(
                    $"[SplineAnimationExporter] Successfully created and exported Spline data to '{path}'.",
                    newDefinition);
            } else {
                if (GUILayout.Button("Export Spline to Definition", GUILayout.Height(30))) {
                    // Record undo on the ScriptableObject target
                    Undo.RecordObject(exporter.target, "Export Spline Data");

                    // Copy the spline data
                    exporter.target.Value = new Spline(splineContainer.Spline);

                    // Mark the asset as dirty so Unity saves it
                    EditorUtility.SetDirty(exporter.target);

                    Debug.Log($"[SplineAnimationExporter] Successfully exported Spline data to '{exporter.target.name}'.",
                        exporter.target);
                }

                if (GUILayout.Button("Import Spline from Definition", GUILayout.Height(30))) {
                    Undo.RecordObject(splineContainer, "Import Spline Data");

                    // Copy the spline data from the target
                    splineContainer.Spline = new Spline(exporter.target.Value);

                    // Mark the GameObject as dirty so Unity saves it
                    EditorUtility.SetDirty(splineContainer);

                    Debug.Log($"[SplineAnimationExporter] Successfully imported Spline data from '{exporter.target.name}'.",
                        exporter.gameObject);
                }
            }
        }
    }
}
