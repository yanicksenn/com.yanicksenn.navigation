using System;
using UnityEngine;

namespace YanickSenn.Navigation {
    public class NavMeshAgent : MonoBehaviour {

        public NavMeshData navMeshData;
        public NavAgentDefinition agentDefinition;

        [Min(0)] public float speed = 5f;
        [Min(0)] public float forwardAlignmentSpeed = 5f;
        [Min(0)] public float upAlignmentSpeed = 5f;

        private NavAgentStrategy _strategy;

        private void Awake() {
            if (agentDefinition != null) {
                _strategy = agentDefinition.CreateStrategy(this);
            }
        }

        private void Update() {
            if (_strategy != null) {
                _strategy.Update(Time.deltaTime);
            }
        }

        public void SetDestination(NavTarget target) {
            IsStopped = false;
            _strategy?.SetDestination(target);
        }

        public void Stop() {
            IsStopped = true;
            _strategy?.Stop();
        }

        public async Awaitable<bool> WaitForCompletionAsync(System.Threading.CancellationToken cancellationToken = default) {
            if (!HasPath) {
                return !IsStopped;
            }

            try {
                while (HasPath) {
                    if (cancellationToken.IsCancellationRequested) {
                        Stop();
                        return false;
                    }
                    await Awaitable.NextFrameAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException) {
                Stop();
                return false;
            }

            return !IsStopped;
        }

        public bool IsStopped { get; set; }
        public bool HasPath => _strategy?.HasPath ?? false;
        public Vector3[] CurrentPath => _strategy?.CurrentPath ?? Array.Empty<Vector3>();
        public Bounds[] CurrentPathCubes => _strategy?.CurrentPathCubes ?? Array.Empty<Bounds>();

        private void OnDrawGizmos() {
            _strategy?.DrawGizmos();

            if (!HasPath) return;
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f); // Cyan semi-transparent for path cubes
            foreach (var cube in CurrentPathCubes) {
                Gizmos.DrawWireCube(cube.center, cube.size);
            }

            var path = CurrentPath;
            if (path != null && path.Length > 0) {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(path[^1], 0.2f);
            }
        }
    }
}
