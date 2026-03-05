using UnityEngine;

namespace YanickSenn.Navigation
{
    [CreateAssetMenu(fileName = "New Constrained Rotation Agent Definition", menuName = "Navigation/Constrained Rotation Agent Definition")]
    public class ConstrainedRotationAgentDefinition : NavAgentDefinition
    {
        [Tooltip("The constant forward speed in units per second.")]
        [Min(0)]
        public float speed = 3f;

        [Tooltip("The maximum rotation speed in degrees per second.")]
        [Min(0)]
        public float maxRotationSpeed = 120f;

        [Tooltip("The speed at which the agent's forward vector slerps towards the current movement speed vector.")]
        [Min(0)]
        public float forwardAlignmentSpeed = 5f;

        [Tooltip("The speed at which the agent's up vector slerps towards world up.")]
        [Min(0)]
        public float upAlignmentSpeed = 5f;

        [Tooltip("The lookahead distance to determine the steering target along the path.")]
        [Min(0.1f)]
        public float lookaheadDistance = 1.0f;

        [Tooltip("The number of steps to project forward to check for corridor collisions.")]
        [Min(1)]
        public int projectionSteps = 10;

        [Tooltip("The time step (in seconds) for each projection step.")]
        [Min(0.01f)]
        public float projectionStepTime = 0.1f;

        public override NavAgentStrategy CreateStrategy(NavMeshAgent agent)
        {
            return new ConstrainedRotationAgentStrategy(agent, this);
        }
    }
}
