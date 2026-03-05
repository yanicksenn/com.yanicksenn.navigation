using System.Collections.Generic;
using UnityEngine;

namespace YanickSenn.Navigation
{
    public static class NavMeshPathfinder
    {
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
                    path = ReconstructPath(cameFrom, current, cubes, target, out pathCubes);
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

        private static Vector3[] ReconstructPath(Dictionary<int, int> cameFrom, int current, Bounds[] cubes, Vector3 target, out Bounds[] pathCubes)
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

            return pathPoints.ToArray();
        }
    }
}
