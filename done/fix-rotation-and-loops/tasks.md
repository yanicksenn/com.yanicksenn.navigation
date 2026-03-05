# Fix Rotation and Loops Implementation Plan

## Objective
Address the erratic "looping" behavior near targets and correct the agent's rotation logic so it can truly rotate freely in 3D space without unnaturally attempting to align its roll axis to the world "Up" direction.

## Root Cause Analysis
1. **Looping/Erratic Simulation (`ToAngleAxis` bug):** 
   Unity's `Quaternion.ToAngleAxis` returns an angle in the range `[0, 360]`. If the delta rotation is slightly negative (e.g., `-1` degree), it returns `359` degrees. When we divide this by `deltaTime`, the strategy thinks the agent is spinning at hundreds of degrees per second. This causes the projection (`GetSafeSteps`) to simulate the agent spinning wildly out of bounds, failing the safety check, and triggering the 3D cone evasion sweep which steers the agent away from the target into an actual loop!
   *Fix:* We must normalize the output of `ToAngleAxis` so `angle > 180` becomes `360 - angle` and the axis is inverted, giving the shortest rotational path.

2. **Forced Roll (Preferred Axis bug):**
   `Quaternion.LookRotation(desiredDirection)` implicitly uses `Vector3.up` as its secondary alignment vector. This forces the agent to constantly roll itself upright relative to the world, resulting in a "preferred axis" of rotation and preventing true, free 3D orientation.
   *Fix:* Replace `LookRotation` with `Quaternion.FromToRotation(forward, desiredDirection) * currentRotation`. This applies the minimal required rotation (pitch and yaw relative to the turn) to point at the target, preserving the agent's current roll organically.

3. **Final Target Overshoot (Missing the sphere):**
   The final snapping logic checks if `distToTarget <= speed * deltaTime`. If the agent is slightly misaligned and its frame-step jumps past the target without landing inside that exact distance sphere, it flies past the target, misses the final snap, and turns around to try again.
   *Fix:* Enhance the final waypoint check to see if the target point has been passed during this frame's movement vector (e.g., using a dot product check against the previous and next frame positions).

## Task Checklist
- [x] **Task 1: Fix Quaternion Shortest Path**
  - Update `ToAngleAxis` extractions in `Update()` to correctly normalize angles `> 180` to their shortest path equivalents.
- [x] **Task 2: Fix Forced Roll**
  - Replace `Quaternion.LookRotation` with `Quaternion.FromToRotation` to achieve true 3D free rotation.
- [x] **Task 3: Robust Final Waypoint Snapping**
  - Update the `isFinalWaypoint` logic to snap the agent if it physically passes the target plane during the current frame, ensuring it never flies past.
- [x] **Task 4: Testing**
  - Add a unit test to ensure an agent that starts slightly misaligned successfully snaps to the final target without entering a loop.