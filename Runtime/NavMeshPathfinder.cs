using System.Collections.Generic;
using UnityEngine;

namespace YanickSenn.Navigation
{
    public static class NavMeshPathfinder
    {
        public static bool SamplePosition(NavMeshData navMeshData, Vector3 sourcePosition, out Vector3 hitPosition, float maxDistance)
        {
            hitPosition = sourcePosition;
            if (navMeshData == null || navMeshData.WalkableCubes == null || navMeshData.WalkableCubes.Length == 0) return false;

            float maxSqrDistance = maxDistance * maxDistance;
            float closestSqrDistance = float.MaxValue;
            Vector3 closestPoint = sourcePosition;
            bool found = false;

            for (int i = 0; i < navMeshData.WalkableCubes.Length; i++)
            {
                Bounds cube = navMeshData.WalkableCubes[i];
                if (cube.Contains(sourcePosition))
                {
                    hitPosition = sourcePosition;
                    return true;
                }

                Vector3 point = cube.ClosestPoint(sourcePosition);
                float sqrDist = (point - sourcePosition).sqrMagnitude;

                if (sqrDist <= maxSqrDistance && sqrDist < closestSqrDistance)
                {
                    closestSqrDistance = sqrDist;
                    closestPoint = point;
                    found = true;
                }
            }

            if (found)
            {
                hitPosition = closestPoint;
                return true;
            }

            return false;
        }

