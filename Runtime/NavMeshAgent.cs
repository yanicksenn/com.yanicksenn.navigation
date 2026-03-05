using UnityEngine;

namespace YanickSenn.Navigation
{
    public class NavMeshAgent : MonoBehaviour
    {
        public NavMeshData navMeshData;
        public NavAgentDefinition agentDefinition;

        private NavAgentStrategy strategy;

        private void Awake()
        {
            if (agentDefinition != null)
            {
                strategy = agentDefinition.CreateStrategy(this);
            }
        }

        private void Update()
        {
            if (strategy != null)
            {
                strategy.Update(Time.deltaTime);
            }
        }

        public void SetDestination(Vector3 target) 
        { 
            strategy?.SetDestination(target);
        }

        public void Stop() 
        { 
            strategy?.Stop();
        }

        public bool HasPath => strategy?.HasPath ?? false;
        public Vector3[] CurrentPath => strategy?.CurrentPath ?? new Vector3[0];
        public Bounds[] CurrentPathCubes => strategy?.CurrentPathCubes ?? new Bounds[0];

        private void OnDrawGizmos()
        {
            strategy?.DrawGizmos();

            if (HasPath)
            {
                Gizmos.color = new Color(0f, 1f, 1f, 0.3f); // Cyan semi-transparent for path cubes
                foreach (var cube in CurrentPathCubes)
                {
                    Gizmos.DrawWireCube(cube.center, cube.size);
                }
            }
        }
    }
}
