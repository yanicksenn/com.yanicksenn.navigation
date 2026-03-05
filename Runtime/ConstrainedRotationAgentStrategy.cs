using UnityEngine;

namespace YanickSenn.Navigation {
    public class ConstrainedRotationAgentStrategy : NavAgentStrategy {
        private readonly ConstrainedRotationAgentDefinition definition;
        private Vector3[] path;
        private Bounds[] corridor;
        private int currentPathIndex;
        private bool hasPath;
        private Vector3 startPosition;

        // Debugging
        private Vector3 currentSteeringTarget;
        private Vector3[] projectedArc;

        public ConstrainedRotationAgentStrategy(NavMeshAgent agent, ConstrainedRotationAgentDefinition definition) :
            base(agent) {
            this.definition = definition;
            this.hasPath = false;
            this.path = new Vector3[0];
            this.corridor = new Bounds[0];
            this.projectedArc = new Vector3[definition.projectionSteps + 1];
        }

        public override void SetDestination(Vector3 target) {
            startPosition = agent.transform.position;
            if (NavMeshPathfinder.TryFindPath(agent.navMeshData, startPosition, target, out path, out corridor)) {
                currentPathIndex = 0;
                hasPath = true;
            } else {
                path = new Vector3[0];
                corridor = new Bounds[0];
                hasPath = false;
            }
        }

        public override void Stop() {
            hasPath = false;
            path = new Vector3[0];
            corridor = new Bounds[0];
        }