        public static bool TryFindPath(NavMeshData navMeshData, Vector3 start, Vector3 target, out Vector3[] path, out Bounds[] pathCubes)
        {
            path = null;
            pathCubes = null;
            if (navMeshData == null || navMeshData.WalkableCubes == null || navMeshData.WalkableCubes.Length == 0) return false;

            Bounds[] cubes = navMeshData.WalkableCubes;
            int startIndex = GetClosestCubeIndex(cubes, start);
            int targetIndex = GetClosestCubeIndex(cubes, target);

            if (startIndex == -1 || targetIndex == -1) return false;

            if (startIndex == targetIndex)
            {
                path = new Vector3[] { target };
                pathCubes = new Bounds[] { cubes[startIndex] };
                return true;
            }

            List<int> openSet = new List<int> { startIndex };
            HashSet<int> closedSet = new HashSet<int>();

            Dictionary<int, int> cameFrom = new Dictionary<int, int>();
            Dictionary<int, float> gScore = new Dictionary<int, float>();
            Dictionary<int, float> fScore = new Dictionary<int, float>();

            gScore[startIndex] = 0;
            fScore[startIndex] = Vector3.Distance(cubes[startIndex].center, cubes[targetIndex].center);

            while (openSet.Count > 0)
            {
                // Get node with lowest fScore
                int current = openSet[0];
                float lowestF = fScore.GetValueOrDefault(current, float.MaxValue);
                for (int i = 1; i < openSet.Count; i++)
                {
                    float f = fScore.GetValueOrDefault(openSet[i], float.MaxValue);
                    if (f < lowestF)
                    {
                        current = openSet[i];
                        lowestF = f;
                    }
                }

                if (current == targetIndex)
                {
                    path = ReconstructPath(cameFrom, current, cubes, start, target, out pathCubes);
                    return true;
                }

                openSet.Remove(current);
                closedSet.Add(current);

                Bounds currentBounds = cubes[current];

                for (int neighbor = 0; neighbor < cubes.Length; neighbor++)
                {
                    if (neighbor == current || closedSet.Contains(neighbor)) continue;

                    Bounds neighborBounds = cubes[neighbor];
                    
                    Vector3 min = Vector3.Max(currentBounds.min, neighborBounds.min);
                    Vector3 max = Vector3.Min(currentBounds.max, neighborBounds.max);
                    Vector3 overlap = max - min;

                    // Ensure they actually touch or overlap (allow small float error)
                    if (overlap.x >= -0.01f && overlap.y >= -0.01f && overlap.z >= -0.01f)
                    {
                        // Check if they share a face (at least 2 dimensions have positive overlap)
                        int overlappingDimensions = 0;
                        if (overlap.x > 0.01f) overlappingDimensions++;
                        if (overlap.y > 0.01f) overlappingDimensions++;
                        if (overlap.z > 0.01f) overlappingDimensions++;

                        if (overlappingDimensions >= 2)
                        {
                            float tentativeGScore = gScore[current] + Vector3.Distance(currentBounds.center, neighborBounds.center);

                            if (tentativeGScore < gScore.GetValueOrDefault(neighbor, float.MaxValue))
                            {
                                cameFrom[neighbor] = current;
                                gScore[neighbor] = tentativeGScore;
                                fScore[neighbor] = tentativeGScore + Vector3.Distance(neighborBounds.center, cubes[targetIndex].center);

                                if (!openSet.Contains(neighbor))
                                {
                                    openSet.Add(neighbor);
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        private static int GetClosestCubeIndex(Bounds[] cubes, Vector3 point)
        {
            int closestIndex = -1;
            float closestSqrDistance = float.MaxValue;

            for (int i = 0; i < cubes.Length; i++)
            {
                if (cubes[i].Contains(point)) return i;

                float sqrDist = cubes[i].SqrDistance(point);
                if (sqrDist < closestSqrDistance)
                {
                    closestSqrDistance = sqrDist;
                    closestIndex = i;
                }
            }

            return closestIndex;
        }

        private static Vector3[] ReconstructPath(Dictionary<int, int> cameFrom, int current, Bounds[] cubes, Vector3 start, Vector3 target, out Bounds[] pathCubes)
        {
            List<int> cubePath = new List<int> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                cubePath.Add(current);
            }
            cubePath.Reverse();

            pathCubes = new Bounds[cubePath.Count];
            for (int i = 0; i < cubePath.Count; i++)
            {
                pathCubes[i] = cubes[cubePath[i]];
            }

            List<Vector3> pathPoints = new List<Vector3>();
            pathPoints.Add(start);
            
            // Generate portal intersections for transitioning between walkable cubes
            for (int i = 0; i < cubePath.Count - 1; i++)
            {
                Bounds b1 = cubes[cubePath[i]];
                Bounds b2 = cubes[cubePath[i + 1]];
                
                b1.Expand(0.01f);
                b2.Expand(0.01f);
                
                Vector3 min = Vector3.Max(b1.min, b2.min);
                Vector3 max = Vector3.Min(b1.max, b2.max);
                Vector3 portalCenter = (min + max) * 0.5f;
                
                pathPoints.Add(portalCenter);
            }
            
            // The ultimate destination
            pathPoints.Add(target);

            // Path Smoothing (String Pulling)
            List<Vector3> smoothedPath = new List<Vector3>();
            smoothedPath.Add(pathPoints[0]);

            int currentIndex = 0;
            while (currentIndex < pathPoints.Count - 1)
            {
                int furthestVisibleIndex = currentIndex + 1;
                for (int i = pathPoints.Count - 1; i > currentIndex + 1; i--)
                {
                    if (IsLineClear(pathPoints[currentIndex], pathPoints[i], pathCubes))
                    {
                        furthestVisibleIndex = i;
                        break;
                    }
                }
                
                smoothedPath.Add(pathPoints[furthestVisibleIndex]);
                currentIndex = furthestVisibleIndex;
            }

            // Remove the start point if it's the first element and it wasn't the target itself.
            // Usually the agent doesn't need its current position as the first waypoint.
            if (smoothedPath.Count > 1)
            {
                smoothedPath.RemoveAt(0);
            }

            return smoothedPath.ToArray();
        }

        private struct CubeInterval : System.IComparable<CubeInterval>
        {
            public float tEnter;
            public float tExit;
            public Bounds cube;

            public int CompareTo(CubeInterval other)
            {
                return tEnter.CompareTo(other.tEnter);
            }
        }

        private static bool IsLineClear(Vector3 from, Vector3 to, Bounds[] pathCubes)
        {
            Vector3 dir = to - from;
            float len = dir.magnitude;
            if (len < 0.001f) return true;

            List<CubeInterval> intervals = new List<CubeInterval>();

            foreach (var cube in pathCubes)
            {
                // Expand bounds very slightly to ensure intervals touch and mitigate float errors
                Bounds expandedCube = cube;
                expandedCube.Expand(0.002f);

                if (IntersectRayAABB(from, dir, expandedCube, out float tEnter, out float tExit))
                {
                    // Clamp to [0, 1] segment
                    tEnter = Mathf.Max(0f, tEnter);
                    tExit = Mathf.Min(1f, tExit);

                    if (tExit - tEnter > 0.001f) // Ignore point touches or extremely short segments
                    {
                        intervals.Add(new CubeInterval { tEnter = tEnter, tExit = tExit, cube = cube });
                    }
                }
            }

            if (intervals.Count == 0) return false;

            intervals.Sort();

            // 1. Check if starts at 0
            if (intervals[0].tEnter > 0.001f) return false;

            float currentCoveredT = intervals[0].tExit;
            Bounds currentCube = intervals[0].cube;

            for (int i = 1; i < intervals.Count; i++)
            {
                var nextInterval = intervals[i];

                // If this interval is completely inside the current covered area, ignore it
                if (nextInterval.tExit <= currentCoveredT) continue;

                // 2. Check for gap
                if (nextInterval.tEnter > currentCoveredT + 0.001f)
                {
                    return false; // Gap found
                }

                // 3. Transition check
                // We are transitioning from currentCube to nextInterval.cube
                if (currentCube != nextInterval.cube && !SharesFace(currentCube, nextInterval.cube))
                {
                    return false; // Invalid transition (e.g. corner cut)
                }

                currentCoveredT = Mathf.Max(currentCoveredT, nextInterval.tExit);
                currentCube = nextInterval.cube;
            }

            // 4. Check if ends at 1
            if (currentCoveredT < 0.999f) return false;

            return true;
        }

        private static bool IntersectRayAABB(Vector3 origin, Vector3 dir, Bounds bounds, out float tMin, out float tMax)
        {
            tMin = float.NegativeInfinity;
            tMax = float.PositiveInfinity;

            for (int i = 0; i < 3; i++)
            {
                if (Mathf.Abs(dir[i]) < 1e-6f)
                {
                    if (origin[i] < bounds.min[i] || origin[i] > bounds.max[i])
                    {
                        return false;
                    }
                }
                else
                {
                    float invD = 1.0f / dir[i];
                    float t0 = (bounds.min[i] - origin[i]) * invD;
                    float t1 = (bounds.max[i] - origin[i]) * invD;

                    if (invD < 0.0f)
                    {
                        float temp = t0;
                        t0 = t1;
                        t1 = temp;
                    }

                    tMin = t0 > tMin ? t0 : tMin;
                    tMax = t1 < tMax ? t1 : tMax;

                    if (tMax < tMin)
                        return false;
                }
            }

            return tMax >= 0f;
        }

        private static bool SharesFace(Bounds b1, Bounds b2)
        {
            Vector3 min = Vector3.Max(b1.min, b2.min);
            Vector3 max = Vector3.Min(b1.max, b2.max);
            Vector3 overlap = max - min;

            if (overlap.x >= -0.01f && overlap.y >= -0.01f && overlap.z >= -0.01f)
            {
                int overlappingDimensions = 0;
                if (overlap.x > 0.01f) overlappingDimensions++;
                if (overlap.y > 0.01f) overlappingDimensions++;
                if (overlap.z > 0.01f) overlappingDimensions++;
                
                return overlappingDimensions >= 2;
            }
            return false;
        }
    }
}
