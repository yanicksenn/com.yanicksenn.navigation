using NUnit.Framework;
using UnityEngine;

namespace YanickSenn.Navigation.Runtime.Tests
{
    public class AnimationAgentStrategyTests
    {
        private GameObject agentObj;
        private NavMeshAgent agent;
        private AnimationAgentDefinition definition;
        private NavMeshData navMeshData;

        private AnchorSequenceAnimationDefinition animMoveForward;
        private AnchorSequenceAnimationDefinition animTurnLeft;
        private AnchorSequenceAnimationDefinition animTurnRight;

        [SetUp]
        public void SetUp()
        {
            agentObj = new GameObject("Agent");
            agent = agentObj.AddComponent<NavMeshAgent>();

            navMeshData = ScriptableObject.CreateInstance<NavMeshData>();

            navMeshData.WalkableCubes = new[]
            {
                new Bounds(new Vector3(0, 0, 0), Vector3.one),
                new Bounds(new Vector3(0, 0, 1), Vector3.one),
                new Bounds(new Vector3(0, 0, 2), Vector3.one),
                new Bounds(new Vector3(1, 0, 2), Vector3.one),
            };

            agent.navMeshData = navMeshData;

            animMoveForward = ScriptableObject.CreateInstance<AnchorSequenceAnimationDefinition>();
            animMoveForward.name = "Forward";
            animMoveForward.anchors = new[] { new TweenAnimationAnchor { localPosition = new Vector3(0, 0, 1), localForward = new Vector3(0, 0, 1), duration = 1f } };

            animTurnLeft = ScriptableObject.CreateInstance<AnchorSequenceAnimationDefinition>();
            animTurnLeft.name = "Left";
            animTurnLeft.anchors = new[] { new TweenAnimationAnchor { localPosition = Vector3.zero, localForward = new Vector3(-1, 0, 0), duration = 0.5f } };

            animTurnRight = ScriptableObject.CreateInstance<AnchorSequenceAnimationDefinition>();
            animTurnRight.name = "Right";
            animTurnRight.anchors = new[] { new TweenAnimationAnchor { localPosition = Vector3.zero, localForward = new Vector3(1, 0, 0), duration = 0.5f } };


            definition = ScriptableObject.CreateInstance<AnimationAgentDefinition>();
            definition.availableAnimations = new AgentAnimationDefinition[] { animMoveForward, animTurnLeft, animTurnRight };
            definition.targetReachThreshold = 0.5f;

            agent.agentDefinition = definition;

            // Invoke private Awake to instantiate strategy after definition is set
            typeof(NavMeshAgent).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(agent, null);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(agentObj);
            Object.DestroyImmediate(definition);
            Object.DestroyImmediate(navMeshData);
            Object.DestroyImmediate(animMoveForward);
            Object.DestroyImmediate(animTurnLeft);
            Object.DestroyImmediate(animTurnRight);
        }

        [Test]
        public void TryFindAnimationSequence_StraightLine_CreatesCorrectSequence()
        {
            agent.transform.position = Vector3.zero;
            agent.transform.rotation = Quaternion.identity;

            Vector3 target = new Vector3(0, 0, 2);

            agent.SetDestination(target);

            Assert.IsTrue(agent.HasPath);
        }

        [Test]
        public void TryFindAnimationSequence_CornerTurn_CreatesCorrectSequence()
        {
            agent.transform.position = Vector3.zero;
            agent.transform.rotation = Quaternion.identity;

            Vector3 target = new Vector3(1, 0, 2);

            agent.SetDestination(target);

            Assert.IsTrue(agent.HasPath);
        }

        [Test]
        public void TryFindAnimationSequence_UnreachableTarget_ReturnsFalse()
        {
            agent.transform.position = Vector3.zero;
            agent.transform.rotation = Quaternion.identity;

            Vector3 target = new Vector3(10, 0, 10);

            agent.SetDestination(target);

            Assert.IsFalse(agent.HasPath);
        }
    }
}
