using UnityEngine;

namespace YanickSenn.Navigation
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class NavMeshPatrol : MonoBehaviour
    {
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private float waitTime = 1f;
        
        private NavMeshAgent agent;
        private int currentPointIndex = -1;
        private float waitTimer = 0f;
        private bool isWaiting = false;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        private void Start()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                Debug.LogWarning("NavMeshPatrol: No patrol points assigned.", this);
                return;
            }

            MoveToNextPoint();
        }

        private void Update()
        {
            if (patrolPoints == null || patrolPoints.Length == 0) return;

            if (isWaiting)
            {
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0)
                {
                    isWaiting = false;
                    MoveToNextPoint();
                }
            }
            else if (!agent.HasPath)
            {
                isWaiting = true;
                waitTimer = waitTime;
            }
        }

        private void MoveToNextPoint()
        {
            currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
            Transform nextPoint = patrolPoints[currentPointIndex];
            
            if (nextPoint != null)
            {
                agent.SetDestination(nextPoint.position);
            }
        }
    }
}
