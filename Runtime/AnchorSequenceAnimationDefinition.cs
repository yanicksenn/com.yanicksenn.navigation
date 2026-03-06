using System;
using UnityEngine;

namespace YanickSenn.Navigation
{
    [Serializable]
    public struct TweenAnimationAnchor
    {
        [Tooltip("The position offset relative to the start of the animation.")]
        public Vector3 localPosition;

        [Tooltip("The forward direction at this anchor, relative to the start rotation.")]
        public Vector3 localForward;

        [Tooltip("The time (in seconds) it takes to reach this anchor from the previous one.")]
        [Min(0f)]
        public float duration;
    }

    [CreateAssetMenu(menuName = "Navigation/Anchor Sequence Animation Definition", fileName = "New Anchor Sequence Animation")]
    public class AnchorSequenceAnimationDefinition : AgentAnimationDefinition
    {
        [Tooltip("The sequence of points that make up this movement animation.")]
        public TweenAnimationAnchor[] anchors;

        public override Vector3 GetEndPosition(Vector3 startPosition, Quaternion startRotation)
        {
            if (anchors == null || anchors.Length == 0) return startPosition;
            return startPosition + startRotation * anchors[anchors.Length - 1].localPosition;
        }

        public override Quaternion GetEndRotation(Quaternion startRotation)
        {
            if (anchors == null || anchors.Length == 0) return startRotation;
            Vector3 finalLocalForward = anchors[anchors.Length - 1].localForward;
            if (finalLocalForward.sqrMagnitude < 0.001f) return startRotation;
            return startRotation * Quaternion.LookRotation(finalLocalForward.normalized, Vector3.up);
        }

        public override AgentAnimation CreateAnimation(Vector3 startPosition, Quaternion startRotation)
        {
            return new AnchorSequenceAnimation(anchors, startPosition, startRotation);
        }

        public override bool CheckValidity(NavMeshAgent agent, Vector3 startPos, Quaternion startRot, Bounds[] pathCubes)
        {
            if (anchors == null || anchors.Length == 0) return true;

            Vector3 worldPos = startPos;
            Quaternion worldRot = startRot;

            foreach (var anchor in anchors)
            {
                worldPos += worldRot * anchor.localPosition;
                worldRot = worldRot * Quaternion.LookRotation(anchor.localForward.normalized, Vector3.up);

                bool pointValid = false;
                foreach (var bounds in pathCubes)
                {
                    if (bounds.Contains(worldPos))
                    {
                        pointValid = true;
                        break;
                    }
                    
                    // Small tolerance
                    Bounds expanded = bounds;
                    expanded.Expand(0.1f);
                    if (expanded.Contains(worldPos))
                    {
                        pointValid = true;
                        break;
                    }
                }

                if (!pointValid) return false;
            }

            return true;
        }

        private void OnDrawGizmosSelected()
        {
            if (anchors == null || anchors.Length == 0) return;

            Vector3 currentPos = Vector3.zero;

            Gizmos.color = Color.cyan;

            for (int i = 0; i < anchors.Length; i++)
            {
                Vector3 nextPos = anchors[i].localPosition;
                Vector3 nextForward = anchors[i].localForward;
                
                Gizmos.DrawLine(currentPos, nextPos);
                
                if (nextForward.sqrMagnitude > 0.001f)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawRay(nextPos, nextForward.normalized * 0.2f);
                    Gizmos.color = Color.cyan;
                }

                currentPos = nextPos;
            }
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(currentPos, 0.05f);
        }
    }
}
