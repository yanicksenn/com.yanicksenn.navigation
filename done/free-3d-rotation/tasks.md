# Free 3D Rotation Support Implementation Plan

## Objective
Upgrade the `ConstrainedRotationAgentStrategy` to support full 3D rotation (Pitch, Yaw, and Roll). The agent is currently locked to rotating exclusively around the Y-axis, which is insufficient for a 3D NavMesh built from Octree cubes. The agent must be able to rotate freely in all directions while still strictly adhering to its `maxRotationSpeed` and maintaining its collision avoidance behavior within the `WalkableCubes`.

## Analysis of Current Implementation
- **Current Limitation:** The strategy uses `Vector3.SignedAngle(forward, desiredDirection, Vector3.up)` and `Quaternion.Euler(0, stepAngle, 0)`, explicitly locking steering to the XZ plane. The 1D "fan sweep" for collision avoidance just lerps between `-maxRotationSpeed` and `+maxRotationSpeed`.
- **3D Requirement:** To support 3D steering, we must transition from scalar angular velocities (degrees per second around Y) to 3D quaternion rotational deltas.

## Proposed Architecture Updates

### 1. 3D Rotation Calculation
Instead of extracting the Y-axis angle, we will use Unity's robust Quaternion math to calculate the clamped 3D rotation:
- Calculate the desired orientation: `Quaternion desiredRotation = Quaternion.LookRotation(desiredDirection);`
- Clamp the rotation per frame: `Quaternion stepRotation = Quaternion.RotateTowards(currentRotation, desiredRotation, maxRotationSpeed * deltaTime);`
- Extract the delta rotation applied this frame: `Quaternion deltaRotation = stepRotation * Quaternion.Inverse(currentRotation);`

### 2. Update Arc Simulation (`GetSafeSteps`)
The projection simulation must be upgraded to accept a 3D rotational delta instead of a 1D scalar angular velocity.
- The `simRot` at each step will be updated as: `simRot = deltaRotation * simRot;`
- This accurately projects the agent's curved trajectory through 3D space across all axes.

### 3. Implement a 3D "Cone Sweep" for Collision Avoidance
The 1D fan sweep (left/right) is obsolete in 3D. We need a 3D sweep to find alternative safe paths if the direct trajectory hits a wall (e.g., floor, ceiling, or a complex corner).
- **The Algorithm:** We will generate candidate rotations by rotating the agent's `forward` vector around multiple axes perpendicular to `forward` (acting like a cone of vision).
- For a given number of samples (e.g., 8 or 16), we can generate test axes by rotating the `transform.right` vector around the `transform.forward` vector by $0^\circ, 45^\circ, 90^\circ$, etc.
- We then generate a `testDeltaRotation` by rotating around these test axes by the `maxRotationSpeed * deltaTime`.
- We evaluate `GetSafeSteps` for each `testDeltaRotation` and select the one that yields the longest safe path while remaining closest to the original `desiredDirection`.

## Task Checklist

- [x] **Task 1: Update Rotation Math in `Update()`**
  - Replace `Vector3.SignedAngle` and `Quaternion.Euler` with `Quaternion.LookRotation` and `Quaternion.RotateTowards`.
  - Calculate `deltaRotation` to represent the 3D angular change for the frame.

- [x] **Task 2: Refactor `GetSafeSteps()`**
  - Change the signature to accept `Quaternion deltaRotation` instead of `float angularVelocity`.
  - Update the internal loop to apply `deltaRotation` cumulatively.

- [x] **Task 3: Implement 3D Cone Sweep**
  - Remove the 1D `-max` to `+max` lerp loop.
  - Implement a loop that samples $N$ rotation axes in a full $360^\circ$ circle around the `forward` vector.
  - Generate test `deltaRotations` and evaluate them using the updated `GetSafeSteps()`.
  - Select and apply the best safe 3D rotation.

- [x] **Task 4: Unit Test Updates**
  - Ensure existing tests pass or are updated to reflect 3D orientation.
  - (Optional) Add a new test verifying pitch/yaw changes are constrained correctly.