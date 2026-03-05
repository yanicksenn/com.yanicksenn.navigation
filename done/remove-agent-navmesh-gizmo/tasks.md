# Remove NavMeshAgent Gizmo

## Goal
Ensure walkable cubes are not shown when a `NavMeshAgent` is selected. They should only be visible when the `NavMeshData` asset is selected.

## Implementation Tasks
- [x] **Task 1:** Delete `Editor/NavMeshAgentGizmos.cs` which currently draws the `NavMeshData` when the agent is selected.
