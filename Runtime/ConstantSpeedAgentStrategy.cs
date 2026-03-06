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
                if (definition.pathSmoothingFactor > 0f && definition.pathSmoothingIterations > 0 && path.Length > 2)
                {
                    path = SmoothPath(path, definition.pathSmoothingFactor, definition.pathSmoothingIterations);
                }

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

        private Vector3[] SmoothPath(Vector3[] originalPath, float factor, int iterations)
        {
            var currentPath = new System.Collections.Generic.List<Vector3>(originalPath);

            for (int i = 0; i < iterations; i++)
            {
                if (currentPath.Count < 3)
                    break;

                var nextPath = new System.Collections.Generic.List<Vector3>();

                // Keep the first point
                nextPath.Add(currentPath[0]);

                for (int j = 0; j < currentPath.Count - 1; j++)
                {
                    Vector3 p0 = currentPath[j];
                    Vector3 p1 = currentPath[j + 1];

                    // Don't cut the corners at the very start and end if we only have 2 segments left overall,
                    // but in Chaikin's open curves, we usually take the points at "factor" and "1-factor"
                    // along each segment, except we keep the exact start and end points.

                    if (j == 0)
                    {
                        // First segment: keeps p0 (already added), adds point near p1
                        nextPath.Add(Vector3.Lerp(p0, p1, 1f - factor));
                    }
                    else if (j == currentPath.Count - 2)
                    {
                        // Last segment: adds point near p0, keeps p1 (added after loop)
                        nextPath.Add(Vector3.Lerp(p0, p1, factor));
                    }
                    else
                    {
                        // Middle segments: adds two points, one near p0, one near p1
                        nextPath.Add(Vector3.Lerp(p0, p1, factor));
                        nextPath.Add(Vector3.Lerp(p0, p1, 1f - factor));
                    }
                }

                // Keep the last point
                nextPath.Add(currentPath[currentPath.Count - 1]);

                currentPath = nextPath;
            }

            return currentPath.ToArray();
        }

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
