using UnityEngine;

public class RunnerMotor : MonoBehaviour
{
    [SerializeField] private float runSpeed = 4f;

    private Rigidbody2D rb;
    private bool isRunning = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }
    }

    private void FixedUpdate()
    {
        if (rb == null)
        {
            return;
        }

        if (!isRunning)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            return;
        }

        // Ignore any manual horizontal input and force auto-run.
        rb.velocity = new Vector2(runSpeed, rb.velocity.y);
    }

    public void SetRunning(bool running)
    {
        isRunning = running;

        if (!isRunning && rb != null)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
        }
    }
}
