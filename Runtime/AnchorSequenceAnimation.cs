using PrimeTween;
using UnityEngine;

namespace YanickSenn.Navigation
{
    public class AnchorSequenceAnimation : AgentAnimation
    {
        private readonly TweenAnimationAnchor[] anchors;
        private readonly Vector3 startPos;
        private readonly Quaternion startRot;
        
        private Sequence sequence;
        
        // Caching for stopping logic if needed
        private NavMeshAgent currentAgent;

        public AnchorSequenceAnimation(TweenAnimationAnchor[] anchors, Vector3 startPos, Quaternion startRot)
        {
            this.anchors = anchors;
            this.startPos = startPos;
            this.startRot = startRot;
        }

        public override void Play(NavMeshAgent agent)
        {
            currentAgent = agent;
            IsPlaying = true;
            
            sequence = Sequence.Create();
            
            Vector3 worldPos = startPos;
            Quaternion worldRot = startRot;

            foreach (var anchor in anchors)
            {
                Vector3 targetPos = worldPos + worldRot * anchor.localPosition;
                Quaternion targetRot = worldRot * Quaternion.LookRotation(anchor.localForward.normalized, Vector3.up);

                var tweenPos = Tween.Position(agent.transform, targetPos, anchor.duration, Ease.Linear);
                var tweenRot = Tween.Rotation(agent.transform, targetRot, anchor.duration, Ease.Linear);

                sequence.Chain(tweenPos);
                sequence.Group(tweenRot);
                
                worldPos = targetPos;
                worldRot = targetRot;
            }

            sequence.OnComplete(() =>
            {
                Complete();
            });
        }

        public override void Stop()
        {
            if (sequence.isAlive)
            {
                sequence.Stop();
            }
            IsPlaying = false;
        }
    }
}
