# Fix NavMeshBuilder Intersection Logic

## Goal
Fix `Bake_SingleObstacle_SubdividesCorrectly` unit test failure caused by `Bounds.Intersects` performing an inclusive boundary check. 

## Context
When two cubes share an edge or a face (e.g., bounds `[0, 2]` and `[-2, 0]` touch at `0`), Unity's `Bounds.Intersects` returns `true`. This incorrectly marks adjacent walkable cubes as intersecting with an obstacle, causing them to be discarded.

## Implementation Tasks
- [x] **Task 1:** Implement a custom `IntersectsExclusive` method in `NavMeshBuilder.cs` that uses strict inequalities (`<` and `>`).
- [x] **Task 2:** Replace the use of `currentBounds.Intersects(obstacleBounds[i])` with `IntersectsExclusive`.
