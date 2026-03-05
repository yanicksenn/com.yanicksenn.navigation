# Nav Mesh Baking Implementation Plan

## Goal
Implement a performant Nav Mesh Baking tool for Unity that systematically subdivides space into variable-sized Walkable Cubes, constrained or unconstrained by user-defined bounds. The walkable data is baked into a `NavMeshData` ScriptableObject, which can then be assigned to a `NavMeshAgent`.

## Core Algorithm Strategy (Geometric Octree Subdivision)
To achieve fast generation and variable cube sizes, an Octree-based spatial subdivision algorithm is optimal. Instead of querying the physics engine, we use pure geometric bounds intersection:
1. **Gather Obstacles:** Find all `Collider` components (or `Renderer` if preferred, but usually `Collider` implies obstacles) in the scene that match the specified collision `LayerMask`. Extract their `Bounds` into a list for extremely fast iteration.
2. **Root Bounding Box:** Calculate an initial massive bounding box based on user constraints. For explicitly constrained axes, use the user's value. For unconstrained axes, use a practically infinite value (e.g., `100,000` units).
3. **Subdivision:** 
    - Check the current cube bounds against all gathered obstacle bounds using `cubeBounds.Intersects(obstacleBounds)`.
    - If there's **no intersection**, the entire cube is added to the list of `WalkableCubes`.
    - If there is an **intersection**:
        - Check if subdividing it further would result in cubes smaller than the `MinCubeSize`.
        - If the cube is already at `MinCubeSize`, it is discarded (marked unwalkable).
        - Otherwise, subdivide the cube into 8 smaller child cubes and repeat the process recursively.

## NavMeshAgent API Boundaries
A simple stub for the agent to consume the baked data.
```csharp
public class NavMeshAgent : MonoBehaviour
{
    public NavMeshData navMeshData;
    public float speed = 5f;

    public void SetDestination(Vector3 target) { /* ... */ }
    public void Stop() { /* ... */ }
    public bool HasPath { get; }
    public Vector3[] CurrentPath { get; }
}
```

## Testing Strategy
Since the builder uses purely mathematical bounds intersection, tests will be very fast `EditMode` tests. We can simulate obstacles by simply passing `Bounds` structs directly to the algorithm without even needing GameObjects.
- **Test Empty Space:** Validates that an unconstrained bake yields a single massive cube.
- **Test Single Obstacle:** Validates that passing a single obstacle bounds properly subdivides the space around it, discarding only the minimum necessary cubes.
- **Test Constraints:** Validates that setting bounds explicitly prevents the algorithm from producing Walkable Cubes outside that volume.
- **Test Minimum Size:** Validates that no cube smaller than `MinCubeSize` is ever generated.

## Granular Implementation Tasks
- [x] **Task 1:** Create `NavMeshData` ScriptableObject to store an array of `Bounds` representing the Walkable Cubes.
- [x] **Task 2:** Create `NavMeshBuilder` core static class containing the recursive geometric Octree subdivision algorithm.
- [x] **Task 3:** Create `NavMeshAgent` MonoBehaviour stub with the specified API boundaries.
- [x] **Task 4:** Create `NavMeshBakeWindow` Editor window with UI toggles for constraints (Min/Max X,Y,Z), `MinCubeSize`, `LayerMask`, and a "Bake" button.
- [x] **Task 5:** Implement Scene View drawing (Gizmos) in `NavMeshBakeWindow` to visualize the baked `NavMeshData` cubes when the window is open.
- [x] **Task 6:** Create `NavMeshBuilderTests` to validate empty space, single obstacle, constraints, and min size enforcement.
