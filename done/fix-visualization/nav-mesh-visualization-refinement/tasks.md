# Nav Mesh Visualization Refinement

## Goal
Restrict the nav mesh grid visualization so that it only appears when the relevant object (the `NavMeshData` asset or a `NavMeshAgent` referencing it) is selected in the Editor.

## Implementation Tasks
- [x] **Task 1:** Remove the global `SceneView.duringSceneGui` hook from `NavMeshBakeWindow`.
- [x] **Task 2:** Create `NavMeshDataEditor` to draw the grid when the `NavMeshData` asset is selected.
- [x] **Task 3:** Add gizmo drawing to `NavMeshAgent` so the grid is visible when the agent is selected.

## Validation
- Selecting a `NavMeshData` asset in the Project window should show the grid in the Scene View.
- Selecting a GameObject with a `NavMeshAgent` that has data assigned should show the grid.
- Deselecting these objects should hide the grid, even if the Bake Window is open.
