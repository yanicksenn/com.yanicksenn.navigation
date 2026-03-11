using System;
using UnityEngine;

namespace YanickSenn.Navigation {
    public class ConstrainedRotationAgentStrategy : NavAgentStrategy {
        private readonly ConstrainedRotationAgentDefinition _definition;
        private readonly Vector3[] _projectedArc;

        private Vector3[] _path;
        private Bounds[] _corridor;
        private int _currentPathIndex;
        private bool _hasPath;
        private Vector3 _startPosition;
        private NavTarget _target;

        // Debugging
        private Vector3 _currentSteeringTarget;

        public ConstrainedRotationAgentStrategy(NavMeshAgent agent, ConstrainedRotationAgentDefinition definition) :
            base(agent) {
            _definition = definition;
            _hasPath = false;
            _path = Array.Empty<Vector3>();
            _corridor = Array.Empty<Bounds>();
            _projectedArc = new Vector3[definition.projectionSteps + 1];
        }

        public override void SetDestination(NavTarget target) {
            _target = target;
            _startPosition = Agent.transform.position;
            if (NavMeshPathfinder.TryFindPath(Agent.navMeshData, _startPosition, target.position, out _path, out _corridor)) {
                
                if (target.hasRotation && _path.Length > 0) {
                    float ghostDistance = Mathf.Max(_definition.lookaheadDistance * 2f, 2f);
                    Vector3 ghostPoint = target.position + target.rotation * Vector3.forward * ghostDistance;
                    Array.Resize(ref _path, _path.Length + 1);
                    _path[^1] = ghostPoint;
                }

                _currentPathIndex = 0;
                _hasPath = true;
            } else {
                _path = Array.Empty<Vector3>();
                _corridor = Array.Empty<Bounds>();
                _hasPath = false;
            }
        }

        public override void Stop() {
            _hasPath = false;
            _path = Array.Empty<Vector3>();
            _corridor = Array.Empty<Bounds>();
        }

