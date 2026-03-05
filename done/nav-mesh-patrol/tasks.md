# Nav Mesh Patrol Implementation

## Goal
Implement a `NavMeshPatrol` MonoBehaviour that uses a `NavMeshAgent` to cycle through a list of points indefinitely.

## Implementation Tasks
- [x] **Task 1:** Create `NavMeshPatrol.cs` with a list of patrol points and cycling logic.
- [x] **Task 2:** Add configuration options for the patrol behavior (e.g., waiting time at each point).

## Validation
- The script should correctly transition to the next point in the list when the agent reaches the current one.
- The list should loop from the last point back to the first.
