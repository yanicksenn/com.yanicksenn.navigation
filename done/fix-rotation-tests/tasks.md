# Fix Rotation Tests Implementation Plan

## Objective
The `Update_ConstrainsRotationToMaxRotationSpeed` test is failing because the actual turned angle is 0.0, but it expected 45.0. 
This occurs because the test sets the destination strictly behind the agent (`new Vector3(-5, 0, 0)`), meaning the target angle `alpha` is perfectly 180 degrees. The mathematical pure pursuit calculation for `sin(180)` is 0. This results in an angular speed of 0, so the agent doesn't turn at all! This is a known instability singularity in pure pursuit: when the target is exactly behind the vehicle, it cannot decide whether to turn left or right.

## Proposed Changes
1. **Handle the 180-Degree Singularity (`Update`)**
   - In `ConstrainedRotationAgentStrategy.Update()`, detect if `alpha` is very close to 180 degrees.
   - If the target is exactly behind the agent, `sin(alpha)` becomes 0, yielding a pure pursuit curvature of 0.
   - We must add a fallback: if `alpha > 179f` (or similar), force the agent to turn using its `maxRotationSpeed` in an arbitrary direction (e.g., around `transform.up` or `transform.right` if 3D).

2. **Test Updates (Optional but Recommended)**
   - The test exposes a true edge case that needed fixing. By applying the singularity fix, the test should naturally pass again as it will fallback to the `maxRotationSpeed`.

## Task Checklist
- [x] **Task 1: Add Singularity Fallback**
  - Update the curvature calculation block in `Update()` to detect angles > 179.9 and apply `maxRotationSpeed` around a default axis.
- [x] **Task 2: Test Verification**
  - Ensure the fallback allows the agent to correctly spin around at max speed when asked to turn around exactly 180 degrees.