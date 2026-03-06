using UnityEngine;
using UnityEngine.Splines;

namespace YanickSenn.Navigation
{
    /// <summary>
    /// Utility component to push the Spline data from a sibling SplineContainer into a given SplineAnimationDefinition.
    /// </summary>
    [RequireComponent(typeof(SplineContainer))]
    [AddComponentMenu("Navigation/Spline Animation Exporter")]
    public class SplineAnimationExporter : MonoBehaviour
    {
        [Tooltip("The Definition asset to export the SplineContainer data into.")]
        public SplineAnimationDefinition targetDefinition;
    }
}
