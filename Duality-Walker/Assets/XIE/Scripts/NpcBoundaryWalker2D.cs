using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class NpcBoundaryWalker2D : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float screenPadding = 0.2f;
    [SerializeField] private bool lockScreenX = true;

    [Header("Blocked")]
    [SerializeField] private float retreatSpeed = 3.5f;
    [SerializeField] private float blockedHoldSeconds = 0.15f;

    [Header("Pit")]
    [SerializeField] private float pitGravityScale = 8f;
    [SerializeField] private float gameOverViewportY = -0.1f;
    [SerializeField] private float gameOverViewportX = -0.05f;

    [Header("Physics")]
    [SerializeField] private float gravityScale = 2.5f;
    [SerializeField] private bool freezeRotation = true;

    [Header("Boundary Line")]
    [SerializeField] private float boundaryOffsetY = 0f;
    [SerializeField] private float spawnLift = 0f;

    private Camera cam;
    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private Collider2D col;

    private bool bootstrapped;
    private float lockedViewportX;
    private float lastBlockedTime = -999f;

    private bool isInPit;
    private BoxCollider2D groundCol;
    private BoundaryGround2D ground;

    private void Awake()
    {
        cam = Camera.main;
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.simulated = true;
        rb.gravityScale = gravityScale;
        rb.constraints = freezeRotation ? RigidbodyConstraints2D.FreezeRotation : RigidbodyConstraints2D.None;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        col.isTrigger = false;
        if (col is BoxCollider2D box && box.size == Vector2.zero)
            box.size = Vector2.one;
    }

    private void Start()
    {
        if (cam == null) cam = Camera.main;
        if (cam != null)
        {
            ground = cam.GetComponent<BoundaryGround2D>();
            if (ground != null) groundCol = ground.GetGroundCollider();
        }
    }

    private void OnEnable()
    {
        bootstrapped = false;
        isInPit = false;
        StartCoroutine(BootstrapToGround());
    }

    private void OnDisable()
    {
        SetIgnoreGround(false);
    }

    private IEnumerator BootstrapToGround()
    {
        yield return new WaitForFixedUpdate();
        SnapToLeftAboveGround();
        rb.linearVelocity = Vector2.zero;

        if (cam == null) cam = Camera.main;
        if (cam != null)
        {
            float z = -cam.transform.position.z;
            Vector3 vp = cam.WorldToViewportPoint(new Vector3(rb.position.x, rb.position.y, z));
            lockedViewportX = vp.x;
        }

        bootstrapped = true;
    }

    private void FixedUpdate()
    {
        if (!bootstrapped) return;
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        bool blocked = !isInPit && (Time.time - lastBlockedTime <= blockedHoldSeconds);

        Vector2 v = rb.linearVelocity;
        if (isInPit)
        {
            v.x = 0f;
        }
        else
        {
            v.x = blocked ? 0f : (lockScreenX ? 0f : moveSpeed);
        }
        rb.linearVelocity = v;

        if (isInPit)
        {
            // 掉坑时不做 X 锁定，保持真实下落
        }
        else if (blocked)
        {
            rb.position = new Vector2(rb.position.x - retreatSpeed * Time.fixedDeltaTime, rb.position.y);
        }
        else if (lockScreenX)
        {
            float z = -cam.transform.position.z;
            Vector3 wp = cam.ViewportToWorldPoint(new Vector3(lockedViewportX, 0f, z));
            rb.position = new Vector2(wp.x, rb.position.y);
        }

        CheckGameOver();
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        RunnerObstacle2D obstacle = collision.collider.GetComponentInParent<RunnerObstacle2D>();
        if (obstacle == null) return;

        if (obstacle.Kind == RunnerObstacle2D.ObstacleKind.Block)
        {
            lastBlockedTime = Time.time;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleTriggerObstacle(other, entering: true);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        HandleTriggerObstacle(other, entering: false);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        RunnerObstacle2D obstacle = other.GetComponentInParent<RunnerObstacle2D>();
        if (obstacle == null) return;

        if (obstacle.Kind == RunnerObstacle2D.ObstacleKind.Pit)
        {
            isInPit = false;
            rb.gravityScale = gravityScale;
            SetIgnoreGround(false);
        }
    }

    private void HandleTriggerObstacle(Collider2D other, bool entering)
    {
        RunnerObstacle2D obstacle = other.GetComponentInParent<RunnerObstacle2D>();
        if (obstacle == null) return;

        if (obstacle.Kind == RunnerObstacle2D.ObstacleKind.Block)
        {
            lastBlockedTime = Time.time;
            return;
        }

        if (obstacle.Kind == RunnerObstacle2D.ObstacleKind.Pit)
        {
            isInPit = true;
            rb.gravityScale = Mathf.Max(gravityScale, pitGravityScale);
            SetIgnoreGround(true);

            if (entering)
                Debug.Log("[NPC] Enter Pit -> 开始下落");
        }
    }

    private void SetIgnoreGround(bool ignore)
    {
        if (groundCol == null)
        {
            if (cam == null) cam = Camera.main;
            if (cam != null)
            {
                if (ground == null) ground = cam.GetComponent<BoundaryGround2D>();
                if (ground != null) groundCol = ground.GetGroundCollider();
            }
        }

        if (groundCol != null && col != null)
            Physics2D.IgnoreCollision(col, groundCol, ignore);
    }

    private void CheckGameOver()
    {
        if (cam == null) return;

        float z = -cam.transform.position.z;
        Vector3 vp = cam.WorldToViewportPoint(new Vector3(transform.position.x, transform.position.y, z));

        if (vp.x < gameOverViewportX || vp.y < gameOverViewportY)
        {
            Debug.Log("[GameOver] NPC 退后或掉坑出屏。");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void SnapToLeftAboveGround()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        float z = -cam.transform.position.z;
        Vector3 vmin = cam.ViewportToWorldPoint(new Vector3(0f, 0f, z));

        float boundaryY = cam.transform.position.y + boundaryOffsetY;
        BoundaryGround2D bg = cam.GetComponent<BoundaryGround2D>();
        if (bg != null) boundaryY = bg.GetGroundTopY();

        float halfW = sr != null ? sr.bounds.extents.x : 0.25f;
        float halfH = sr != null ? sr.bounds.extents.y : 0.25f;

        rb.position = new Vector2(
            vmin.x + screenPadding + halfW,
            boundaryY + halfH + spawnLift
        );
    }
}