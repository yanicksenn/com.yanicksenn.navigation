using UnityEngine;

namespace YanickSenn.Navigation
{
    [CreateAssetMenu(fileName = "New Constrained Rotation Agent Definition", menuName = "Navigation/Constrained Rotation Agent Definition")]
    public class ConstrainedRotationAgentDefinition : NavAgentDefinition
    {
        public float slowDownAngleThreshold = 90;
        public float minSpeed = 20;
        public float maxRotationSpeed = 180;
        public float lookaheadDistance = 2f;
        public float projectionStepTime = 0.1f;
        public int projectionSteps = 10;

        public override NavAgentStrategy CreateStrategy(NavMeshAgent agent) {
            return new ConstrainedRotationAgentStrategy(agent, this);
        }
    }
}
