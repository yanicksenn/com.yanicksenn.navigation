using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace YanickSenn.Navigation.Editor
{
    public class NavMeshBakeWindow : EditorWindow
    {
        private bool constrainMinX = false;
        private bool constrainMaxX = false;
        private bool constrainMinY = false;
        private bool constrainMaxY = false;
        private bool constrainMinZ = false;
        private bool constrainMaxZ = false;

        private float minX = -10f;
        private float maxX = 10f;
        private float minY = -10f;
        private float maxY = 10f;
        private float minZ = -10f;
        private float maxZ = 10f;

        private float minCubeSize = 1f;
        [SerializeField] private LayerMask collisionLayerMask = -1;

        private NavMeshData bakedData;
        private const float INFINITY_REPRESENTATION = 100000f;

        private SerializedObject serializedObject;
        private SerializedProperty layerMaskProp;

        [MenuItem("Window/Navigation/Nav Mesh Baker")]
        public static void ShowWindow()
        {
            GetWindow<NavMeshBakeWindow>("Nav Mesh Baker");
        }

        private void OnEnable()
        {
            serializedObject = new SerializedObject(this);
            layerMaskProp = serializedObject.FindProperty("collisionLayerMask");
        }

        private void OnGUI()
        {
            serializedObject.Update();

            GUILayout.Label("Baking Bounds", EditorStyles.boldLabel);
            
            DrawConstraintToggle(ref constrainMinX, "Min X", ref minX);
            DrawConstraintToggle(ref constrainMaxX, "Max X", ref maxX);
            DrawConstraintToggle(ref constrainMinY, "Min Y", ref minY);
            DrawConstraintToggle(ref constrainMaxY, "Max Y", ref maxY);
            DrawConstraintToggle(ref constrainMinZ, "Min Z", ref minZ);
            DrawConstraintToggle(ref constrainMaxZ, "Max Z", ref maxZ);

            EditorGUILayout.Space();

            minCubeSize = EditorGUILayout.FloatField("Min Cube Size", minCubeSize);
            minCubeSize = Mathf.Max(0.1f, minCubeSize);

            EditorGUILayout.PropertyField(layerMaskProp, new GUIContent("Collision Layer"));

            bakedData = (NavMeshData)EditorGUILayout.ObjectField("Baked Data Object", bakedData, typeof(NavMeshData), false);

            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Bake"))
            {
                Bake();
            }
        }

        private void DrawConstraintToggle(ref bool toggle, string label, ref float value)
        {
            EditorGUILayout.BeginHorizontal();
            toggle = EditorGUILayout.Toggle(toggle, GUILayout.Width(20));
            EditorGUI.BeginDisabledGroup(!toggle);
            value = EditorGUILayout.FloatField(label, value);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }

        private void Bake()
        {
            if (bakedData == null)
            {
                Debug.LogError("No NavMeshData assigned to store the bake results.");
                return;
            }

            float finalMinX = constrainMinX ? minX : -INFINITY_REPRESENTATION;
            float finalMaxX = constrainMaxX ? maxX : INFINITY_REPRESENTATION;
            float finalMinY = constrainMinY ? minY : -INFINITY_REPRESENTATION;
            float finalMaxY = constrainMaxY ? maxY : INFINITY_REPRESENTATION;
            float finalMinZ = constrainMinZ ? minZ : -INFINITY_REPRESENTATION;
            float finalMaxZ = constrainMaxZ ? maxZ : INFINITY_REPRESENTATION;

            Vector3 min = new Vector3(finalMinX, finalMinY, finalMinZ);
            Vector3 max = new Vector3(finalMaxX, finalMaxY, finalMaxZ);

            Bounds rootBounds = new Bounds();
            rootBounds.SetMinMax(min, max);

            var obstacleBounds = new List<Bounds>();
            var allColliders = FindObjectsOfType<Collider>();
            foreach (var coll in allColliders)
            {
                if ((collisionLayerMask.value & (1 << coll.gameObject.layer)) != 0)
                {
                    obstacleBounds.Add(coll.bounds);
                }
            }

            var settings = new NavMeshBuilder.BakeSettings
            {
                MinCubeSize = minCubeSize,
                ConstraintBounds = rootBounds
            };

            List<Bounds> result = NavMeshBuilder.Bake(settings, obstacleBounds);

            Undo.RecordObject(bakedData, "Bake NavMesh");
            bakedData.WalkableCubes = result.ToArray();
            EditorUtility.SetDirty(bakedData);
            AssetDatabase.SaveAssets();

            Debug.Log($"Bake complete! Generated {result.Count} walkable cubes.");
        }
    }
}
