using UnityEngine;

namespace YanickSenn.Navigation {
    public abstract class NavAgentStrategy {
        protected NavMeshAgent agent;

        public NavAgentStrategy(NavMeshAgent agent) {
            this.agent = agent;
        }

        public abstract void SetDestination(Vector3 target);
        public abstract void Stop();
        public abstract void Update(float deltaTime);

        public abstract bool HasPath { get; }
        public abstract Vector3[] CurrentPath { get; }
        public abstract Bounds[] CurrentPathCubes { get; }

        public virtual void DrawGizmos() {
        }
    }
}
