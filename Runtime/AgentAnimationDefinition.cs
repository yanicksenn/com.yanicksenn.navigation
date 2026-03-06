using UnityEngine;

namespace YanickSenn.Navigation
{
    public abstract class AgentAnimationDefinition : ScriptableObject
    {
        /// <summary>
        /// Evaluates the end position of the animation given a starting pose.
        /// </summary>
        public abstract Vector3 GetEndPosition(Vector3 startPosition, Quaternion startRotation);

        /// <summary>
        /// Evaluates the end rotation of the animation given a starting pose.
        /// </summary>
        public abstract Quaternion GetEndRotation(Quaternion startRotation);

        /// <summary>
        /// Instantiates a new animation sequence that can be played on an agent.
        /// </summary>
        public abstract AgentAnimation CreateAnimation(Vector3 startPosition, Quaternion startRotation);

        /// <summary>
        /// Checks if this animation footprint stays within the provided walkable bounds.
        /// </summary>
        public abstract bool CheckValidity(NavMeshAgent agent, Vector3 startPos, Quaternion startRot, Bounds[] pathCubes);
    }
}
