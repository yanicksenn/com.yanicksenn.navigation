using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

namespace YanickSenn.Navigation
{
    [CreateAssetMenu(menuName = "Navigation/Spline Animation Definition", fileName = "New Spline Sequence")]
    public class SplineAnimationDefinition : AgentAnimationDefinition
    {
        [Tooltip("The spline defining the animation path. It is defined in local space relative to the start position and rotation.")]
        public Spline spline = new Spline();

        [Tooltip("The time (in seconds) it takes to traverse the spline.")]
        [Min(0f)]
        public float duration = 1f;

        public override Vector3 GetEndPosition(Vector3 startPosition, Quaternion startRotation)
        {
            if (spline == null || spline.Count == 0) return startPosition;
            
            float3 localEndPos = spline.EvaluatePosition(1f);
            return startPosition + startRotation * new Vector3(localEndPos.x, localEndPos.y, localEndPos.z);
        }

        public override Quaternion GetEndRotation(Quaternion startRotation)
        {
            if (spline == null || spline.Count == 0) return startRotation;
            
            float3 tangent = spline.EvaluateTangent(1f);
            Vector3 tangentV3 = new Vector3(tangent.x, tangent.y, tangent.z);
            
            if (tangentV3.sqrMagnitude < 0.001f) return startRotation;
            return startRotation * Quaternion.LookRotation(tangentV3.normalized, Vector3.up);
        }

        public override AgentAnimation CreateAnimation(Vector3 startPosition, Quaternion startRotation)
        {
            return new SplineAgentAnimation(spline, duration, startPosition, startRotation);
        }

        public override bool CheckValidity(NavMeshAgent agent, Vector3 startPos, Quaternion startRot, Bounds[] pathCubes)
        {
            if (spline == null || spline.Count == 0) return true;

            int sampleCount = Mathf.Max(10, Mathf.CeilToInt(spline.GetLength() * 10f)); // Sample roughly every 0.1 units

            for (int i = 0; i <= sampleCount; i++)
            {
                float t = i / (float)sampleCount;
                float3 localPos = spline.EvaluatePosition(t);
                Vector3 worldPos = startPos + startRot * new Vector3(localPos.x, localPos.y, localPos.z);

                bool pointValid = false;
                foreach (var bounds in pathCubes)
                {
                    if (bounds.Contains(worldPos))
                    {
                        pointValid = true;
                        break;
                    }
                    
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
    }
}
