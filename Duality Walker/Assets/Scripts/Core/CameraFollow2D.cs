using UnityEngine;

namespace DualityWalker.Core
{
    public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new(3f, 1.5f, -10f);
        [SerializeField] private float smoothTime = 0.15f;
        [SerializeField] private float deadZoneX = 1f;

        private Vector3 velocity;

        public void SetTarget(Transform followTarget)
        {
            target = followTarget;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            var current = transform.position;
            var desired = target.position + offset;

            if (Mathf.Abs(desired.x - current.x) < deadZoneX)
            {
                desired.x = current.x;
            }

            transform.position = Vector3.SmoothDamp(current, desired, ref velocity, smoothTime);
        }
    }
}
