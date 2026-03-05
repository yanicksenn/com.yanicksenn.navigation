using UnityEngine;

namespace YanickSenn.Navigation
{
    [CreateAssetMenu(fileName = "NavMeshData", menuName = "Navigation/NavMesh Data")]
    public class NavMeshData : ScriptableObject
    {
        [HideInInspector]
        public Bounds[] WalkableCubes;
    }
}
