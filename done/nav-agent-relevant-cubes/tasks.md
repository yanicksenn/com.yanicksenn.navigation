# Nav Agent Relevant Cubes Visualization

## Goal
Show only the relevant walkable cubes of a `NavMeshAgent` when it is moving. The logic for drawing these cubes should be housed in the `NavMeshAgent` itself.

## Implementation Tasks
- [x] **Task 1:** Update `NavMeshPathfinder.TryFindPath` to also return an array of `Bounds` representing the sequence of walkable cubes making up the path.
- [x] **Task 2:** Update `NavAgentStrategy` and `ConstantSpeedAgentStrategy` to expose a `CurrentPathCubes` property.
- [x] **Task 3:** Update `NavMeshAgent.OnDrawGizmos` to iterate over its strategy's `CurrentPathCubes` and draw them using `Gizmos.DrawWireCube`.
- [x] **Task 4:** Fix `NavMeshPathfinderTests` and `ConstantSpeedAgentStrategyTests` to accommodate the signature change.