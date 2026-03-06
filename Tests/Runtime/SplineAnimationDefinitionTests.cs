using NUnit.Framework;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

namespace YanickSenn.Navigation.Runtime.Tests
{
    public class SplineAnimationDefinitionTests
    {
        private SplineAnimationDefinition straightSplineDef;
        private SplineAnimationDefinition curvedSplineDef;

        [SetUp]
        public void SetUp()
        {
            straightSplineDef = ScriptableObject.CreateInstance<SplineAnimationDefinition>();
            straightSplineDef.spline = new Spline();
            straightSplineDef.spline.Add(new BezierKnot(new float3(0, 0, 0)));
            straightSplineDef.spline.Add(new BezierKnot(new float3(0, 0, 1))); // Straight ahead

            curvedSplineDef = ScriptableObject.CreateInstance<SplineAnimationDefinition>();
            curvedSplineDef.spline = new Spline();
            curvedSplineDef.spline.Add(new BezierKnot(new float3(0, 0, 0), new float3(0, 0, -0.5f), new float3(0, 0, 0.5f)));
            curvedSplineDef.spline.Add(new BezierKnot(new float3(1, 0, 1), new float3(-0.5f, 0, 0), new float3(0.5f, 0, 0))); // Curve to the right
        }

        [Test]
        public void GetEndPosition_StraightSpline_ReturnsEndpoint()
        {
            Vector3 startPos = Vector3.zero;
            Quaternion startRot = Quaternion.identity;

            Vector3 endPos = straightSplineDef.GetEndPosition(startPos, startRot);

            Assert.AreEqual(new Vector3(0, 0, 1), endPos);
        }

        [Test]
        public void GetEndPosition_StraightSplineWithStartRotation_ReturnsRotatedEndpoint()
        {
            Vector3 startPos = Vector3.zero;
            Quaternion startRot = Quaternion.Euler(0, 90, 0); // facing right

            Vector3 endPos = straightSplineDef.GetEndPosition(startPos, startRot);

            Assert.AreEqual(1f, endPos.x, 0.001f);
            Assert.AreEqual(0f, endPos.y, 0.001f);
            Assert.AreEqual(0f, endPos.z, 0.001f);
        }

        [Test]
        public void GetEndRotation_StraightSpline_ReturnsForward()
        {
            Quaternion startRot = Quaternion.identity;

            Quaternion endRot = straightSplineDef.GetEndRotation(startRot);
            
            // Tangent of a straight line along Z is forward
            Vector3 finalForward = endRot * Vector3.forward;
            Assert.AreEqual(0f, finalForward.x, 0.001f);
            Assert.AreEqual(0f, finalForward.y, 0.001f);
            Assert.AreEqual(1f, finalForward.z, 0.001f);
        }

        [Test]
        public void CheckValidity_ValidPath_ReturnsTrue()
        {
            // Walkable corridor (0,0,0) -> (0,0,1)
            Bounds[] pathCubes = new[]
            {
                new Bounds(new Vector3(0, 0, 0.5f), new Vector3(1, 1, 2))
            };

            bool isValid = straightSplineDef.CheckValidity(null, Vector3.zero, Quaternion.identity, pathCubes);

            Assert.IsTrue(isValid);
        }

        [Test]
        public void CheckValidity_SplineGoesOutsideBounds_ReturnsFalse()
        {
            // Only a small box at origin
            Bounds[] pathCubes = new[]
            {
                new Bounds(new Vector3(0, 0, 0), new Vector3(0.5f, 0.5f, 0.5f))
            };

            // Spline goes to Z=1, which is outside the box
            bool isValid = straightSplineDef.CheckValidity(null, Vector3.zero, Quaternion.identity, pathCubes);

            Assert.IsFalse(isValid);
        }
    }
}