        public override void Update(float deltaTime) {
            if (Mathf.Abs(Vector3.Dot(Agent.transform.forward, Vector3.up)) < 0.99f) {
                Quaternion targetUpRotation = Quaternion.LookRotation(Agent.transform.forward, Vector3.up);
                Agent.transform.rotation = Quaternion.Slerp(Agent.transform.rotation, targetUpRotation, Agent.upAlignmentSpeed * deltaTime);
            }

            if (!_hasPath || _path == null || _currentPathIndex >= _path.Length) {
                _hasPath = false;
                return;
            }

            Vector3 currentPos = Agent.transform.position;
            Vector3 forward = Agent.transform.forward;

            Vector3 targetPosition = _path[_currentPathIndex];
            Vector3 previousPosition = _currentPathIndex > 0 ? _path[_currentPathIndex - 1] : _startPosition;

            bool hasGhostPoint = _target.hasRotation;
            int actualFinalIndex = hasGhostPoint ? _path.Length - 2 : _path.Length - 1;
            bool isFinalWaypoint = (_currentPathIndex >= actualFinalIndex);
            
            // The position we actually want to stop at
            Vector3 actualTargetPosition = _target.position;
            float distToActualTarget = Vector3.Distance(currentPos, actualTargetPosition);

            Vector3 tempDesiredDirection = (targetPosition - currentPos).normalized;
            if (tempDesiredDirection == Vector3.zero) tempDesiredDirection = forward;
            float tempAlpha = Vector3.Angle(forward, tempDesiredDirection);

            float tempSpeed = Agent.speed;
            if (tempAlpha > _definition.slowDownAngleThreshold) {
                float excessAngle = tempAlpha - _definition.slowDownAngleThreshold;
                float maxExcess = 180f - _definition.slowDownAngleThreshold;
                float tSpeed = Mathf.Clamp01(excessAngle / maxExcess);
                tempSpeed = Mathf.Lerp(Agent.speed, _definition.minSpeed, tSpeed);
            }

            if (isFinalWaypoint) {
                // If we can reach the actual target in this frame, or we pass its plane, snap to it exactly and stop
                Vector3 stepVector = forward * (tempSpeed * deltaTime);
                Vector3 nextPos = currentPos + stepVector;

                Vector3 finalSegmentDir = _path.Length > 1
                    ? (_path[actualFinalIndex] - _path[actualFinalIndex > 0 ? actualFinalIndex - 1 : 0]).normalized
                    : (_path[0] - _startPosition).normalized;
                if (finalSegmentDir == Vector3.zero) finalSegmentDir = forward;

                // Passed target if we cross its plane this frame, or if we are already past its plane.
                bool passedTarget = Vector3.Dot(actualTargetPosition - currentPos, finalSegmentDir) <= 0f ||
                                    Vector3.Dot(actualTargetPosition - nextPos, finalSegmentDir) <= 0f;

                if (passedTarget || distToActualTarget <= tempSpeed * deltaTime) {
                    if (_target.hasRotation) {
                        Agent.transform.rotation = Quaternion.Slerp(Agent.transform.rotation, _target.rotation, 
                            Agent.forwardAlignmentSpeed * deltaTime);
                        // Force exact rotation on arrival for precision
                        Agent.transform.rotation = _target.rotation;
                    } else {
                        Vector3 moveDir = actualTargetPosition - currentPos;
                        if (moveDir.sqrMagnitude > 0.0001f) {
                            Quaternion targetRotation = Quaternion.LookRotation(moveDir.normalized, Agent.transform.up);
                            Agent.transform.rotation = Quaternion.Slerp(Agent.transform.rotation, targetRotation,
                                Agent.forwardAlignmentSpeed * deltaTime);
                        }
                    }

                    Agent.transform.position = actualTargetPosition;
                    _hasPath = false;
                    return;
                }
            }
            
            if (!isFinalWaypoint || hasGhostPoint) {
                // 1. Advance path index if passed the waypoint plane or close enough
                Vector3 ap = currentPos - previousPosition;
                Vector3 ab = targetPosition - previousPosition;
                float sqrLen = ab.sqrMagnitude;
                float t = sqrLen == 0 ? 1f : Vector3.Dot(ap, ab) / sqrLen;

                if (t >= 0.95f || Vector3.Distance(currentPos, targetPosition) < 0.5f) {
                    _currentPathIndex++;
                    if (_currentPathIndex < _path.Length) {
                        targetPosition = _path[_currentPathIndex];
                        previousPosition = _currentPathIndex > 0 ? _path[_currentPathIndex - 1] : _startPosition;
                    }
                }
            }

            // 2. Determine Lookahead Target (Carrot on a stick)
            // We already calculated tempSpeed above to estimate a good lookahead distance.
            Vector3 lookaheadTarget = GetLookaheadTarget(currentPos, tempSpeed);

            // 3. Determine initial steering intent
            Vector3 toTarget = lookaheadTarget - currentPos;
            float distToLookahead = toTarget.magnitude;
            Vector3 desiredDirection = toTarget.normalized;
            if (desiredDirection == Vector3.zero) desiredDirection = forward;

            Quaternion currentRotation = Agent.transform.rotation;

            float alpha = Vector3.Angle(forward, desiredDirection);

            // Calculate dynamic speed based on turn severity
            float currentSpeed = Agent.speed;
            if (alpha > _definition.slowDownAngleThreshold) {
                // Determine how far into the slowdown zone we are
                float excessAngle = alpha - _definition.slowDownAngleThreshold;
                float maxExcess = 180f - _definition.slowDownAngleThreshold;
                float tSpeed = Mathf.Clamp01(excessAngle / maxExcess);
                currentSpeed = Mathf.Lerp(Agent.speed, _definition.minSpeed, tSpeed);
            }

            float angularSpeed = 0f;
            Vector3 rotationAxis = Vector3.up;

            if (alpha > 179.9f) {
                // Pure pursuit singularity: Target is exactly behind us. Force a hard turn.
                angularSpeed = _definition.maxRotationSpeed;
                rotationAxis = Agent.transform.up;
            } else if (alpha > 0.001f && distToLookahead > 0.001f) {
                // Pure pursuit calculates a circular arc.
                // Curvature kappa = 2 * sin(alpha) / L
                // Angular speed omega = kappa * speed
                float omegaRad = (2f * currentSpeed * Mathf.Sin(alpha * Mathf.Deg2Rad)) / distToLookahead;
                float omegaDeg = omegaRad * Mathf.Rad2Deg;

                angularSpeed = Mathf.Min(omegaDeg, _definition.maxRotationSpeed);

                rotationAxis = Vector3.Cross(forward, desiredDirection).normalized;
                if (rotationAxis == Vector3.zero) rotationAxis = Agent.transform.right;
            }

            Quaternion targetRot = currentRotation;
            if (angularSpeed > 0.001f) {
                targetRot = Quaternion.AngleAxis(angularSpeed * deltaTime, rotationAxis) * currentRotation;
            }

            // 4. Project movement to check corridor bounds
            int safeStepsForDesired = GetSafeSteps(currentPos, currentRotation, rotationAxis, angularSpeed, currentSpeed);

            if (angularSpeed > 0.1f && safeStepsForDesired < _definition.projectionSteps) {
                // Arc is not completely safe.
                // Perform a 3D cone sweep to find the safest rotation closest to our desired pure pursuit rotation.
                int bestSafeSteps = safeStepsForDesired;
                Quaternion bestTargetRot = targetRot;
                float closestAngleDiff = float.MaxValue;

                int numConeAngles = 8;
                int numRadii = 2;

                // Add the zero-rotation option explicitly (going straight)
                int straightSafeSteps = GetSafeSteps(currentPos, currentRotation, Vector3.up, 0f, currentSpeed);
                if (straightSafeSteps > bestSafeSteps) {
                    bestSafeSteps = straightSafeSteps;
                    bestTargetRot = currentRotation;
                    closestAngleDiff = Quaternion.Angle(currentRotation, targetRot);
                }

                for (int r = 1; r <= numRadii; r++) {
                    float speedFraction = (float)r / numRadii;
                    float testAngularSpeed = _definition.maxRotationSpeed * speedFraction;

                    for (int i = 0; i < numConeAngles; i++) {
                        float angle = (i * 360f) / numConeAngles;
                        Vector3 testAxis = Quaternion.AngleAxis(angle, forward) * Agent.transform.right;

                        Quaternion testTargetRot =
                            Quaternion.AngleAxis(testAngularSpeed * deltaTime, testAxis) * currentRotation;

                        int safeSteps = GetSafeSteps(currentPos, currentRotation, testAxis, testAngularSpeed, currentSpeed);

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
                GetSafeSteps(currentPos, currentRotation, rotationAxis, angularSpeed, currentSpeed);
            } else {
                GetSafeSteps(currentPos, currentRotation, Vector3.up, 0f, currentSpeed);
            }

            _currentSteeringTarget = lookaheadTarget;

            // Apply rotation
            Agent.transform.rotation = targetRot;

            // Apply forward translation using the current dynamic speed
            Agent.transform.position += Agent.transform.forward * (currentSpeed * deltaTime);
        }

        private Vector3 GetLookaheadTarget(Vector3 currentPos, float speed) {
            // Minimum turning radius prevents the agent from orbiting a point it cannot physically turn tightly enough to reach.
            // When going slower, the min radius gets smaller allowing tighter turns.
            float minRadius = speed / (_definition.maxRotationSpeed * Mathf.Deg2Rad);
            // Ensure lookahead is always safely larger than the turning radius, scaled by speed ratio
            float speedRatio = speed / Agent.speed;
            float L = Mathf.Max(_definition.lookaheadDistance * speedRatio, minRadius * 1.2f);

            Vector3 previousPosition = _currentPathIndex > 0 ? _path[_currentPathIndex - 1] : _startPosition;
            Vector3 targetPosition = _path[_currentPathIndex];

            // Find closest point on current segment
            Vector3 closestPoint = GetClosestPointOnSegment(previousPosition, targetPosition, currentPos);

            float distanceAccumulator = 0f;
            Vector3 lastPoint = closestPoint;

            // Advance along the path starting from the closest point
            for (int i = _currentPathIndex; i < _path.Length; i++) {
                float distToSegmentEnd = Vector3.Distance(lastPoint, _path[i]);
                if (distanceAccumulator + distToSegmentEnd >= L) {
                    float excess = L - distanceAccumulator;
                    Vector3 dir = (_path[i] - lastPoint).normalized;
                    return lastPoint + dir * excess;
                }

                distanceAccumulator += distToSegmentEnd;
                lastPoint = _path[i];
            }

            // Reached the end of the path, just return the final waypoint
            return _path[^1];
        }

        private Vector3 GetClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 p) {
            Vector3 ap = p - a;
            Vector3 ab = b - a;
            float sqrLen = ab.sqrMagnitude;
            if (sqrLen == 0) return a;
            float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / sqrLen);
            return a + t * ab;
        }

