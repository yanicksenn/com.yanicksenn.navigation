using UnityEngine;
using UnityEngine.Splines;
using YanickSenn.Utils.Variables;

namespace YanickSenn.Navigation {
    /// <summary>
    /// Utility component to push the Spline data from a sibling SplineContainer into a given SplineAnimationDefinition.
    /// </summary>
    [RequireComponent(typeof(SplineContainer))]
    public class SplineAnimationExporter : MonoBehaviour {
        [Tooltip("The asset to export the Spline data into.")]
        public SplineVariable target;
    }
}
