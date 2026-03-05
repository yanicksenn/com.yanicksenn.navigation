using UnityEngine;

namespace YanickSenn.Navigation
{
    public class ConstantSpeedAgentStrategy : NavAgentStrategy
    {
        private readonly ConstantSpeedAgentDefinition definition;
        private Vector3[] path;
        private Bounds[] pathCubes;
        private int currentPathIndex;
        private bool hasPath;
        private Vector3 startPosition;

        public ConstantSpeedAgentStrategy(NavMeshAgent agent, ConstantSpeedAgentDefinition definition) : base(agent)
        {
            this.definition = definition;
            this.hasPath = false;
            this.path = new Vector3[0];
            this.pathCubes = new Bounds[0];
        }

        public override void SetDestination(Vector3 target)
        {
            startPosition = agent.transform.position;
            if (NavMeshPathfinder.TryFindPath(agent.navMeshData, startPosition, target, out path, out pathCubes))
            {
                currentPathIndex = 0;
                hasPath = true;
            }
            else
            {
                path = new Vector3[0];
                pathCubes = new Bounds[0];
                hasPath = false;
            }
        }

        public override void Stop()
        {
            hasPath = false;
            path = new Vector3[0];
            pathCubes = new Bounds[0];
        }

        public override void Update(float deltaTime)
        {
            if (Mathf.Abs(Vector3.Dot(agent.transform.forward, Vector3.up)) < 0.99f)
            {
                Quaternion targetUpRotation = Quaternion.LookRotation(agent.transform.forward, Vector3.up);
                agent.transform.rotation = Quaternion.Slerp(agent.transform.rotation, targetUpRotation, definition.upAlignmentSpeed * deltaTime);
            }

            if (!hasPath || path == null || currentPathIndex >= path.Length)
            {
                hasPath = false;
                return;
            }

            float remainingDistance = definition.speed * deltaTime;

            while (remainingDistance > 0 && currentPathIndex < path.Length)
            {
                Vector3 currentPosition = agent.transform.position;
                Vector3 targetPosition = path[currentPathIndex];
                Vector3 direction = targetPosition - currentPosition;
                float distance = direction.magnitude;

                if (distance > 0.0001f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, agent.transform.up);
                    agent.transform.rotation = Quaternion.Slerp(agent.transform.rotation, targetRotation, definition.forwardAlignmentSpeed * deltaTime);
                }

                if (remainingDistance >= distance)
                {
                    // Snaps to the waypoint and carries remaining speed to the next waypoint
                    agent.transform.position = targetPosition;
                    remainingDistance -= distance;
                    currentPathIndex++;

                    if (currentPathIndex >= path.Length)
                    {
                        hasPath = false;
                    }
                }
                else
                {
                    // Move partial distance towards the waypoint
                    agent.transform.position += direction.normalized * remainingDistance;
                    remainingDistance = 0;
                }
            }
        }

        public override bool HasPath => hasPath;

        public override Vector3[] CurrentPath => hasPath ? path : new Vector3[0];

        public override Bounds[] CurrentPathCubes => hasPath ? pathCubes : new Bounds[0];

        public override void DrawGizmos()
        {
            if (path == null || path.Length == 0) return;

            // Draw completed path in grey
            Gizmos.color = Color.grey;
            Vector3 lastPoint = startPosition;
            for (int i = 0; i < currentPathIndex; i++)
            {
                Gizmos.DrawLine(lastPoint, path[i]);
                lastPoint = path[i];
            }
            Gizmos.DrawLine(lastPoint, agent.transform.position);

            // Draw remaining path in yellow
            Gizmos.color = Color.yellow;
            lastPoint = agent.transform.position;
            for (int i = currentPathIndex; i < path.Length; i++)
            {
                Gizmos.DrawLine(lastPoint, path[i]);
                lastPoint = path[i];
            }
        }
    }
}
