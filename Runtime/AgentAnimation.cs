using System;
using UnityEngine;

namespace YanickSenn.Navigation
{
    public abstract class AgentAnimation
    {
        public bool IsPlaying { get; protected set; }

        public Action OnComplete;

        public abstract void Play(NavMeshAgent agent);

        public abstract void Stop();

        protected void Complete()
        {
            if (IsPlaying)
            {
                IsPlaying = false;
                OnComplete?.Invoke();
            }
        }
    }
}