        private int GetSafeSteps(Vector3 startPos, Quaternion startRot, Vector3 rotationAxis, float angularSpeed, float traversalSpeed) {
            _projectedArc[0] = startPos;
            Vector3 simPos = startPos;
            Quaternion simRot = startRot;

            for (int i = 1; i <= _definition.projectionSteps; i++) {
                float dt = _definition.projectionStepTime;

                if (angularSpeed > 0.001f) {
                    simRot = Quaternion.AngleAxis(angularSpeed * dt, rotationAxis) * simRot;
                }

                Vector3 nextSimPos = simPos + simRot * Vector3.forward * (traversalSpeed * dt);

                // If we pass the actual target, the rest of the path is safe
                if (_hasPath && _path.Length > 0) {
                    bool hasGhostPoint = _target.hasRotation;
                    int actualFinalIndex = hasGhostPoint ? _path.Length - 2 : _path.Length - 1;
                    Vector3 actualTarget = _target.position;

                    Vector3 finalSegmentDir = _path.Length > 1
                        ? (_path[actualFinalIndex] - _path[actualFinalIndex > 0 ? actualFinalIndex - 1 : 0]).normalized
                        : (_path[0] - _startPosition).normalized;
                    if (finalSegmentDir == Vector3.zero) finalSegmentDir = startRot * Vector3.forward;

                    if (Vector3.Dot(actualTarget - simPos, finalSegmentDir) >= 0f &&
                        Vector3.Dot(actualTarget - nextSimPos, finalSegmentDir) <= 0f) {
                        for (int j = i; j <= _definition.projectionSteps; j++) _projectedArc[j] = nextSimPos;
                        return _definition.projectionSteps;
                    }
                }

                simPos = nextSimPos;
                _projectedArc[i] = simPos;

                if (!IsPointInCorridor(simPos)) {
                    return i - 1;
                }
            }

            return _definition.projectionSteps;
        }

