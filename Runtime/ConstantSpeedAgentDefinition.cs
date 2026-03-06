using UnityEngine;

namespace YanickSenn.Navigation
{
    [CreateAssetMenu(fileName = "ConstantSpeedAgentDefinition", menuName = "Navigation/Constant Speed Agent Definition")]
    public class ConstantSpeedAgentDefinition : NavAgentDefinition
    {
        public float speed = 5f;
        public float forwardAlignmentSpeed = 10f;
        public float upAlignmentSpeed = 5f;

        [Range(0f, 0.5f)]
        public float pathSmoothingFactor = 0f;
        [Min(0)]
        public int pathSmoothingIterations = 2;

        public override NavAgentStrategy CreateStrategy(NavMeshAgent agent)
        {
            return new ConstantSpeedAgentStrategy(agent, this);
        }
    }
}
