# Fix Confused Agent Implementation Plan

## Objective
The agent is moving around helplessly because it frequently misses the exact `targetPosition` due to floating point offsets or curving. The current logic checks if the agent "passes" a target using `Vector3.Dot(target - currentPos, target - nextPos) <= 0f`. This formula only yields `<= 0` if the agent passes *exactly* through the infinitesimally small center point of the target. If the agent is offset by even a fraction of a unit, the dot product remains positive, meaning the agent never registers reaching the final target, nor does its collision avoidance simulation realize it reached the end. 
This causes the agent to fly past the destination, fail safety checks, and orbit confusedly.

## Proposed Changes
1. **Fix Plane Crossing Logic (`Update`)**
   - We must calculate the "forward plane" of the final waypoint. This plane's normal is the direction of the final path segment.
   - We check if the agent passes the target by checking if the dot product of `(targetPosition - nextPos)` and the `finalSegmentDir` becomes `< 0`. This properly detects crossing the finish line like a ribbon, regardless of horizontal offset.

2. **Fix Projection Safety Logic (`GetSafeSteps`)**
   - The exact same broken dot-product logic exists inside the `GetSafeSteps` loop. We must update it to use the `finalSegmentDir` plane-crossing check.
   - If the simulation crosses the final finish line, the rest of the simulated frames are automatically marked safe.

## Task Checklist
- [x] **Task 1: Fix `Update` Final Target Snap**
  - Implement plane-crossing check using `finalSegmentDir`.
- [x] **Task 2: Fix `GetSafeSteps` Target Crossing**
  - Update the dot product check in `GetSafeSteps` to use `finalSegmentDir`.