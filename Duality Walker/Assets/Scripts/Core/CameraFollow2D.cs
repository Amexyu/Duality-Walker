using UnityEngine;

namespace DualityWalker.Core
{
    public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new(6f, 1.5f, -10f);
        [SerializeField] private float smoothTime = 0.08f;
        [SerializeField] private float deadZoneX = 0.5f;
        [SerializeField] private bool lockY = true;
        [SerializeField] private float fixedY = 1.5f;

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

            if (lockY)
            {
                desired.y = fixedY;
            }

            transform.position = Vector3.SmoothDamp(current, desired, ref velocity, smoothTime);
        }
    }
}
