using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace YanickSenn.Navigation.Tests
{
    public class NavMeshBuilderTests
    {
        [Test]
        public void Bake_EmptySpace_YieldsSingleCube()
        {
            var settings = new NavMeshBuilder.BakeSettings
            {
                ConstraintBounds = new Bounds(Vector3.zero, new Vector3(100, 100, 100)),
                MinCubeSize = 1f
            };
            var obstacles = new List<Bounds>();

            var result = NavMeshBuilder.Bake(settings, obstacles);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(new Vector3(100, 100, 100), result[0].size);
        }

        [Test]
        public void Bake_SingleObstacle_SubdividesCorrectly()
        {
            var settings = new NavMeshBuilder.BakeSettings
            {
                ConstraintBounds = new Bounds(Vector3.zero, new Vector3(4, 4, 4)),
                MinCubeSize = 2f
            };
            var obstacles = new List<Bounds>
            {
                new Bounds(new Vector3(1, 1, 1), new Vector3(2, 2, 2))
            };

            var result = NavMeshBuilder.Bake(settings, obstacles);

            // Initial is 4x4x4. Intersects 2x2x2 obstacle in the top-right-forward corner.
            // Subdivides to eight 2x2x2 cubes.
            // One child will intersect completely (and be dropped as it's size 2 = min size).
            // So we expect 7 Walkable Cubes.
            Assert.AreEqual(7, result.Count);
        }

        [Test]
        public void Bake_MinSizeIsEnforced()
        {
            var settings = new NavMeshBuilder.BakeSettings
            {
                ConstraintBounds = new Bounds(Vector3.zero, new Vector3(4, 4, 4)),
                MinCubeSize = 2f
            };
            var obstacles = new List<Bounds>
            {
                new Bounds(new Vector3(0, 0, 0), new Vector3(0.1f, 0.1f, 0.1f)) // small obstacle in center
            };

            var result = NavMeshBuilder.Bake(settings, obstacles);

            // Initial 4x4x4 -> 8 children of 2x2x2.
            // Center obstacle will intersect all 8 children, since it is at (0,0,0) exactly on the border.
            // Since minCubeSize is 2, the 8 children will NOT subdivide further and will be discarded.
            Assert.AreEqual(0, result.Count);
        }
    }
}
