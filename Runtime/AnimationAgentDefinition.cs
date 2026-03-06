using UnityEngine;

namespace YanickSenn.Navigation
{
    [CreateAssetMenu(menuName = "Navigation/Animation Agent Definition", fileName = "New Animation Agent Definition")]
    public class AnimationAgentDefinition : NavAgentDefinition
    {
        [Tooltip("The available animations this agent can use to construct paths.")]
        public AgentAnimationDefinition[] availableAnimations;

        [Tooltip("How close the agent needs to get to the destination to consider it reached. Since animations are discrete, an exact match might not be possible.")]
        [Min(0.01f)]
        public float targetReachThreshold = 0.5f;

        public override NavAgentStrategy CreateStrategy(NavMeshAgent agent)
        {
            return new AnimationAgentStrategy(agent, this);
        }
    }
}