        public override void Update(float deltaTime) {
            if (Mathf.Abs(Vector3.Dot(agent.transform.forward, Vector3.up)) < 0.99f) {
                Quaternion targetUpRotation = Quaternion.LookRotation(agent.transform.forward, Vector3.up);
                agent.transform.rotation = Quaternion.Slerp(agent.transform.rotation, targetUpRotation, definition.upAlignmentSpeed * deltaTime);
            }

            if (!hasPath || path == null || currentPathIndex >= path.Length) {
                hasPath = false;
                return;
            }

            Vector3 currentPos = agent.transform.position;
            Vector3 forward = agent.transform.forward;

            Vector3 targetPosition = path[currentPathIndex];
            Vector3 previousPosition = currentPathIndex > 0 ? path[currentPathIndex - 1] : startPosition;

            bool isFinalWaypoint = (currentPathIndex == path.Length - 1);
            float distToTarget = Vector3.Distance(currentPos, targetPosition);

            if (isFinalWaypoint) {
                // If we can reach the target in this frame, or we pass its plane, snap to it exactly and stop
                Vector3 stepVector = forward * (definition.speed * deltaTime);
                Vector3 nextPos = currentPos + stepVector;

                Vector3 finalSegmentDir = path.Length > 1
                    ? (path[path.Length - 1] - path[path.Length - 2]).normalized
                    : (path[0] - startPosition).normalized;
                if (finalSegmentDir == Vector3.zero) finalSegmentDir = forward;

                // Passed target if it was in front of us, and is now behind us relative to the final path direction.
                bool passedTarget = Vector3.Dot(targetPosition - currentPos, finalSegmentDir) >= 0f &&
                                    Vector3.Dot(targetPosition - nextPos, finalSegmentDir) <= 0f;

                if (passedTarget || distToTarget <= definition.speed * deltaTime) {
                    Vector3 moveDir = targetPosition - currentPos;
                    if (moveDir.sqrMagnitude > 0.0001f) {
                        Quaternion targetRotation = Quaternion.LookRotation(moveDir.normalized, agent.transform.up);
                        agent.transform.rotation = Quaternion.Slerp(agent.transform.rotation, targetRotation,
                            definition.forwardAlignmentSpeed * deltaTime);
                    }

                    agent.transform.position = targetPosition;
                    hasPath = false;
                    return;
                }
            } else {
                // 1. Advance path index if passed the waypoint plane or close enough
                Vector3 ap = currentPos - previousPosition;
                Vector3 ab = targetPosition - previousPosition;
                float sqrLen = ab.sqrMagnitude;
                float t = sqrLen == 0 ? 1f : Vector3.Dot(ap, ab) / sqrLen;

                if (t >= 0.95f || distToTarget < 0.5f) {
                    currentPathIndex++;
                    targetPosition = path[currentPathIndex];
                    previousPosition = currentPathIndex > 0 ? path[currentPathIndex - 1] : startPosition;
                }
            }

            // 2. Determine Lookahead Target (Carrot on a stick)
            Vector3 lookaheadTarget = GetLookaheadTarget(currentPos);

            // 3. Determine initial steering intent
            Vector3 toTarget = lookaheadTarget - currentPos;
            float distToLookahead = toTarget.magnitude;
            Vector3 desiredDirection = toTarget.normalized;
            if (desiredDirection == Vector3.zero) desiredDirection = forward;

            Quaternion currentRotation = agent.transform.rotation;

            float alpha = Vector3.Angle(forward, desiredDirection);
            float angularSpeed = 0f;
            Vector3 rotationAxis = Vector3.up;

            if (alpha > 179.9f) {
                // Pure pursuit singularity: Target is exactly behind us. Force a hard turn.
                angularSpeed = definition.maxRotationSpeed;
                rotationAxis = agent.transform.up;
            } else if (alpha > 0.001f && distToLookahead > 0.001f) {
                // Pure pursuit calculates a circular arc.
                // Curvature kappa = 2 * sin(alpha) / L
                // Angular speed omega = kappa * speed
                float omegaRad = (2f * definition.speed * Mathf.Sin(alpha * Mathf.Deg2Rad)) / distToLookahead;
                float omegaDeg = omegaRad * Mathf.Rad2Deg;

                angularSpeed = Mathf.Min(omegaDeg, definition.maxRotationSpeed);

                rotationAxis = Vector3.Cross(forward, desiredDirection).normalized;
                if (rotationAxis == Vector3.zero) rotationAxis = agent.transform.right;
            }

            Quaternion targetRot = currentRotation;
            if (angularSpeed > 0.001f) {
                targetRot = Quaternion.AngleAxis(angularSpeed * deltaTime, rotationAxis) * currentRotation;
            }

            // 4. Project movement to check corridor bounds
            int safeStepsForDesired = GetSafeSteps(currentPos, currentRotation, rotationAxis, angularSpeed);

            if (angularSpeed > 0.1f && safeStepsForDesired < definition.projectionSteps) {
                // Arc is not completely safe.
                // Perform a 3D cone sweep to find the safest rotation closest to our desired pure pursuit rotation.
                int bestSafeSteps = safeStepsForDesired;
                Quaternion bestTargetRot = targetRot;
                float closestAngleDiff = float.MaxValue;

                int numConeAngles = 8;
                int numRadii = 2;

                // Add the zero-rotation option explicitly (going straight)
                int straightSafeSteps = GetSafeSteps(currentPos, currentRotation, Vector3.up, 0f);
                if (straightSafeSteps > bestSafeSteps) {
                    bestSafeSteps = straightSafeSteps;
                    bestTargetRot = currentRotation;
                    closestAngleDiff = Quaternion.Angle(currentRotation, targetRot);
                }

                for (int r = 1; r <= numRadii; r++) {
                    float speedFraction = (float)r / numRadii;
                    float testSpeed = definition.maxRotationSpeed * speedFraction;

                    for (int i = 0; i < numConeAngles; i++) {
                        float angle = (i * 360f) / numConeAngles;
                        Vector3 testAxis = Quaternion.AngleAxis(angle, forward) * agent.transform.right;

                        Quaternion testTargetRot =
                            Quaternion.AngleAxis(testSpeed * deltaTime, testAxis) * currentRotation;

                        int safeSteps = GetSafeSteps(currentPos, currentRotation, testAxis, testSpeed);

                        if (safeSteps > bestSafeSteps) {
                            bestSafeSteps = safeSteps;
                            bestTargetRot = testTargetRot;
                            closestAngleDiff = Quaternion.Angle(testTargetRot, targetRot);
                        } else if (safeSteps == bestSafeSteps) {
                            float diff = Quaternion.Angle(testTargetRot, targetRot);
                            if (diff < closestAngleDiff) {
                                closestAngleDiff = diff;
                                bestTargetRot = testTargetRot;
                            }
                        }
                    }
                }

                targetRot = bestTargetRot;

                // Recalculate angularSpeed and rotationAxis for the final projection
                Quaternion finalDelta = targetRot * Quaternion.Inverse(currentRotation);
                finalDelta.ToAngleAxis(out float finalAngle, out Vector3 finalAxis);
                if (finalAngle > 180f) {
                    finalAngle = 360f - finalAngle;
                    finalAxis = -finalAxis;
                }

                if (finalAngle > 0.001f) {
                    angularSpeed = finalAngle / deltaTime;
                    rotationAxis = finalAxis;
                } else {
                    angularSpeed = 0f;
                    rotationAxis = Vector3.up;
                }
            }

            // Re-run for final projected arc visualization
            if (angularSpeed > 0.001f) {
                GetSafeSteps(currentPos, currentRotation, rotationAxis, angularSpeed);
            } else {
                GetSafeSteps(currentPos, currentRotation, Vector3.up, 0f);
            }

            currentSteeringTarget = lookaheadTarget;

            // Apply rotation
            agent.transform.rotation = targetRot;

            // Apply constant forward translation
            agent.transform.position += agent.transform.forward * (definition.speed * deltaTime);
        }

