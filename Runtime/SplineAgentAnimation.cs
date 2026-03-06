using PrimeTween;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace YanickSenn.Navigation
{
    public class SplineAgentAnimation : AgentAnimation
    {
        private readonly Spline spline;
        private readonly float duration;
        private readonly Vector3 startPos;
        private readonly Quaternion startRot;
        
        private Tween currentTween;
        private NavMeshAgent currentAgent;

        public SplineAgentAnimation(Spline spline, float duration, Vector3 startPos, Quaternion startRot)
        {
            this.spline = spline;
            this.duration = duration;
            this.startPos = startPos;
            this.startRot = startRot;
        }

        public override void Play(NavMeshAgent agent)
        {
            currentAgent = agent;
            IsPlaying = true;

            currentTween = Tween.Custom(this, 0f, 1f, duration, onValueChange: (target, t) => target.UpdateTransform(t))
                .OnComplete(() =>
                {
                    Complete();
                });
        }

        public override void Stop()
        {
            if (currentTween.isAlive)
            {
                currentTween.Stop();
            }
            IsPlaying = false;
        }

        private void UpdateTransform(float t)
        {
            if (currentAgent == null) return;

            float3 localPos = spline.EvaluatePosition(t);
            float3 tangent = spline.EvaluateTangent(t);
            
            Vector3 localPosV3 = new Vector3(localPos.x, localPos.y, localPos.z);
            Vector3 worldPos = startPos + startRot * localPosV3;
            currentAgent.transform.position = worldPos;

            Vector3 tangentV3 = new Vector3(tangent.x, tangent.y, tangent.z);
            if (tangentV3.sqrMagnitude > 0.001f)
            {
                Quaternion worldRot = startRot * Quaternion.LookRotation(tangentV3.normalized, Vector3.up);
                currentAgent.transform.rotation = worldRot;
            }
        }
    }
}
