using UnityEditor;
using UnityEngine;
using YanickSenn.Navigation;

namespace YanickSenn.Navigation.Editor
{
    [CustomEditor(typeof(AnchorSequenceAnimationDefinition))]
    public class AnchorSequenceAnimationDefinitionEditor : UnityEditor.Editor
    {
        // Adding the HasPreviewGUI ensures the editor knows it might draw stuff,
        // but for SceneView Gizmos we hook into OnSceneGUI or RenderStaticPreview.
        // The most robust way for ScriptableObjects to draw in SceneView when selected
        // is via a static delegate hook in the Editor, or overriding HasPreviewGUI and OnPreviewGUI.

        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        static void DrawGizmoForScriptableObject(AnchorSequenceAnimationDefinition definition, GizmoType gizmoType)
        {
            if (definition.anchors == null || definition.anchors.Length == 0) return;

            // Draw relative to the origin of the world so the user can see it near the grid or (0,0,0)
            Vector3 currentPos = Vector3.zero;

            Gizmos.color = Color.cyan;

            for (int i = 0; i < definition.anchors.Length; i++)
            {
                Vector3 nextPos = currentPos + definition.anchors[i].localPosition;
                Vector3 nextForward = definition.anchors[i].localForward;
                
                Gizmos.DrawLine(currentPos, nextPos);
                
                if (nextForward.sqrMagnitude > 0.001f)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawRay(nextPos, nextForward.normalized * 0.2f);
                    Gizmos.color = Color.cyan;
                }

                currentPos = nextPos;
            }
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(currentPos, 0.05f);
        }
    }
}