        private Vector3 GetLookaheadTarget(Vector3 currentPos) {
            // Minimum turning radius prevents the agent from orbiting a point it cannot physically turn tightly enough to reach.
            float minRadius = definition.speed / (definition.maxRotationSpeed * Mathf.Deg2Rad);
            // Ensure lookahead is always safely larger than the turning radius
            float L = Mathf.Max(definition.lookaheadDistance, minRadius * 1.2f);

            Vector3 previousPosition = currentPathIndex > 0 ? path[currentPathIndex - 1] : startPosition;
            Vector3 targetPosition = path[currentPathIndex];

            // Find closest point on current segment
            Vector3 closestPoint = GetClosestPointOnSegment(previousPosition, targetPosition, currentPos);

            float distanceAccumulator = 0f;
            Vector3 lastPoint = closestPoint;

            // Advance along the path starting from the closest point
            for (int i = currentPathIndex; i < path.Length; i++) {
                float distToSegmentEnd = Vector3.Distance(lastPoint, path[i]);
                if (distanceAccumulator + distToSegmentEnd >= L) {
                    float excess = L - distanceAccumulator;
                    Vector3 dir = (path[i] - lastPoint).normalized;
                    return lastPoint + dir * excess;
                }

                distanceAccumulator += distToSegmentEnd;
                lastPoint = path[i];
            }

            // Extrapolate past the final waypoint
            Vector3 finalSegmentDir;
            if (path.Length > 1) {
                finalSegmentDir = (path[path.Length - 1] - path[path.Length - 2]).normalized;
            } else {
                finalSegmentDir = (path[0] - startPosition).normalized;
            }

            if (finalSegmentDir == Vector3.zero) finalSegmentDir = agent.transform.forward;

            float remainingExcess = L - distanceAccumulator;
            return path[path.Length - 1] + finalSegmentDir * remainingExcess;
        }

        private Vector3 GetClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 p) {
            Vector3 ap = p - a;
            Vector3 ab = b - a;
            float sqrLen = ab.sqrMagnitude;
            if (sqrLen == 0) return a;
            float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / sqrLen);
            return a + t * ab;
        }

        private int GetSafeSteps(Vector3 startPos, Quaternion startRot, Vector3 rotationAxis, float angularSpeed) {
            projectedArc[0] = startPos;
            Vector3 simPos = startPos;
            Quaternion simRot = startRot;

            for (int i = 1; i <= definition.projectionSteps; i++) {
                float dt = definition.projectionStepTime;

                if (angularSpeed > 0.001f) {
                    simRot = Quaternion.AngleAxis(angularSpeed * dt, rotationAxis) * simRot;
                }

                Vector3 nextSimPos = simPos + simRot * Vector3.forward * (definition.speed * dt);

                // If we pass the final target, the rest of the path is safe
                if (hasPath && path.Length > 0) {
                    Vector3 finalTarget = path[path.Length - 1];
                    Vector3 finalSegmentDir = path.Length > 1
                        ? (path[path.Length - 1] - path[path.Length - 2]).normalized
                        : (path[0] - startPosition).normalized;
                    if (finalSegmentDir == Vector3.zero) finalSegmentDir = startRot * Vector3.forward;

                    if (Vector3.Dot(finalTarget - simPos, finalSegmentDir) >= 0f &&
                        Vector3.Dot(finalTarget - nextSimPos, finalSegmentDir) <= 0f) {
                        for (int j = i; j <= definition.projectionSteps; j++) projectedArc[j] = nextSimPos;
                        return definition.projectionSteps;
                    }
                }

                simPos = nextSimPos;
                projectedArc[i] = simPos;

                if (!IsPointInCorridor(simPos)) {
                    return i - 1;
                }
            }

            return definition.projectionSteps;
        }

        private bool IsPointInCorridor(Vector3 point) {
            // Allow a small tolerance margin so boundary floats don't fail immediately
            for (int i = 0; i < corridor.Length; i++) {
                Bounds b = corridor[i];
                b.Expand(0.01f);
                if (b.Contains(point)) {
                    return true;
                }
            }

            return false;
        }

        public override bool HasPath => hasPath;

        public override Vector3[] CurrentPath => hasPath ? path : new Vector3[0];

        public override Bounds[] CurrentPathCubes => hasPath ? corridor : new Bounds[0];

        public override void DrawGizmos() {
            if (path == null || path.Length == 0) return;

            // Draw completed path in grey
            Gizmos.color = Color.grey;
            Vector3 lastPoint = startPosition;
            for (int i = 0; i < currentPathIndex; i++) {
                Gizmos.DrawLine(lastPoint, path[i]);
                lastPoint = path[i];
            }

            Gizmos.DrawLine(lastPoint, agent.transform.position);

            // Draw remaining path in yellow
            Gizmos.color = Color.yellow;
            lastPoint = agent.transform.position;
            for (int i = currentPathIndex; i < path.Length; i++) {
                Gizmos.DrawLine(lastPoint, path[i]);
                lastPoint = path[i];
            }

            // Draw lookahead target
            if (hasPath) {
                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(currentSteeringTarget, 0.2f);
                Gizmos.DrawLine(agent.transform.position, currentSteeringTarget);
            }

            // Draw projected arc
            if (hasPath && projectedArc != null && projectedArc.Length > 1) {
                Gizmos.color = Color.cyan;
                for (int i = 0; i < projectedArc.Length - 1; i++) {
                    if (projectedArc[i] != Vector3.zero && projectedArc[i + 1] != Vector3.zero) {
                        Gizmos.DrawLine(projectedArc[i], projectedArc[i + 1]);
                    }
                }
            }
        }
    }
}
