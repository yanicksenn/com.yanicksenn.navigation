using UnityEngine;

namespace YanickSenn.Navigation {
    public struct NavTarget {
        public Vector3 position;
        public Quaternion rotation;
        public bool hasRotation;

        public NavTarget(Vector3 position) {
            this.position = position;
            this.rotation = Quaternion.identity;
            this.hasRotation = false;
        }

        public NavTarget(Vector3 position, Quaternion rotation) {
            this.position = position;
            this.rotation = rotation;
            this.hasRotation = true;
        }

        public static implicit operator NavTarget(Vector3 position) => new NavTarget(position);
        public static implicit operator Vector3(NavTarget target) => target.position;
    }
}
