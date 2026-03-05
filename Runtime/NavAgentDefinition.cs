using UnityEngine;

namespace YanickSenn.Navigation
{
    public abstract class NavAgentDefinition : ScriptableObject
    {
        public abstract NavAgentStrategy CreateStrategy(NavMeshAgent agent);
    }
}
