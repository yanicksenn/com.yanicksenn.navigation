# Pure Pursuit Optimizations Implementation Plan

## Objective
The agent sometimes uses its maximum rotation speed even when a wider, smoother circle would be optimal. If it gets too close to a target and is misaligned, this leads to an orbit because the target falls inside its minimum turning circle. 
We will implement true 3D Pure Pursuit curvature logic, which mathematically guarantees the agent traces a graceful arc to the lookahead target instead of greedily cranking the "steering wheel" to max. We will also prevent final-target orbiting by extending the lookahead point past the destination.

## Proposed Changes

1. **Calculate Pure Pursuit Curvature (`Update`)**
   - Replace the `RotateTowards` logic with true pure pursuit angular velocity computation.
   - Using the formula `omega = (2 * speed * sin(alpha)) / distanceToLookahead`.
   - The agent will only use its `maxRotationSpeed` if the mathematical arc requires a turn tighter than it is physically capable of making.

2. **Extrapolate Final Lookahead (`GetLookaheadTarget`)**
   - If the remaining path distance is less than the required lookahead `L`, instead of clamping the lookahead to the final waypoint (which drops the distance to 0 and causes infinite curvature/orbiting), extrapolate the point linearly past the final waypoint.
   - This ensures the agent always steers towards a point exactly distance `L` away, pulling it smoothly *through* the final destination.

3. **Update Safety Simulation (`GetSafeSteps`)**
   - Since the lookahead target is now extrapolated past the final waypoint, the pure pursuit arc will point slightly past the destination.
   - If the simulation projects the agent's path *past* the final waypoint, it must instantly consider the rest of the steps "safe" to prevent the agent from triggering avoidance maneuvers for walls behind its destination.

## Task Checklist
- [x] **Task 1: Pure Pursuit Angular Velocity**
  - Implement `omega = (2 * v * sin(alpha)) / L` in `Update` and cap by `maxRotationSpeed`.
- [x] **Task 2: Extrapolate Lookahead Target**
  - Update `GetLookaheadTarget` to project past the final waypoint.
- [x] **Task 3: Safe Steps Target Check**
  - Update `GetSafeSteps` to return max steps if the simulated path crosses the final target plane.