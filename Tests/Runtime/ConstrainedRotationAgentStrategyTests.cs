using NUnit.Framework;
using UnityEngine;

namespace YanickSenn.Navigation.Tests
{
    public class ConstrainedRotationAgentStrategyTests
    {
        private GameObject agentGO;
        private NavMeshAgent agent;
        private ConstrainedRotationAgentDefinition definition;
        private ConstrainedRotationAgentStrategy strategy;
        private NavMeshData navMeshData;

        [SetUp]
        public void Setup()
        {
            agentGO = new GameObject("Agent");
            agent = agentGO.AddComponent<NavMeshAgent>();
            
            navMeshData = ScriptableObject.CreateInstance<NavMeshData>();
            
            // Create a simple corridor setup
            navMeshData.WalkableCubes = new Bounds[] { 
                new Bounds(new Vector3(5, 0, 0), new Vector3(10, 2, 2)), // Start path going +X
                new Bounds(new Vector3(10, 0, 5), new Vector3(2, 2, 10))  // Turn corner to +Z
            };
            agent.navMeshData = navMeshData;

            definition = ScriptableObject.CreateInstance<ConstrainedRotationAgentDefinition>();
            definition.speed = 2f;
            definition.maxRotationSpeed = 90f;
            definition.lookaheadDistance = 1f;

            strategy = (ConstrainedRotationAgentStrategy)definition.CreateStrategy(agent);
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
            strategy.SetDestination(new Vector3(10, 0, 8));
            Assert.IsTrue(strategy.HasPath);
        }

        [Test]
        public void Stop_SetsHasPathToFalse()
        {
            strategy.SetDestination(new Vector3(10, 0, 8));
            strategy.Stop();
            Assert.IsFalse(strategy.HasPath);
        }

        [Test]
        public void Update_ConstrainsRotationToMaxRotationSpeed()
        {
            agentGO.transform.position = new Vector3(2, 0, 0);
            agentGO.transform.rotation = Quaternion.LookRotation(Vector3.right); // Facing +X
            
            // Destination is behind to force a hard turn
            strategy.SetDestination(new Vector3(-5, 0, 0)); 

            // Allow moving in a large unconstrained area for this specific test
            navMeshData.WalkableCubes = new Bounds[] { new Bounds(Vector3.zero, new Vector3(200, 200, 200)) };
            strategy.SetDestination(new Vector3(-5, 0, 0)); 

            float deltaTime = 0.5f;
            strategy.Update(deltaTime);

            // Maximum rotation per frame is 90 degrees/s * 0.5s = 45 degrees
            float expectedAngle = 45f;
            float actualAngle = Vector3.Angle(Vector3.right, agentGO.transform.forward);
            
            // Check that it didn't snap immediately to face the target
            Assert.AreEqual(expectedAngle, actualAngle, 1f, "Agent turned faster than maxRotationSpeed.");
            
            // Ensure agent moved forward relative to its NEW rotation, not just snapped
            Assert.IsTrue(agentGO.transform.position.magnitude > 0);
        }

        [Test]
        public void Update_ConstrainsRotationToMaxRotationSpeed_3D_Pitch()
        {
            agentGO.transform.position = new Vector3(2, 0, 0);
            agentGO.transform.rotation = Quaternion.LookRotation(Vector3.forward); // Facing +Z
            
            // Destination is straight up to force a pitch change
            strategy.SetDestination(new Vector3(2, 10, 0)); 

            navMeshData.WalkableCubes = new Bounds[] { new Bounds(new Vector3(2, 5, 0), new Vector3(10, 20, 10)) };
            strategy.SetDestination(new Vector3(2, 10, 0)); 

            float deltaTime = 0.5f;
            strategy.Update(deltaTime);

            float expectedAngle = 45f;
            float actualAngle = Vector3.Angle(Vector3.forward, agentGO.transform.forward);
            
            Assert.AreEqual(expectedAngle, actualAngle, 1f, "Agent pitched faster than maxRotationSpeed.");
            
            // Should be pitching UP towards +Y
            Assert.IsTrue(agentGO.transform.forward.y > 0);
        }

        [Test]
        public void Update_AdjustsToStayInCorridor()
        {
            // Position near the corner of the two cubes
            agentGO.transform.position = new Vector3(8, 0, 0);
            agentGO.transform.rotation = Quaternion.LookRotation(Vector3.right); // Facing +X
            
            // Target is down the other corridor (+Z)
            strategy.SetDestination(new Vector3(10, 0, 8));
            
            // Advance many frames to simulate the turn
            for (int i = 0; i < 20; i++)
            {
                strategy.Update(0.1f);
                
                // Assert that at every frame, the agent is within at least one walkable cube
                bool isInBounds = false;
                foreach (Bounds cube in navMeshData.WalkableCubes)
                {
                    Bounds expanded = cube;
                    expanded.Expand(0.1f); // tiny margin for float inaccuracy
                    if (expanded.Contains(agentGO.transform.position))
                    {
                        isInBounds = true;
                        break;
                    }
                }
                Assert.IsTrue(isInBounds, $"Agent stepped out of bounds at position {agentGO.transform.position}");
            }
        }
        [Test]
        public void Update_SnapsToFinalTargetWithoutLooping()
        {
            // Position the agent slightly offset from a perfect line to the target
            agentGO.transform.position = new Vector3(0.5f, 0, 0);
            agentGO.transform.rotation = Quaternion.LookRotation(Vector3.forward); // Facing +Z
            
            // Destination is straight ahead, but agent is offset by 0.5f in X
            strategy.SetDestination(new Vector3(0, 0, 10)); 

            navMeshData.WalkableCubes = new Bounds[] { new Bounds(new Vector3(0, 0, 5), new Vector3(10, 10, 20)) };
            strategy.SetDestination(new Vector3(0, 0, 10)); 

            float deltaTime = 0.1f; // 10 frames per second
            
            // Maximum time we allow to reach a target 10 units away at speed 2
            // Expected time ~5 seconds. Give it 10 seconds (100 frames) maximum to prevent infinite loop.
            int maxFrames = 100;
            int frames = 0;
            
            while (strategy.HasPath && frames < maxFrames)
            {
                strategy.Update(deltaTime);
                frames++;
            }
            
            Assert.IsFalse(strategy.HasPath, "Agent entered a loop and never reached the final target.");
            
            // Check precision snapping
            Assert.AreEqual(0f, agentGO.transform.position.x, 0.001f);
            Assert.AreEqual(0f, agentGO.transform.position.y, 0.001f);
            Assert.AreEqual(10f, agentGO.transform.position.z, 0.001f);
        }
    }
}
