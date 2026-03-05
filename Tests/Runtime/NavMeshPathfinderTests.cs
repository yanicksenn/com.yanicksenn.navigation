using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace YanickSenn.Navigation.Tests
{
    public class NavMeshPathfinderTests
    {
        private NavMeshData CreateNavMeshData(params Bounds[] cubes)
        {
            var data = ScriptableObject.CreateInstance<NavMeshData>();
            data.WalkableCubes = cubes;
            return data;
        }

        [Test]
        public void TryFindPath_SingleCube_ReturnsTarget()
        {
            var data = CreateNavMeshData(new Bounds(Vector3.zero, new Vector3(10, 10, 10)));
            
            bool found = NavMeshPathfinder.TryFindPath(data, Vector3.zero, new Vector3(2, 0, 0), out var path, out var pathCubes);

            Assert.IsTrue(found);
            Assert.AreEqual(1, path.Length);
            Assert.AreEqual(new Vector3(2, 0, 0), path[0]);
        }

        [Test]
        public void TryFindPath_TwoAdjacentCubes_ReturnsPortalAndTarget()
        {
            // Two 2x2x2 cubes adjacent on X axis
            var c1 = new Bounds(new Vector3(-1, 0, 0), new Vector3(2, 2, 2));
            var c2 = new Bounds(new Vector3(1, 0, 0), new Vector3(2, 2, 2));
            var data = CreateNavMeshData(c1, c2);

            bool found = NavMeshPathfinder.TryFindPath(data, new Vector3(-1, 0, 0), new Vector3(1, 0, 0), out var path, out var pathCubes);

            Assert.IsTrue(found);
            // Expected: Portal at x=0, then Target at x=1
            Assert.AreEqual(2, path.Length);
            Assert.AreEqual(0f, path[0].x);
            Assert.AreEqual(1f, path[1].x);
        }

        [Test]
        public void TryFindPath_ObstacleAvoidance_FindsLPath()
        {
            /*
              Target (0,2)
                |
              Cube3 (0,2)
                |
              Cube1 (0,0) -- Cube2 (2,0) -- Start (2,0)
            */
            var c1 = new Bounds(new Vector3(0, 0, 0), new Vector3(2, 2, 2));
            var c2 = new Bounds(new Vector3(2, 0, 0), new Vector3(2, 2, 2));
            var c3 = new Bounds(new Vector3(0, 2, 0), new Vector3(2, 2, 2));
            var data = CreateNavMeshData(c1, c2, c3);

            bool found = NavMeshPathfinder.TryFindPath(data, new Vector3(2, 0, 0), new Vector3(0, 2, 0), out var path, out var pathCubes);

            Assert.IsTrue(found);
            // Should go through C2 -> C1 -> C3
            // Portals: C2|C1 at (1,0,0), C1|C3 at (0,1,0), then Target (0,2,0)
            Assert.AreEqual(3, path.Length);
            Assert.AreEqual(new Vector3(1, 0, 0), path[0]);
            Assert.AreEqual(new Vector3(0, 1, 0), path[1]);
            Assert.AreEqual(new Vector3(0, 2, 0), path[2]);
        }

        [Test]
        public void TryFindPath_NoPath_ReturnsFalse()
        {
            var c1 = new Bounds(new Vector3(0, 0, 0), new Vector3(2, 2, 2));
            var c2 = new Bounds(new Vector3(10, 0, 0), new Vector3(2, 2, 2));
            var data = CreateNavMeshData(c1, c2);

            bool found = NavMeshPathfinder.TryFindPath(data, Vector3.zero, new Vector3(10, 0, 0), out var path, out var pathCubes);

            Assert.IsFalse(found);
            Assert.IsNull(path);
        }
    }
}
