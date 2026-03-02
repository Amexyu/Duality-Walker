using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BLACKMOVE : MonoBehaviour
{
    [Header("移动")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float maxFallSpeed = 20f;

    [Header("相机")]
    [SerializeField] private float lookAheadX = 4f;
    [SerializeField] private float cameraSmoothTimeX = 0.12f;
    [SerializeField] private float cameraSmoothTimeY = 0.12f;
    [SerializeField] private bool lockCameraY = true;
    [SerializeField] private bool followDownWhenFalling = true;
    [SerializeField] private float followDownOffsetY = 1.2f; // 玩家在屏幕中稍偏下
    [SerializeField] private float followDownTriggerY = -0.2f;

    [Header("相机前推")]
    [SerializeField] private bool keepCameraMovingForward = true;
    [SerializeField] private float cameraForwardSpeed = 3f;

    [Header("失败判定")]
    [SerializeField] private bool gameOverWhenOutOfView = true;
    [SerializeField] private float outOfViewViewportMargin = 0.02f;
    [SerializeField] private bool freezeTimeOnGameOver = true;

    [Header("玩家显示层级")]
    [SerializeField] private int playerSortingOrder = 50;

    [Header("帧动画")]
    [SerializeField] private SpriteRenderer animatedRenderer;
    [SerializeField] private Sprite[] walkFrames;
    [SerializeField] private float walkFps = 8f;
    [SerializeField] private Sprite[] runFrames;
    [SerializeField] private float runFps = 14f;
    [SerializeField] private float runEnterDelay = 7f;
    [SerializeField] private float uninterruptedMoveMinSpeed = 0.1f;

    private Rigidbody2D rb;
    private Camera mainCamera;

    private float camVelX;
    private float camVelY;
    private float baseCamY;
    private float baseCamZ;
    private float cameraForwardX;

    private Sprite[] currentFrames;
    private float animTimer;
    private int animIndex;

    private float uninterruptedMoveTimer;
    private bool isRunningMode;
    private float lastPosX;
    private bool hasLastPosSample;
    private bool isGameOver;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        PhysicsMaterial2D mat = new PhysicsMaterial2D("PlayerNoFriction");
        mat.friction = 0f;
        mat.bounciness = 0f;

        Collider2D[] cols = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < cols.Length; i++)
        {
            cols[i].sharedMaterial = mat;
        }

        if (animatedRenderer == null)
        {
            animatedRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }

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
        cameraForwardX = mainCamera.transform.position.x;

        lastPosX = transform.position.x;
        hasLastPosSample = true;
    }

    private void Update()
    {
        if (isGameOver)
        {
            return;
        }

        UpdateRunState(Time.deltaTime);
        UpdateFrameAnimation(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        if (isGameOver)
        {
            return;
        }

        Vector2 v = rb.linearVelocity;
        v.x = moveSpeed;
        v.y = Mathf.Max(v.y, -maxFallSpeed);
        rb.linearVelocity = v;
    }

    private void LateUpdate()
    {
        if (mainCamera == null || isGameOver)
        {
            return;
        }

        Vector3 camPos = mainCamera.transform.position;

        if (keepCameraMovingForward)
        {
            cameraForwardX += cameraForwardSpeed * Time.deltaTime;
        }

        float targetXByPlayer = transform.position.x + lookAheadX;
        float targetX = keepCameraMovingForward ? Mathf.Max(targetXByPlayer, cameraForwardX) : targetXByPlayer;
        camPos.x = Mathf.SmoothDamp(camPos.x, targetX, ref camVelX, cameraSmoothTimeX);

        float targetY = baseCamY;
        if (lockCameraY && followDownWhenFalling)
        {
            if (transform.position.y < baseCamY + followDownTriggerY)
            {
                targetY = Mathf.Min(baseCamY, transform.position.y + followDownOffsetY);
            }
        }
        else if (!lockCameraY)
        {
            targetY = transform.position.y;
        }

        camPos.y = Mathf.SmoothDamp(camPos.y, targetY, ref camVelY, cameraSmoothTimeY);
        camPos.z = baseCamZ;

        mainCamera.transform.position = camPos;

        CheckGameOverByOutOfView();
    }

    private void CheckGameOverByOutOfView()
    {
        if (!gameOverWhenOutOfView || mainCamera == null)
        {
            return;
        }

        Vector3 vp = mainCamera.WorldToViewportPoint(transform.position);
        float min = -outOfViewViewportMargin;
        float max = 1f + outOfViewViewportMargin;

        bool outOfView = vp.z < 0f || vp.x < min || vp.x > max || vp.y < min || vp.y > max;
        if (!outOfView)
        {
            return;
        }

        TriggerGameOver();
    }

    private void TriggerGameOver()
    {
        if (isGameOver)
        {
            return;
        }

        isGameOver = true;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        if (freezeTimeOnGameOver)
        {
            Time.timeScale = 0f;
        }

        Debug.Log("Game Over: 玩家已离开摄像机可视范围。");
    }

    private void UpdateRunState(float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            return;
        }

        float currentPosX = transform.position.x;
        if (!hasLastPosSample)
        {
            lastPosX = currentPosX;
            hasLastPosSample = true;
            return;
        }

        float movedDistanceX = Mathf.Abs(currentPosX - lastPosX);
        lastPosX = currentPosX;

        bool movingWithoutInterruption = movedDistanceX >= uninterruptedMoveMinSpeed * deltaTime;

        if (movingWithoutInterruption)
        {
            uninterruptedMoveTimer += deltaTime;
            if (!isRunningMode && uninterruptedMoveTimer >= runEnterDelay)
            {
                isRunningMode = true;
            }
        }
        else
        {
            uninterruptedMoveTimer = 0f;
            isRunningMode = false;
        }
    }

    private void UpdateFrameAnimation(float deltaTime)
    {
        if (animatedRenderer == null)
        {
            return;
        }

        Sprite[] targetFrames = isRunningMode ? runFrames : walkFrames;
        float targetFps = isRunningMode ? runFps : walkFps;

        if (targetFrames == null || targetFrames.Length == 0 || targetFps <= 0f)
        {
            return;
        }

        if (!ReferenceEquals(currentFrames, targetFrames))
        {
            currentFrames = targetFrames;
            animIndex = 0;
            animTimer = 0f;
            animatedRenderer.sprite = currentFrames[animIndex];
        }

        float frameDuration = 1f / targetFps;
        animTimer += deltaTime;

        while (animTimer >= frameDuration)
        {
            animTimer -= frameDuration;
            animIndex = (animIndex + 1) % currentFrames.Length;
            animatedRenderer.sprite = currentFrames[animIndex];
        }
    }

    private void ApplyPlayerSortingOrder()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingOrder = playerSortingOrder;
        }
    }
}
