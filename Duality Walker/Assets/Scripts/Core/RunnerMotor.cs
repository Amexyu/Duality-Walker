using UnityEngine;

namespace DualityWalker.Core
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class RunnerMotor : MonoBehaviour
    {
        [SerializeField] private float runSpeed = 5f;
        [SerializeField] private float maxFallSpeed = -25f;

        private Rigidbody2D rb;
        private bool isRunning;

        public float RunSpeed => runSpeed;
        public bool IsRunning => isRunning;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.freezeRotation = true;
        }

        private void FixedUpdate()
        {
            if (!isRunning)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                return;
            }

            var y = Mathf.Max(rb.linearVelocity.y, maxFallSpeed);
            rb.linearVelocity = new Vector2(runSpeed, y);
        }

        public void SetRunning(bool running)
        {
            isRunning = running;
        }

        public void SetRunSpeed(float speed)
        {
            runSpeed = Mathf.Max(0f, speed);
        }
    }
}
