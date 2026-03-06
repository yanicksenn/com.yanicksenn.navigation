using System.Collections.Generic;
using UnityEngine;

namespace YanickSenn.Navigation
{
    public class AnimationAgentStrategy : NavAgentStrategy
    {
        private readonly AnimationAgentDefinition definition;
        private bool hasPath;
        private Vector3[] continuousPath;
        private Bounds[] pathCubes;
        
        private List<AgentAnimationDefinition> currentDefinitionSequence;
        private AgentAnimation currentPlayingAnimation;
        private int currentAnimationIndex;

        public AnimationAgentStrategy(NavMeshAgent agent, AnimationAgentDefinition definition) : base(agent)
        {
            this.definition = definition;
            this.hasPath = false;
        }

        public override bool HasPath => hasPath;

        public override Vector3[] CurrentPath => hasPath ? continuousPath : new Vector3[0];

        public override Bounds[] CurrentPathCubes => hasPath ? pathCubes : new Bounds[0];

        public override void SetDestination(Vector3 target)
        {
            Stop();

            // 1. Get the continuous path cubes to bound our search
            if (!NavMeshPathfinder.TryFindPath(agent.navMeshData, agent.transform.position, target, out continuousPath, out pathCubes))
            {
                return;
            }

            // 2. Perform discrete A* search using available animations
            if (TryFindAnimationSequence(agent.transform.position, agent.transform.rotation, target, out currentDefinitionSequence))
            {
                hasPath = true;
                currentAnimationIndex = 0;
                PlayNextAnimation();
            }
        }

        public override void Stop()
        {
            hasPath = false;
            
            if (currentPlayingAnimation != null && currentPlayingAnimation.IsPlaying)
            {
                currentPlayingAnimation.Stop();
                currentPlayingAnimation.OnComplete -= PlayNextAnimation;
            }
            currentPlayingAnimation = null;
            
            currentDefinitionSequence?.Clear();
            continuousPath = new Vector3[0];
            pathCubes = new Bounds[0];
        }

        public override void Update(float deltaTime)
        {
            // Animations handle their own progression
        }

        private bool TryFindAnimationSequence(Vector3 startPos, Quaternion startRot, Vector3 target, out List<AgentAnimationDefinition> sequence)
        {
            sequence = new List<AgentAnimationDefinition>();
            
            if (definition.availableAnimations == null || definition.availableAnimations.Length == 0) return false;

            var startState = new DiscreteState(startPos, startRot);

            List<SearchNode> openSet = new List<SearchNode>();
            HashSet<DiscreteState> closedSet = new HashSet<DiscreteState>();

            SearchNode startNode = new SearchNode
            {
                state = startState,
                position = startPos,
                rotation = startRot,
                gScore = 0,
                fScore = Vector3.Distance(startPos, target),
                parent = null,
                animationUsed = null
            };

            openSet.Add(startNode);

            int maxIterations = 2000;
            int iterations = 0;

            while (openSet.Count > 0 && iterations < maxIterations)
            {
                iterations++;
                
                int currentIndex = 0;
                for (int i = 1; i < openSet.Count; i++)
                {
                    if (openSet[i].fScore < openSet[currentIndex].fScore)
                    {
                        currentIndex = i;
                    }
                }

                SearchNode current = openSet[currentIndex];
                openSet.RemoveAt(currentIndex);

                // Goal check
                if (Vector3.Distance(current.position, target) <= definition.targetReachThreshold)
                {
                    ReconstructSequence(current, sequence);
                    return true;
                }

                closedSet.Add(current.state);

                // Expand neighbors relying on abstract definition methods
                foreach (var animDef in definition.availableAnimations)
                {
                    Vector3 nextPos = animDef.GetEndPosition(current.position, current.rotation);
                    Quaternion nextRot = animDef.GetEndRotation(current.rotation);
                    DiscreteState nextState = new DiscreteState(nextPos, nextRot);

                    if (closedSet.Contains(nextState)) continue;

                    // Validate via abstraction
                    if (!animDef.CheckValidity(agent, current.position, current.rotation, pathCubes)) continue;

                    float tentativeGScore = current.gScore + Vector3.Distance(current.position, nextPos);

                    SearchNode neighbor = openSet.Find(n => n.state.Equals(nextState));

                    if (neighbor == null)
                    {
                        openSet.Add(new SearchNode
                        {
                            state = nextState,
                            position = nextPos,
                            rotation = nextRot,
                            gScore = tentativeGScore,
                            fScore = tentativeGScore + Vector3.Distance(nextPos, target),
                            parent = current,
                            animationUsed = animDef
                        });
                    }
                    else if (tentativeGScore < neighbor.gScore)
                    {
                        neighbor.gScore = tentativeGScore;
                        neighbor.fScore = tentativeGScore + Vector3.Distance(nextPos, target);
                        neighbor.parent = current;
                        neighbor.animationUsed = animDef;
                    }
                }
            }

            return false;
        }

        private void ReconstructSequence(SearchNode node, List<AgentAnimationDefinition> sequence)
        {
            while (node.parent != null)
            {
                sequence.Add(node.animationUsed);
                node = node.parent;
            }
            sequence.Reverse();
        }

        private void PlayNextAnimation()
        {
            if (!hasPath || currentDefinitionSequence == null || currentAnimationIndex >= currentDefinitionSequence.Count)
            {
                Stop();
                return;
            }

            var nextDef = currentDefinitionSequence[currentAnimationIndex];
            
            // Factory Pattern
            currentPlayingAnimation = nextDef.CreateAnimation(agent.transform.position, agent.transform.rotation);
            currentPlayingAnimation.OnComplete += PlayNextAnimation;
            
            currentAnimationIndex++;
            currentPlayingAnimation.Play(agent);
        }

        private class SearchNode
        {
            public DiscreteState state;
            public Vector3 position;
            public Quaternion rotation;
            public float gScore;
            public float fScore;
            public SearchNode parent;
            public AgentAnimationDefinition animationUsed;
        }

        private readonly struct DiscreteState : System.IEquatable<DiscreteState>
        {
            public readonly int x, y, z;
            public readonly int rx, ry, rz;

            public DiscreteState(Vector3 pos, Quaternion rot)
            {
                float gridRes = 0.1f;
                x = Mathf.RoundToInt(pos.x / gridRes);
                y = Mathf.RoundToInt(pos.y / gridRes);
                z = Mathf.RoundToInt(pos.z / gridRes);

                Vector3 euler = rot.eulerAngles;
                float rotRes = 5f;
                rx = Mathf.RoundToInt(euler.x / rotRes);
                ry = Mathf.RoundToInt(euler.y / rotRes);
                rz = Mathf.RoundToInt(euler.z / rotRes);
            }

            public bool Equals(DiscreteState other)
            {
                return x == other.x && y == other.y && z == other.z &&
                       rx == other.rx && ry == other.ry && rz == other.rz;
            }

            public override bool Equals(object obj) => obj is DiscreteState other && Equals(other);
            
            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + x.GetHashCode();
                    hash = hash * 31 + y.GetHashCode();
                    hash = hash * 31 + z.GetHashCode();
                    hash = hash * 31 + rx.GetHashCode();
                    hash = hash * 31 + ry.GetHashCode();
                    hash = hash * 31 + rz.GetHashCode();
                    return hash;
                }
            }
        }
    }
}