        private bool IsPointInCorridor(Vector3 point) {
            // Allow a small tolerance margin so boundary floats don't fail immediately
            for (int i = 0; i < _corridor.Length; i++) {
                Bounds b = _corridor[i];
                b.Expand(0.01f);
                if (b.Contains(point)) {
                    return true;
                }
            }

            return false;
        }

        public override bool HasPath => _hasPath;

        public override Vector3[] CurrentPath => _hasPath ? _path : new Vector3[0];

        public override Bounds[] CurrentPathCubes => _hasPath ? _corridor : new Bounds[0];

        public override void DrawGizmos() {
            if (_path == null || _path.Length == 0) return;

            // Draw completed path in grey
            Gizmos.color = Color.grey;
            Vector3 lastPoint = _startPosition;
            for (int i = 0; i < _currentPathIndex; i++) {
                Gizmos.DrawLine(lastPoint, _path[i]);
                lastPoint = _path[i];
            }

            Gizmos.DrawLine(lastPoint, Agent.transform.position);

            // Draw remaining path in yellow
            Gizmos.color = Color.yellow;
            lastPoint = Agent.transform.position;
            for (int i = _currentPathIndex; i < _path.Length; i++) {
                Gizmos.DrawLine(lastPoint, _path[i]);
                lastPoint = _path[i];
            }

            // Draw lookahead target
            if (_hasPath) {
                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(_currentSteeringTarget, 0.2f);
                Gizmos.DrawLine(Agent.transform.position, _currentSteeringTarget);
            }

            // Draw projected arc
            if (_hasPath && _projectedArc != null && _projectedArc.Length > 1) {
                Gizmos.color = Color.cyan;
                for (int i = 0; i < _projectedArc.Length - 1; i++) {
                    if (_projectedArc[i] != Vector3.zero && _projectedArc[i + 1] != Vector3.zero) {
                        Gizmos.DrawLine(_projectedArc[i], _projectedArc[i + 1]);
                    }
                }
            }
        }
    }
}
