using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BLACKMOVE : MonoBehaviour
{
    [Header("移动")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float maxFallSpeed = 20f;

    [Header("回中加速")]
    [SerializeField] private float boostMoveSpeed = 6f;
    [SerializeField] private float accelerateRate = 10f;
    [SerializeField] private float decelerateRate = 8f;
    [SerializeField] private float unblockedDelay = 0.5f;
    [SerializeField] private float blockedProgressRatio = 0.35f;
    [SerializeField] private float catchupLagX = 1.2f;

    [Header("接地判定（仅接地时回中加速）")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundedCheckDistance = 0.06f;
    [SerializeField] private float groundedSkin = 0.04f;

    [Header("相机跟随")]
    [SerializeField] private float lookAheadX = 4f;
    [SerializeField] private float cameraSmoothTimeX = 0.12f;
    [SerializeField] private float cameraSmoothTimeY = 0.12f;
    [SerializeField] private bool lockCameraY = true;
    [SerializeField] private bool followDownWhenFalling = true;
    [SerializeField] private float followDownOffsetY = 1.2f;
    [SerializeField] private float followDownTriggerY = -0.2f;

    [Header("相机强制前进（NPC卡住时画面继续移动）")]
    [SerializeField] private bool forceCameraForward = true;
    [SerializeField] private float cameraForwardSpeed = 3f;

    [Header("玩家显示层级")]
    [SerializeField] private int playerSortingOrder = 50;

    private Rigidbody2D rb;
    private Camera mainCamera;
    private Collider2D bodyCol;

    private float camVelX;
    private float camVelY;
    private float baseCamY;
    private float baseCamZ;
    private float cameraTrackX;

    private float currentMoveSpeed;
    private float unblockedTime;
    private float lastX;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCol = GetComponent<Collider2D>();
        if (bodyCol == null)
        {
            bodyCol = GetComponentInChildren<Collider2D>();
        }

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.simulated = true;
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var mat = new PhysicsMaterial2D("PlayerNoFriction");
        mat.friction = 0f;
        mat.bounciness = 0f;

        var cols = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < cols.Length; i++)
        {
            cols[i].sharedMaterial = mat;
        }

        currentMoveSpeed = moveSpeed;
        lastX = rb.position.x;

        ApplyPlayerSortingOrder();
    }

    private void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        baseCamY = mainCamera.transform.position.y;
        baseCamZ = mainCamera.transform.position.z;
        cameraTrackX = rb.position.x + lookAheadX;
    }

    private void FixedUpdate()
    {
        float currentX = rb.position.x;
        float expectedMove = currentMoveSpeed * Time.fixedDeltaTime;
        float actualMove = Mathf.Max(0f, currentX - lastX);

        bool blocked = expectedMove > 0.0001f && actualMove < expectedMove * blockedProgressRatio;
        if (blocked)
        {
            unblockedTime = 0f;
        }
        else
        {
            unblockedTime += Time.fixedDeltaTime;
        }

        float targetSpeed = moveSpeed;
        if (unblockedTime >= unblockedDelay && IsBehindCenter() && IsGrounded())
        {
            targetSpeed = boostMoveSpeed;
        }

        float rate = targetSpeed > currentMoveSpeed ? accelerateRate : decelerateRate;
        currentMoveSpeed = Mathf.MoveTowards(currentMoveSpeed, targetSpeed, rate * Time.fixedDeltaTime);

        var v = rb.linearVelocity;
        v.x = currentMoveSpeed;
        v.y = Mathf.Max(v.y, -maxFallSpeed);
        rb.linearVelocity = v;

        lastX = rb.position.x;
    }

    private bool IsBehindCenter()
    {
        if (mainCamera == null)
        {
            return false;
        }

        float desiredPlayerX = mainCamera.transform.position.x - lookAheadX;
        float lag = desiredPlayerX - rb.position.x;
        return lag > catchupLagX;
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
        {
            return;
        }

        var camPos = mainCamera.transform.position;

        float playerX = rb.position.x;
        float playerY = rb.position.y;

        var followX = playerX + lookAheadX;
        if (forceCameraForward)
        {
            cameraTrackX += cameraForwardSpeed * Time.deltaTime;
            followX = Mathf.Max(followX, cameraTrackX);
        }

        camPos.x = Mathf.SmoothDamp(camPos.x, followX, ref camVelX, cameraSmoothTimeX);

        var targetY = baseCamY;
        if (lockCameraY && followDownWhenFalling)
        {
            if (playerY < baseCamY + followDownTriggerY)
            {
                targetY = Mathf.Min(baseCamY, playerY + followDownOffsetY);
            }
        }
        else if (!lockCameraY)
        {
            targetY = playerY;
        }

        camPos.y = Mathf.SmoothDamp(camPos.y, targetY, ref camVelY, cameraSmoothTimeY);
        camPos.z = baseCamZ;

        mainCamera.transform.position = camPos;
    }

    private void ApplyPlayerSortingOrder()
    {
        var renderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingOrder = playerSortingOrder;
        }
    }

    private bool IsGrounded()
    {
        if (bodyCol == null)
        {
            return false;
        }

        Bounds b = bodyCol.bounds;
        Vector2 size = new Vector2(
            Mathf.Max(0.01f, b.size.x - groundedSkin),
            Mathf.Max(0.01f, b.size.y * 0.15f)
        );

        Vector2 origin = new Vector2(b.center.x, b.min.y + size.y * 0.5f);
        RaycastHit2D hit = Physics2D.BoxCast(origin, size, 0f, Vector2.down, groundedCheckDistance, groundMask);
        return hit.collider != null;
    }
}