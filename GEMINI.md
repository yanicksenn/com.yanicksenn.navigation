# Navigation Package Knowledge Base

This document provides an overview of the custom Navigation package, detailing the project structure, core systems, and how to use the available classes.

## 📁 Project Structure

All scripts are contained within the `YanickSenn.Navigation` namespace.

```text
com.yanicksenn.navigation/
├── Editor/                         # Editor-only scripts and tools
│   ├── NavMeshBakeWindow.cs        # The Nav Mesh Baker tool window (Window > Navigation > Nav Mesh Baker).
│   ├── NavMeshDataEditor.cs        # Custom inspector to draw the NavMesh in the Scene View when the asset is selected.
│   └── NavMeshVisualizationUtility.cs # Shared utility for drawing Gizmos and Handles.
│
├── Runtime/                        # Core runtime scripts
│   ├── NavMeshData.cs              # ScriptableObject storing the baked WalkableCubes array.
│   ├── NavMeshBuilder.cs           # Core Octree-based geometric subdivision baking algorithm.
│   ├── NavMeshPathfinder.cs        # A* pathfinding implementation over WalkableCubes.
│   ├── NavMeshAgent.cs             # The MonoBehaviour component attached to moving entities.
│   ├── NavMeshPatrol.cs            # Utility script to make an agent patrol between waypoints.
│   │
│   ├── NavAgentDefinition.cs       # Abstract ScriptableObject base for defining agent configuration.
│   ├── NavAgentStrategy.cs         # Abstract class for runtime agent logic/movement.
│   ├── ConstantSpeedAgentDefinition.cs # Concrete definition for constant-speed movement.
│   └── ConstantSpeedAgentStrategy.cs   # Concrete strategy utilizing pathfinding and constant speed interpolation.
│
└── Tests/                          # Unit tests (EditMode)
    └── Runtime/
        ├── ConstantSpeedAgentStrategyTests.cs
        ├── NavMeshBuilderTests.cs
        └── NavMeshPathfinderTests.cs
```

## 🧠 Core Systems & Usage

### 1. Baking the Nav Mesh
The Nav Mesh is generated using an Octree-based subdivision algorithm. Instead of using physics queries, it uses pure geometric bounds intersection (`Bounds.Intersects`) against colliders on a specific LayerMask.

**How to Bake:**
1. Open the baker window: `Window > Navigation > Nav Mesh Baker`.
2. Create a `NavMeshData` ScriptableObject in your project (`Create > Navigation > NavMesh Data`).
3. Assign the created data object to the "Baked Data Object" field in the window.
4. Configure Baking Bounds (toggling Min/Max constraints). Unconstrained axes will extend infinitely.
5. Set the `MinCubeSize` (the smallest resolution the algorithm will subdivide down to).
6. Set the `Collision Layer` to target your obstacle colliders.
7. Click **Bake**. 

*Visualization:* The full baked grid is only drawn in the Scene View when the `NavMeshData` asset is selected in the Project or Inspector window.

### 2. Pathfinding (`NavMeshPathfinder`)
The `NavMeshPathfinder` uses the A* algorithm over the `WalkableCubes` defined in `NavMeshData`. 
- Nodes are the walkable cubes.
- Neighbors are determined by slightly expanding bounding boxes to detect shared faces.
- It calculates dynamic "portals" between cubes, allowing agents to move efficiently across shared face boundaries rather than traveling strictly center-to-center.
- It returns both the `Vector3[]` waypoints and the `Bounds[]` of the cubes that make up the path.

### 3. Agent Architecture (Strategy Pattern)
The `NavMeshAgent` component is extremely lightweight and acts as a proxy. Its behavior is dictated by the Strategy Pattern.

**How to setup an Agent:**
1. Add the `NavMeshAgent` MonoBehaviour to your GameObject.
2. Assign the baked `NavMeshData` to the agent.
3. Create an agent definition asset, for example: `Create > Navigation > Constant Speed Agent Definition`. Configure its speed.
4. Assign this definition asset to the `agentDefinition` field on the `NavMeshAgent`.

**Agent API:**
- `agent.SetDestination(Vector3 target)`: Instructs the agent to calculate a path and begin moving.
- `agent.Stop()`: Halts the agent and clears its path.
- `agent.HasPath`: Returns `true` if the agent is currently following a path.
- `agent.CurrentPath`: Returns the array of remaining waypoints.
- `agent.CurrentPathCubes`: Returns the array of cubes making up the current path.

*Visualization:* When a `NavMeshAgent` is selected in the Editor and has an active path, it will draw its remaining path in **Yellow**, its completed path in **Grey**, and the boundary cubes of its current path route in **Cyan**.

### 4. Patrolling
To make an agent patrol automatically:
1. Ensure the GameObject has a `NavMeshAgent` set up correctly.
2. Add the `NavMeshPatrol` component.
3. Assign an array of `Transform` points to the `patrolPoints` list.
4. (Optional) Set a `waitTime` to pause at each point. 
The agent will cycle through the points indefinitely, looping back to the start when finished.

## 🛠 Extending the System
To add new movement behaviors (e.g., Physics-based movement, acceleration/deceleration, steering behaviors):
1. Create a new class inheriting from `NavAgentDefinition` (ScriptableObject). Add any necessary serialized configuration fields.
2. Create a new class inheriting from `NavAgentStrategy`. Implement `Update(float deltaTime)`, `SetDestination`, and `Stop`. Use `NavMeshPathfinder.TryFindPath` to get the route.
3. Override `CreateStrategy` in your definition to return your new strategy.
