# Nav Agent Strategy Refactoring

## Goal
Make the `NavMeshAgent` flexible and customizable by implementing a Strategy Pattern. The agent configuration is stored in an abstract `NavAgentDefinition` ScriptableObject. It creates an abstract `NavAgentStrategy` which executes the actual movement and state logic.

## Scope
1. **Strategy Interfaces:** Create abstract base classes `NavAgentDefinition` and `NavAgentStrategy`.
2. **Implementation:** Create a `ConstantSpeedAgentDefinition` and `ConstantSpeedAgentStrategy` that implements a simple point-to-point movement without obstacle avoidance or pathfinding, moving the agent at a constant speed to the target position, snapping upon arrival.
3. **Agent Integration:** Refactor `NavMeshAgent` to hold a reference to the definition, instantiate its strategy at runtime, and proxy its API to the strategy.

## Implementation Tasks
- [x] **Task 1:** Create `NavAgentDefinition.cs` abstract ScriptableObject with `CreateStrategy(NavMeshAgent)` method.
- [x] **Task 2:** Create `NavAgentStrategy.cs` abstract class representing the runtime state and behavior of an agent.
- [x] **Task 3:** Create `ConstantSpeedAgentDefinition.cs` defining `speed` and creating the corresponding strategy.
- [x] **Task 4:** Create `ConstantSpeedAgentStrategy.cs` handling the constant movement interpolation and snapping behavior.
- [x] **Task 5:** Update `NavMeshAgent.cs` to use the strategy pattern in `Awake`/`Update` and proxy API calls.
- [x] **Task 6:** Create unit tests validating `ConstantSpeedAgentStrategy`'s movement and snapping.
