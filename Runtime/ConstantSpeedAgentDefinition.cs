using UnityEngine;

namespace YanickSenn.Navigation
{
    [CreateAssetMenu(fileName = "ConstantSpeedAgentDefinition", menuName = "Navigation/Constant Speed Agent Definition")]
    public class ConstantSpeedAgentDefinition : NavAgentDefinition
    {
        public float speed = 5f;
        public float forwardAlignmentSpeed = 10f;
        public float upAlignmentSpeed = 5f;

        public override NavAgentStrategy CreateStrategy(NavMeshAgent agent)
        {
            return new ConstantSpeedAgentStrategy(agent, this);
        }
    }
}
