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
            // Expected: Smoothed path directly to Target at x=1
            Assert.AreEqual(1, path.Length);
            Assert.AreEqual(1f, path[0].x);
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
            // With String Pulling, line from Start(2,0,0) to C1|C3 portal(0,1,0) is clear, 
            // so the C2|C1 portal(1,0,0) is optimized out.
            Assert.AreEqual(2, path.Length);
            Assert.AreEqual(new Vector3(0, 1, 0), path[0]);
            Assert.AreEqual(new Vector3(0, 2, 0), path[1]);
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

        [Test]
        public void SamplePosition_InsideCube_ReturnsSamePosition()
        {
            var c1 = new Bounds(new Vector3(0, 0, 0), new Vector3(2, 2, 2));
            var data = CreateNavMeshData(c1);

            bool found = NavMeshPathfinder.SamplePosition(data, new Vector3(0.5f, 0.5f, 0.5f), out var hitPosition, 10f);

            Assert.IsTrue(found);
            Assert.AreEqual(new Vector3(0.5f, 0.5f, 0.5f), hitPosition);
        }

        [Test]
        public void SamplePosition_OutsideCubeWithinMaxDistance_ReturnsClosestPoint()
        {
            var c1 = new Bounds(new Vector3(0, 0, 0), new Vector3(2, 2, 2));
            var data = CreateNavMeshData(c1);

            bool found = NavMeshPathfinder.SamplePosition(data, new Vector3(2f, 0f, 0f), out var hitPosition, 2f);

            Assert.IsTrue(found);
            Assert.AreEqual(new Vector3(1f, 0f, 0f), hitPosition);
        }

        [Test]
        public void SamplePosition_OutsideCubeBeyondMaxDistance_ReturnsFalse()
        {
            var c1 = new Bounds(new Vector3(0, 0, 0), new Vector3(2, 2, 2));
            var data = CreateNavMeshData(c1);

            bool found = NavMeshPathfinder.SamplePosition(data, new Vector3(5f, 0f, 0f), out var hitPosition, 2f);

            Assert.IsFalse(found);
            Assert.AreEqual(new Vector3(5f, 0f, 0f), hitPosition);
        }

        [Test]
        public void SamplePosition_MultipleCubes_ReturnsClosestPointOverall()
        {
            var c1 = new Bounds(new Vector3(0, 0, 0), new Vector3(2, 2, 2));
            var c2 = new Bounds(new Vector3(5, 0, 0), new Vector3(2, 2, 2));
            var data = CreateNavMeshData(c1, c2);

            bool found = NavMeshPathfinder.SamplePosition(data, new Vector3(3f, 0f, 0f), out var hitPosition, 10f);

            Assert.IsTrue(found);
            Assert.AreEqual(new Vector3(4f, 0f, 0f), hitPosition);
        }
    }
}
