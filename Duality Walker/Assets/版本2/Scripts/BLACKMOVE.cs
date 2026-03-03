using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    [Header("Animator参数动画（可选）")]
    [SerializeField] private Animator animator;
    [SerializeField] private string animSpeedXParam = "SpeedX";
    [SerializeField] private string animSpeedYParam = "SpeedY";
    [SerializeField] private string animGroundedParam = "Grounded";
    [SerializeField] private string animFallingParam = "Falling";
    [SerializeField] private string animBlockedParam = "Blocked";
    [SerializeField] private string animBoostParam = "Boost";

    [Header("RUN帧动画（来自STARTUINPC）")]
    [SerializeField] private bool useRunFrameAnimation = true;
    [SerializeField] private Image targetImage;
    [SerializeField] private SpriteRenderer targetSpriteRenderer;
    [SerializeField] private Sprite[] runFrames;
    [SerializeField] private float runFps = 12f;
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private float runMinSpeedX = 0.05f;

    [Header("WALK帧动画（可选）")]
    [SerializeField] private Sprite[] walkFrames;
    [SerializeField] private float walkFps = 8f;
    [SerializeField] private float walkMinSpeedX = 0.01f;
    [SerializeField] private float runSwitchSpeedX = 2.5f;
    [SerializeField] private float runAfterUnblockedSeconds = 7f;

    [Header("死亡/结算")]
    [SerializeField] private string killLineTag = "KillLine";
    [SerializeField] private string stopUiSceneName = "StopUI";
    [SerializeField] private bool disableTimeScaleOnStop = true;

    [SerializeField] private float startMoveDelay = 0.02f;

    private enum FrameAnimMode
    {
        None,
        Walk,
        Run
    }

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
    private float startMoveTimer;

    private int animSpeedXHash;
    private int animSpeedYHash;
    private int animGroundedHash;
    private int animFallingHash;
    private int animBlockedHash;
    private int animBoostHash;

    private bool hasAnimSpeedX;
    private bool hasAnimSpeedY;
    private bool hasAnimGrounded;
    private bool hasAnimFalling;
    private bool hasAnimBlocked;
    private bool hasAnimBoost;

    private int currentRunFrameIndex;
    private float runFrameTimer;
    private bool isPlayingRun;
    private FrameAnimMode currentFrameAnimMode = FrameAnimMode.None;

    private bool isBlockedThisStep;
    private bool isGameStopped;
    private float uninterruptedMoveTime;
    private bool runByUnblockedTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCol = GetComponent<Collider2D>();
        if (bodyCol == null)
        {
            bodyCol = GetComponentInChildren<Collider2D>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }

        if (targetSpriteRenderer == null)
        {
            targetSpriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (targetSpriteRenderer == null)
        {
            targetSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
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

        CacheAnimatorParams();
        ApplyPlayerSortingOrder();
    }

    private void ResetRuntimeState()
    {
        isGameStopped = false;
        isBlockedThisStep = false;
        unblockedTime = 0f;
        currentMoveSpeed = moveSpeed;
        uninterruptedMoveTime = 0f;
        runByUnblockedTime = false;

        if (rb != null)
        {
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        lastX = rb != null ? rb.position.x : transform.position.x;
        startMoveTimer = Mathf.Max(0f, startMoveDelay);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        ResetRuntimeState();

        if (useRunFrameAnimation)
        {
            PlayRun();
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        StopRun();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == stopUiSceneName)
        {
            return;
        }

        Time.timeScale = 1f;
        AudioListener.pause = false;
        ResetRuntimeState();
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

    private void Update()
    {
        if (!useRunFrameAnimation)
        {
            return;
        }

        float speedXAbs = Mathf.Abs(rb.linearVelocity.x);
        bool isMovingByVelocity = speedXAbs > walkMinSpeedX;
        bool shouldPlay = isMovingByVelocity || isBlockedThisStep;

        if (!shouldPlay)
        {
            StopRun();
            return;
        }

        // 改为：连续未受阻移动达到阈值才进入 Run
        bool shouldRun = runByUnblockedTime;
        if (shouldRun)
        {
            if (!isPlayingRun || currentFrameAnimMode != FrameAnimMode.Run)
            {
                PlayRun();
            }
        }
        else
        {
            if (!isPlayingRun || currentFrameAnimMode != FrameAnimMode.Walk)
            {
                PlayWalk();
            }
        }

        UpdateRunAnimation();
    }

    private void FixedUpdate()
    {
        if (isGameStopped)
        {
            return;
        }

        if (startMoveTimer > 0f)
        {
            startMoveTimer -= Time.fixedDeltaTime;
            lastX = rb.position.x;
            return;
        }

        float currentX = rb.position.x;
        float expectedMove = currentMoveSpeed * Time.fixedDeltaTime;
        float actualMove = Mathf.Max(0f, currentX - lastX);

        bool blocked = expectedMove > 0.0001f && actualMove < expectedMove * blockedProgressRatio;
        isBlockedThisStep = blocked;

        if (blocked)
        {
            unblockedTime = 0f;
        }
        else
        {
            unblockedTime += Time.fixedDeltaTime;
        }

        // 连续未受阻移动计时：被阻挡或几乎不动就清零
        bool isMovingForward = actualMove > 0.0001f;
        if (!blocked && isMovingForward)
        {
            uninterruptedMoveTime += Time.fixedDeltaTime;
        }
        else
        {
            uninterruptedMoveTime = 0f;
        }

        runByUnblockedTime = uninterruptedMoveTime >= runAfterUnblockedSeconds;

        bool grounded = IsGrounded();

        float targetSpeed = moveSpeed;
        if (unblockedTime >= unblockedDelay && IsBehindCenter() && grounded)
        {
            targetSpeed = boostMoveSpeed;
        }

        float rate = targetSpeed > currentMoveSpeed ? accelerateRate : decelerateRate;
        currentMoveSpeed = Mathf.MoveTowards(currentMoveSpeed, targetSpeed, rate * Time.fixedDeltaTime);

        var v = rb.linearVelocity;
        v.x = currentMoveSpeed;
        v.y = Mathf.Max(v.y, -maxFallSpeed);
        rb.linearVelocity = v;

        if (!useRunFrameAnimation)
        {
            bool boosting = targetSpeed > moveSpeed + 0.001f;
            UpdateAnimation(v, grounded, blocked, boosting);
        }

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

        float followX = playerX + lookAheadX;
        if (forceCameraForward)
        {
            cameraTrackX += cameraForwardSpeed * Time.deltaTime;
            followX = Mathf.Max(followX, cameraTrackX);
        }

        camPos.x = Mathf.SmoothDamp(camPos.x, followX, ref camVelX, cameraSmoothTimeX);

        float targetY = baseCamY;
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

    private void CacheAnimatorParams()
    {
        if (animator == null)
        {
            return;
        }

        animSpeedXHash = Animator.StringToHash(animSpeedXParam);
        animSpeedYHash = Animator.StringToHash(animSpeedYParam);
        animGroundedHash = Animator.StringToHash(animGroundedParam);
        animFallingHash = Animator.StringToHash(animFallingParam);
        animBlockedHash = Animator.StringToHash(animBlockedParam);
        animBoostHash = Animator.StringToHash(animBoostParam);

        var parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            string name = parameters[i].name;
            if (name == animSpeedXParam) hasAnimSpeedX = true;
            if (name == animSpeedYParam) hasAnimSpeedY = true;
            if (name == animGroundedParam) hasAnimGrounded = true;
            if (name == animFallingParam) hasAnimFalling = true;
            if (name == animBlockedParam) hasAnimBlocked = true;
            if (name == animBoostParam) hasAnimBoost = true;
        }
    }

    private void UpdateAnimation(Vector2 velocity, bool grounded, bool blocked, bool boosting)
    {
        if (animator == null)
        {
            return;
        }

        if (hasAnimSpeedX) animator.SetFloat(animSpeedXHash, Mathf.Abs(velocity.x));
        if (hasAnimSpeedY) animator.SetFloat(animSpeedYHash, velocity.y);
        if (hasAnimGrounded) animator.SetBool(animGroundedHash, grounded);
        if (hasAnimFalling) animator.SetBool(animFallingHash, velocity.y < -0.01f && !grounded);
        if (hasAnimBlocked) animator.SetBool(animBlockedHash, blocked);
        if (hasAnimBoost) animator.SetBool(animBoostHash, boosting);
    }

    private void PlayWalk()
    {
        if (walkFrames != null && walkFrames.Length > 0)
        {
            StartFrameAnimation(FrameAnimMode.Walk, walkFrames, walkFps);
            return;
        }

        PlayRun();
    }

    private void PlayRun()
    {
        if (runFrames == null || runFrames.Length == 0)
        {
            isPlayingRun = false;
            currentFrameAnimMode = FrameAnimMode.None;
            Debug.LogWarning("[BLACKMOVE] runFrames 为空，无法播放。", this);
            return;
        }

        StartFrameAnimation(FrameAnimMode.Run, runFrames, runFps);
    }

    private void StartFrameAnimation(FrameAnimMode mode, Sprite[] frames, float fps)
    {
        if (targetImage == null && targetSpriteRenderer == null)
        {
            isPlayingRun = false;
            currentFrameAnimMode = FrameAnimMode.None;
            Debug.LogWarning("[BLACKMOVE] 未找到 Image 或 SpriteRenderer。", this);
            return;
        }

        if (frames == null || frames.Length == 0)
        {
            isPlayingRun = false;
            currentFrameAnimMode = FrameAnimMode.None;
            return;
        }

        currentFrameAnimMode = mode;
        isPlayingRun = true;
        currentRunFrameIndex = 0;
        runFrameTimer = 0f;
        SetFrame(frames[currentRunFrameIndex]);
    }

    private void StopRun()
    {
        isPlayingRun = false;
        currentFrameAnimMode = FrameAnimMode.None;
    }

    private void UpdateRunAnimation()
    {
        if (!isPlayingRun)
        {
            return;
        }

        Sprite[] frames = null;
        float fps = 0f;

        if (currentFrameAnimMode == FrameAnimMode.Run)
        {
            frames = runFrames;
            fps = runFps;
        }
        else if (currentFrameAnimMode == FrameAnimMode.Walk)
        {
            bool hasWalk = walkFrames != null && walkFrames.Length > 0;
            frames = hasWalk ? walkFrames : runFrames;
            fps = hasWalk ? walkFps : runFps;
        }

        if (frames == null || frames.Length == 0)
        {
            return;
        }

        if (fps <= 0f)
        {
            fps = 1f;
        }

        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float frameDuration = 1f / fps;

        runFrameTimer += deltaTime;

        while (runFrameTimer >= frameDuration)
        {
            runFrameTimer -= frameDuration;
            currentRunFrameIndex = (currentRunFrameIndex + 1) % frames.Length;
            SetFrame(frames[currentRunFrameIndex]);
        }
    }

    private void SetFrame(Sprite frame)
    {
        if (targetImage != null)
        {
            targetImage.sprite = frame;
        }

        if (targetSpriteRenderer != null)
        {
            targetSpriteRenderer.sprite = frame;
        }
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

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryEnterStopUi(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryEnterStopUi(collision.gameObject);
    }

    private void TryEnterStopUi(GameObject other)
    {
        if (isGameStopped || other == null)
        {
            return;
        }

        if (!other.CompareTag(killLineTag))
        {
            return;
        }

        if (string.IsNullOrEmpty(stopUiSceneName))
        {
            Debug.LogWarning("[BLACKMOVE] stopUiSceneName 为空。", this);
            return;
        }

        isGameStopped = true;
        StopRun();

        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        if (disableTimeScaleOnStop)
        {
            Time.timeScale = 0f;
        }

        SceneManager.LoadScene(stopUiSceneName, LoadSceneMode.Single);
    }
}