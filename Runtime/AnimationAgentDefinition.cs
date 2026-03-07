using UnityEngine;

namespace YanickSenn.Navigation
{
    [CreateAssetMenu(menuName = "Navigation/Animation Agent Definition", fileName = "New Animation Agent Definition")]
    public class AnimationAgentDefinition : NavAgentDefinition
    {
        public AgentAnimationDefinition[] availableAnimations;
        [Min(0.01f)] public float targetReachThreshold = 0.5f;

        public override NavAgentStrategy CreateStrategy(NavMeshAgent agent)
        {
            return new AnimationAgentStrategy(agent, this);
        }
    }
}
