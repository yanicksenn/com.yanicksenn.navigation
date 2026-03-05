using System.Collections.Generic;
using UnityEngine;

namespace YanickSenn.Navigation
{
    public static class NavMeshBuilder
    {
        public struct BakeSettings
        {
            public float MinCubeSize;
            public Bounds ConstraintBounds;
        }

        public static List<Bounds> Bake(BakeSettings settings, IList<Bounds> obstacleBounds)
        {
            var walkableCubes = new List<Bounds>();
            Subdivide(settings.ConstraintBounds, settings.MinCubeSize, obstacleBounds, walkableCubes);
            return walkableCubes;
        }

        private static void Subdivide(Bounds currentBounds, float minCubeSize, IList<Bounds> obstacleBounds, List<Bounds> walkableCubes)
        {
            bool intersects = false;
            for (int i = 0; i < obstacleBounds.Count; i++)
            {
                if (IntersectsExclusive(currentBounds, obstacleBounds[i]))
                {
                    intersects = true;
                    break;
                }
            }

            if (!intersects)
            {
                walkableCubes.Add(currentBounds);
                return;
            }

            // Check if we can subdivide. Using slightly smaller value to avoid float precision infinite loops
            if (currentBounds.size.x <= minCubeSize * 1.001f || 
                currentBounds.size.y <= minCubeSize * 1.001f || 
                currentBounds.size.z <= minCubeSize * 1.001f)
            {
                // Cannot subdivide further. This cube is considered unwalkable (discarded).
                return;
            }

            // Subdivide into 8 children
            Vector3 childSize = currentBounds.size * 0.5f;
            Vector3 center = currentBounds.center;
            Vector3 extents = childSize * 0.5f;

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 childCenter = center + new Vector3(x * extents.x, y * extents.y, z * extents.z);
                        Bounds childBounds = new Bounds(childCenter, childSize);
                        Subdivide(childBounds, minCubeSize, obstacleBounds, walkableCubes);
                    }
                }
            }
        }

        private static bool IntersectsExclusive(Bounds a, Bounds b)
        {
            return a.min.x < b.max.x && a.max.x > b.min.x &&
                   a.min.y < b.max.y && a.max.y > b.min.y &&
                   a.min.z < b.max.z && a.max.z > b.min.z;
        }
    }
}
