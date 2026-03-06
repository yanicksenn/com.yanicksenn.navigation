using NUnit.Framework;
using UnityEngine;

namespace YanickSenn.Navigation.Runtime.Tests
{
    public class AnchorSequenceAnimationDefinitionTests
    {
        private AnchorSequenceAnimationDefinition animMoveForward;
        private AnchorSequenceAnimationDefinition animTurnRight;

        [SetUp]
        public void SetUp()
        {
            animMoveForward = ScriptableObject.CreateInstance<AnchorSequenceAnimationDefinition>();
            animMoveForward.anchors = new[]
            {
                new TweenAnimationAnchor { localPosition = new Vector3(0, 0, 1), localForward = new Vector3(0, 0, 1), duration = 1f }
            };

            animTurnRight = ScriptableObject.CreateInstance<AnchorSequenceAnimationDefinition>();
            animTurnRight.anchors = new[]
            {
                new TweenAnimationAnchor { localPosition = Vector3.zero, localForward = new Vector3(1, 0, 0), duration = 0.5f }
            };
        }

        [Test]
        public void GetEndPosition_MoveForwardWithZeroRotation_ReturnsOffsetPosition()
        {
            Vector3 startPos = Vector3.zero;
            Quaternion startRot = Quaternion.identity;

            Vector3 endPos = animMoveForward.GetEndPosition(startPos, startRot);

            Assert.AreEqual(new Vector3(0, 0, 1), endPos);
        }

        [Test]
        public void GetEndPosition_MoveForwardWith90DegreeRotation_ReturnsOffsetPosition()
        {
            Vector3 startPos = Vector3.zero;
            Quaternion startRot = Quaternion.Euler(0, 90, 0);

            Vector3 endPos = animMoveForward.GetEndPosition(startPos, startRot);

            Assert.AreEqual(1f, endPos.x, 0.001f);
            Assert.AreEqual(0f, endPos.y, 0.001f);
            Assert.AreEqual(0f, endPos.z, 0.001f);
        }

        [Test]
        public void GetEndRotation_TurnRight_Returns90DegreeOffsetRotation()
        {
            Quaternion startRot = Quaternion.identity;

            Quaternion endRot = animTurnRight.GetEndRotation(startRot);

            Vector3 euler = endRot.eulerAngles;
            Assert.AreEqual(90f, euler.y, 0.001f);
        }
    }
}
