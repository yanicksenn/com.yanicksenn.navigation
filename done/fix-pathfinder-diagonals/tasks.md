# Fix Pathfinder Diagonal Nodes

## Goal
Fix `TryFindPath_ObstacleAvoidance_FindsLPath` unit test failure caused by the pathfinder allowing diagonal movement across cube corners.

## Context
When expanding bounds by `0.01f` to find neighbors, cubes that touch only at a corner (0 dimensions overlapping) or an edge (1 dimension overlapping) are registered as intersecting. This allows the agent to "hop" diagonally through walls or empty space, taking shortcuts that shouldn't exist.

## Implementation Tasks
- [x] **Task 1:** Update `NavMeshPathfinder.cs` neighbor detection to explicitly calculate intersection bounds and ensure they overlap on at least two dimensions (forming a valid face portal).