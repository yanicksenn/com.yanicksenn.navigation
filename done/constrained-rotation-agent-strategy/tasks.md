# Constrained Rotation Agent Strategy

## Objective
Implement a `ConstrainedRotationAgentStrategy` where the agent navigates along a path using constrained rotation (yaw, pitch, roll). Crucially, the agent must stay entirely within the `WalkableCubes` of the NavMesh. To avoid getting stuck on corners where its turning radius is too large, it must predict its trajectory and potentially take a wider (greater) path.

## Implementation Plan

### 1. Class Structure
- Create `ConstrainedRotationAgentStrategy.cs` in `Runtime/`.
- Inherit from `NavAgentStrategy`.
- Store references to `NavMeshAgent` and `ConstrainedRotationAgentDefinition`.

### 2. Pathfinding
- When `SetDestination(target)` is called, use `NavMeshPathfinder.TryFindPath` to compute `Vector3[]` waypoints and `Bounds[]` walkable cubes.
- Store the path and maintain a `currentWaypointIndex`.

### 3. Trajectory Prediction (Local Rollout / Dynamic Window Approach)
Since the agent has a maximum rotation rate and a constant speed, it cannot make infinitely sharp turns. Steering directly toward the next waypoint might cause it to overshoot and leave the NavMesh.
- **Generate Candidate Steering Inputs:** Every frame, generate a set of candidate rotation inputs (e.g., max left, max right, straight, max pitch up/down, and combinations).
- **Simulate Trajectories:** For each candidate input, simulate the agent's movement forward for `lookaheadDistance`. The simulation uses the agent's `speed` and the candidate rotation constrained by `maxRotationRate`.
- **Bounds Checking:** During simulation, sample points along each trajectory and check if they are contained within `CurrentPathCubes` (or any walkable cube). Discard any trajectory that exits the NavMesh.
- **Evaluate and Select:** Score the surviving trajectories based on their alignment with the path (e.g., closest to the target waypoint or lookahead point on the path).
- **Apply Best Steering:** Apply the rotation from the highest-scoring trajectory to the agent.

### 4. Rotation Constraints
- Use the definition's `maxRotationRate` (Vector3 representing Pitch, Yaw, Roll degrees per second).
- Convert the desired steering direction into Euler angle deltas.
- Clamp the deltas to `maxRotationRate * deltaTime`.
- Apply the clamped rotation to the agent's transform.

### 5. Movement Execution
- Update the agent's position by moving it forward along its new local forward vector at `agent.speed * deltaTime`.
- Advance the `currentWaypointIndex` when the agent is close enough to the current target waypoint.
- Stop when the final waypoint is reached.

### 6. Edge Case Handling
- **Dead Ends/No Valid Trajectory:** If all simulated trajectories exit the NavMesh (e.g., stuck in a corner), fall back to stopping or rotating in place (if speed can be decoupled, but standard behavior usually implies constant movement. We may need to clamp speed if rotation requires it, but we will assume constant speed unless restricted).
- **Cube Contains Point Check:** Implement an efficient helper to check if a `Vector3` point is inside any of the `Bounds` of the `CurrentPathCubes`.

## Checklist
- [x] Create `ConstrainedRotationAgentStrategy.cs`.
- [x] Implement `NavAgentStrategy` interface.
- [x] Add path following state logic (waypoint indexing).
- [x] Implement Trajectory Prediction (Rollout) logic.
- [x] Implement constrained Euler rotation clamping.
- [x] Implement bounds validation against `CurrentPathCubes`.
- [x] Apply movement and visualize predicted trajectories via Gizmos (for debugging).
- [x] Verify functionality via Editor / Play mode testing.
