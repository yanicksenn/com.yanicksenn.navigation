using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

namespace YanickSenn.Navigation.Editor
{
    [CustomEditor(typeof(SplineAnimationDefinition))]
    public class SplineAnimationDefinitionEditor : UnityEditor.Editor
    {
        private float previewTime = 0f;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview Animation", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            previewTime = EditorGUILayout.Slider("Preview Time", previewTime, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                // Force scene view repaint when scrubbing the slider
                SceneView.RepaintAll();
            }
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        static void DrawGizmoForScriptableObject(SplineAnimationDefinition definition, GizmoType gizmoType)
        {
            if (definition.spline == null || definition.spline.Count == 0) return;

            Vector3 worldOrigin = Vector3.zero;
            Quaternion worldRotation = Quaternion.identity;

            Gizmos.color = Color.cyan;

            int resolution = Mathf.Max(10, Mathf.CeilToInt(definition.spline.GetLength() * 10f));

            Vector3 prevPos = worldOrigin + worldRotation * (Vector3)definition.spline.EvaluatePosition(0f);

            // Draw knots
            foreach (var knot in definition.spline.Knots)
            {
                Gizmos.color = Color.magenta;
                Vector3 knotPos = worldOrigin + worldRotation * new Vector3(knot.Position.x, knot.Position.y, knot.Position.z);
                Gizmos.DrawSphere(knotPos, 0.05f);
            }

            // Draw curve
            Gizmos.color = Color.cyan;
            for (int i = 1; i <= resolution; i++)
            {
                float t = i / (float)resolution;
                Vector3 nextPos = worldOrigin + worldRotation * (Vector3)definition.spline.EvaluatePosition(t);

                Gizmos.DrawLine(prevPos, nextPos);

                // Draw occasional forward tangents
                if (i % (resolution / 5) == 0)
                {
                    Vector3 tangent = (Vector3)definition.spline.EvaluateTangent(t);
                    if (tangent.sqrMagnitude > 0.001f)
                    {
                        Gizmos.color = Color.blue;
                        Gizmos.DrawRay(nextPos, tangent.normalized * 0.2f);
                        Gizmos.color = Color.cyan;
                    }
                }

                prevPos = nextPos;
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(prevPos, 0.06f);

            // Fetch the editor instance to access previewTime
            var activeEditors = ActiveEditorTracker.sharedTracker.activeEditors;
            foreach (var editor in activeEditors)
            {
                if (editor is SplineAnimationDefinitionEditor splineEditor && splineEditor.target == definition)
                {
                    splineEditor.DrawPreviewAgent(definition, worldOrigin, worldRotation);
                    break;
                }
            }
        }

        private void DrawPreviewAgent(SplineAnimationDefinition definition, Vector3 origin, Quaternion rotation)
        {
            float3 localPos = definition.spline.EvaluatePosition(previewTime);
            float3 tangent = definition.spline.EvaluateTangent(previewTime);

            Vector3 worldPos = origin + rotation * new Vector3(localPos.x, localPos.y, localPos.z);
            Vector3 tangentV3 = new Vector3(tangent.x, tangent.y, tangent.z);

            Gizmos.color = Color.green;

            // Draw dummy agent body
            if (tangentV3.sqrMagnitude > 0.001f)
            {
                Quaternion agentRot = rotation * Quaternion.LookRotation(tangentV3.normalized, Vector3.up);
                Gizmos.matrix = Matrix4x4.TRS(worldPos, agentRot, Vector3.one);
                Gizmos.DrawWireCube(new Vector3(0, 0.5f, 0), new Vector3(0.5f, 1f, 0.5f));

                // Draw strong forward indicator for preview
                Gizmos.color = Color.red;
                Gizmos.DrawLine(new Vector3(0, 0.5f, 0), new Vector3(0, 0.5f, 0.6f));
                Gizmos.DrawSphere(new Vector3(0, 0.5f, 0.6f), 0.05f);

                Gizmos.matrix = Matrix4x4.identity;
            }
            else
            {
                // Fallback if no tangent
                Gizmos.DrawWireCube(worldPos + new Vector3(0, 0.5f, 0), new Vector3(0.5f, 1f, 0.5f));
            }
        }
    }
}
