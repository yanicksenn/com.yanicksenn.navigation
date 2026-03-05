using UnityEditor;
using UnityEngine;

namespace YanickSenn.Navigation.Editor
{
    public static class NavMeshVisualizationUtility
    {
        public static void DrawNavMeshAsGizmo(NavMeshData data)
        {
            if (data == null || data.WalkableCubes == null) return;

            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            foreach (var cube in data.WalkableCubes)
            {
                Gizmos.DrawWireCube(cube.center, cube.size);
            }
        }

        public static void DrawNavMeshAsHandle(NavMeshData data)
        {
            if (data == null || data.WalkableCubes == null) return;

            Handles.color = new Color(0f, 1f, 0f, 0.3f);
            foreach (var cube in data.WalkableCubes)
            {
                Handles.DrawWireCube(cube.center, cube.size);
            }
        }
    }

    [CustomEditor(typeof(NavMeshData))]
    public class NavMeshDataEditor : UnityEditor.Editor
    {
        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUIHook;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUIHook;
        }

        private void OnSceneGUIHook(SceneView sceneView)
        {
            if (target == null) return;
            NavMeshVisualizationUtility.DrawNavMeshAsHandle((NavMeshData)target);
        }
    }
}
