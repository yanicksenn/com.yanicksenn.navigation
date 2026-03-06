using NUnit.Framework;
using UnityEngine;

namespace YanickSenn.Navigation.Tests
{
    public class ConstantSpeedAgentStrategyTests
    {
        private GameObject agentGO;
        private NavMeshAgent agent;
        private ConstantSpeedAgentDefinition definition;
        private ConstantSpeedAgentStrategy strategy;
        private NavMeshData navMeshData;

        [SetUp]
        public void Setup()
        {
            agentGO = new GameObject("Agent");
            agent = agentGO.AddComponent<NavMeshAgent>();

            navMeshData = ScriptableObject.CreateInstance<NavMeshData>();
            // Create a giant walkable cube that covers everything from -100 to 100
            // This ensures TryFindPath will immediately succeed for simple tests
            navMeshData.WalkableCubes = new Bounds[] { new Bounds(Vector3.zero, new Vector3(200, 200, 200)) };
            agent.navMeshData = navMeshData;

            definition = ScriptableObject.CreateInstance<ConstantSpeedAgentDefinition>();
            definition.speed = 2f;

            strategy = (ConstantSpeedAgentStrategy)definition.CreateStrategy(agent);
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(agentGO);
            Object.DestroyImmediate(definition);
            Object.DestroyImmediate(navMeshData);
        }

        [Test]
        public void SetDestination_SetsHasPathToTrue()
        {
            Assert.IsFalse(strategy.HasPath);
            strategy.SetDestination(new Vector3(10, 0, 0));
            Assert.IsTrue(strategy.HasPath);
        }

        [Test]
        public void Stop_SetsHasPathToFalse()
        {
            strategy.SetDestination(new Vector3(10, 0, 0));
            strategy.Stop();
            Assert.IsFalse(strategy.HasPath);
        }

        [Test]
        public void Update_MovesAgentTowardsTargetAtConstantSpeed()
        {
            agentGO.transform.position = Vector3.zero;
            strategy.SetDestination(new Vector3(10, 0, 0));

            strategy.Update(1f);

            Assert.AreEqual(new Vector3(2, 0, 0).x, agentGO.transform.position.x, 0.01f);
            Assert.IsTrue(strategy.HasPath);
        }

        [Test]
        public void Update_SnapsToTargetWhenCloseEnough() {
            agentGO.transform.position = Vector3.zero;
            strategy.SetDestination(new Vector3(1.5f, 0, 0));

            strategy.Update(1f);

            Assert.AreEqual(new Vector3(1.5f, 0, 0).x, agentGO.transform.position.x, 0.01f);
        }

        [Test]
        public void SetDestination_WithSmoothing_GeneratesSmoothedPath()
        {
            // By default path from (0,0,0) to (10,0,10) on a single cube is just 2 points: start and target.
            // Let's manually trigger a multi-point path to test smoothing.
            // The algorithm only smoothens paths with more than 2 points.
            // Since we can't easily mock TryFindPath which is a static method, we'll configure
            // the navmesh to force a 3-point path by creating 3 discrete cubes.

            navMeshData.WalkableCubes = new Bounds[] {
                new Bounds(new Vector3(0, 0, 0), new Vector3(2, 2, 2)),
                new Bounds(new Vector3(2, 0, 0), new Vector3(2, 2, 2)),
                new Bounds(new Vector3(2, 0, 2), new Vector3(2, 2, 2))
            };

            // This setup creates an L-shape path: (0,0,0) -> (1,0,0) portal -> (2,0,1) portal -> (2,0,2) target

            definition.pathSmoothingFactor = 0.25f;
            definition.pathSmoothingIterations = 2;

            strategy.SetDestination(new Vector3(2, 0, 2));

            // Original path would be 4 points: Start(0,0,0), Portal(1,0,0), Portal(2,0,1), Target(2,0,2)
            // With 2 iterations of smoothing, it should have > 4 points.
            Assert.IsTrue(strategy.CurrentPath.Length > 4);
        }
    }
}
