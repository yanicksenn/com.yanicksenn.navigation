# Nav Agent Pathfinding Implementation

## Goal
Make the `ConstantSpeedAgentStrategy` base its movement on the generated nav mesh walkable cubes using pathfinding (A*). The agent should dynamically choose the most efficient route across the nav mesh nodes rather than moving blindly through unwalkable space.

## Scope
1. **NavMeshPathfinder:** Implement an A* pathfinding algorithm over the `NavMeshData.WalkableCubes`.
2. **Strategy Integration:** Update `ConstantSpeedAgentStrategy` to use the pathfinder to calculate multi-waypoint paths and traverse them gracefully frame-by-frame.
3. **Testing:** Update unit tests with mocked `NavMeshData` to ensure movement still succeeds and waypoints are processed accurately.

## Implementation Tasks
- [x] **Task 1:** Create `NavMeshPathfinder.cs` utility class with `TryFindPath` (A*) implementation.
- [x] **Task 2:** Update `ConstantSpeedAgentStrategy.cs` to query the pathfinder and smoothly interpolate across multiple waypoints using delta time.
- [x] **Task 3:** Update `ConstantSpeedAgentStrategyTests.cs` to mock `navMeshData` so pathfinding passes and movement works.
