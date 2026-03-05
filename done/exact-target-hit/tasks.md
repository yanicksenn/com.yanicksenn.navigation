# Exact Target Hit Implementation Plan

## Objective
Ensure the `ConstrainedRotationAgentStrategy` hits its final target position exactly. The current implementation advances or stops when the agent is "close enough" (distance < 0.5 or crosses the waypoint plane), which causes it to stop slightly before or past the exact destination point. While it is acceptable to loosely follow intermediate path segments, the final destination must be matched exactly.

## Proposed Changes
Update the waypoint advancement logic in `ConstrainedRotationAgentStrategy.Update(float deltaTime)` to differentiate between intermediate waypoints and the final destination.

1. **Final Target Logic:**
   - If the agent is on the final path segment (`currentPathIndex == path.Length - 1`), check if the distance to the target is less than or equal to the distance it would travel this frame (`definition.speed * deltaTime`).
   - If it is, snap the agent's position exactly to the `targetPosition`, set `hasPath = false`, and `return` to halt movement.

2. **Intermediate Waypoint Logic:**
   - If the agent is NOT on the final segment, retain the existing plane-crossing (`t >= 0.95f`) and distance (`< 0.5f`) checks to allow smooth, loose cornering.
   - This ensures the agent isn't forced to hit every corner exactly (which would cause sharp, unrealistic turns or orbiting), but still hits the very end of the path perfectly.

## Task Checklist
- [x] **Task 1: Update Waypoint Logic in Strategy**
  - Refactor the first section of `Update()` to check `isFinalWaypoint`.
  - Add the snapping logic for the final waypoint.
  - Move the intermediate advancement logic to an `else` block.
- [x] **Task 2: Testing**
  - Verify that the agent successfully stops precisely at the final position.