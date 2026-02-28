using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class RunnerPressureController : MonoBehaviour
{
    [Header("Start / 初始位置（编辑模式对齐用）")]
    [Range(0f, 1f)] public float startViewportX = 0.43f;
    [Range(0f, 1f)] public float centerViewportX = 0.50f;

    [Header("Cooldown / 解除阻挡后的冷却（秒）")]
    public float cooldownAfterClear = 0.6f;

    [Header("Chase / 冷却结束后追到最大位置（带加速度）")]
    public float chaseMaxSpeed = 5.0f;
    public float chaseAccel = 8.0f;
    public float speedGain = 6.0f;

    [Header("Lock Y / 锁定地面Y（整块在白区）")]
    public float groundLineY = 0f;
    public float runnerHalfHeight = 0.5f;
    public bool lockY = true;

    [Header("Sensor / 阻挡探测器（BlockSensorOverlap2D）")]
    public BlockSensorOverlap2D sensor;

    [Header("Pit / 掉坑与爬回参数")]
    public float fallGravity = 25f;
    public float maxFallSpeed = 12f;
    public float climbSpeed = 6f;

    [Header("Pit Floor / 坑底图层（用于触底）")]
    public LayerMask pitFloorMask;      // 只勾 PitFloor
    public float floorRayExtra = 0.1f;  // 射线额外长度（防穿透）

    [Header("Debug / 测试用：按F填坑")]
    public bool debugFillPitWithF = true;

    Rigidbody2D _rb;
    Camera _cam;

    float _xVel;
    bool _wasBlocked;
    float _cooldownTimer;

    // --- pit vertical state ---
    PitController _currentPit;
    float _yVel;
    float _bottomHoldY; // 触底后停在这里

    enum VState { Surface, Falling, Bottom, Climbing }
    VState _vState = VState.Surface;

    float SurfaceY => groundLineY + runnerHalfHeight;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _cam = Camera.main;

        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.gravityScale = 0f;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (!sensor) sensor = GetComponentInChildren<BlockSensorOverlap2D>();

        // 运行时不传送X，只锁Y（避免瞬移）
        if (lockY)
        {
            var p = _rb.position;
            _rb.position = new Vector2(p.x, SurfaceY);
            Physics2D.SyncTransforms();
        }
    }

    void Update()
    {
        if (!debugFillPitWithF) return;

        if (Input.GetKeyDown(KeyCode.F))
        {
            if (_currentPit != null && !_currentPit.IsFilled)
                _currentPit.Fill();
        }
    }

    public void EnterPit(PitController pit)
    {
        if (pit == null) return;

        _currentPit = pit;

        if (!pit.IsFilled)
        {
            _vState = VState.Falling;
            _yVel = 0f;
        }
    }

    void FixedUpdate()
    {
        var gm = GameManager.I;
        if (gm != null && gm.IsGameOver) return;

        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        float dt = Time.fixedDeltaTime;

        float x = _rb.position.x;
        float y = _rb.position.y;

        // 1) 垂直（掉坑/触底/爬回）
        UpdateVertical(dt, x, ref y);

        // 2) 水平（被挡推走 / 冷却 / 加速追到中间）
        UpdateHorizontal(dt, gm);

        x += _xVel * dt;

        // 中间为上限
        float centerX = ViewportXToWorldX(centerViewportX);
        if (x > centerX)
        {
            x = centerX;
            if (_xVel > 0f) _xVel = 0f;
        }

        // 地面状态锁Y
        if (lockY && _vState == VState.Surface)
            y = SurfaceY;

        _rb.MovePosition(new Vector2(x, y));
    }

    void UpdateVertical(float dt, float x, ref float y)
    {
        // 坑填了：爬回
        if (_currentPit != null && _currentPit.IsFilled)
        {
            if (_vState == VState.Falling || _vState == VState.Bottom)
            {
                _vState = VState.Climbing;
                _yVel = 0f;
            }
        }

        switch (_vState)
        {
            case VState.Surface:
                y = lockY ? SurfaceY : y;
                _yVel = 0f;
                break;

            case VState.Falling:
                {
                    // 速度更新
                    _yVel -= fallGravity * dt;
                    _yVel = Mathf.Max(_yVel, -maxFallSpeed);

                    float nextY = y + _yVel * dt;

                    // 用射线找“坑底Floor Collider”，找到就触底
                    if (TryHitPitFloor(x, y, nextY, out float landY))
                    {
                        y = landY;
                        _bottomHoldY = landY;
                        _yVel = 0f;
                        _vState = VState.Bottom;
                    }
                    else
                    {
                        y = nextY;
                    }
                    break;
                }

            case VState.Bottom:
                y = _bottomHoldY; // 坑底等待填坑
                break;

            case VState.Climbing:
                y = Mathf.MoveTowards(y, SurfaceY, climbSpeed * dt);
                if (Mathf.Abs(y - SurfaceY) < 0.001f)
                {
                    y = SurfaceY;
                    _vState = VState.Surface;

                    if (_currentPit != null)
                    {
                        _currentPit.ReleaseAfterRecovered();
                        _currentPit = null;
                    }
                }
                break;
        }
    }

    bool TryHitPitFloor(float x, float y, float nextY, out float landY)
    {
        landY = 0f;

        if (_currentPit == null) return false;
        if (pitFloorMask.value == 0) return false; // 没设置图层

        // 预计下落距离
        float downDist = Mathf.Abs(nextY - y) + runnerHalfHeight + floorRayExtra;

        // 从当前脚底附近往下射线
        Vector2 origin = new Vector2(x, y);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, downDist, pitFloorMask);

        if (!hit.collider) return false;

        // 只接受“属于当前坑”的坑底（避免打到别的坑的floor）
        if (hit.collider.transform.root != _currentPit.transform.root) return false;

        // 命中点是floor表面，NPC中心=表面+半高
        landY = hit.point.y + runnerHalfHeight;
        return true;
    }

    void UpdateHorizontal(float dt, GameManager gm)
    {
        bool blockedBySensor = (sensor != null && sensor.IsBlocked);

        // 在坑里（下落/坑底）时：视为被挡，继续被推走（否则会停在坑里太“假”）
        bool inPitStuck = (_vState == VState.Falling || _vState == VState.Bottom);
        bool climbing = (_vState == VState.Climbing);

        bool blocked = blockedBySensor || inPitStuck;

        if (blocked)
        {
            _cooldownTimer = cooldownAfterClear;

            float scroll = gm != null ? gm.CurrentSpeed : 0f;
            _xVel = -scroll;
        }
        else
        {
            // 爬回地面时，不追赶
            if (climbing)
            {
                if (_cooldownTimer > 0f) _cooldownTimer -= dt;
                _xVel = 0f;
                _wasBlocked = blocked;
                return;
            }

            if (_wasBlocked) _xVel = 0f;

            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= dt;
                _xVel = 0f;
            }
            else
            {
                float centerX = ViewportXToWorldX(centerViewportX);
                float x = _rb.position.x;

                float error = centerX - x;
                float desiredVel = Mathf.Clamp(error * speedGain, -chaseMaxSpeed, chaseMaxSpeed);
                _xVel = Mathf.MoveTowards(_xVel, desiredVel, chaseAccel * dt);
            }
        }

        _wasBlocked = blocked;
    }

    float ViewportXToWorldX(float vx)
    {
        float z = Mathf.Abs(_cam.transform.position.z - transform.position.z);
        return _cam.ViewportToWorldPoint(new Vector3(vx, 0.5f, z)).x;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying) return;

        var cam = Camera.main;
        if (cam == null) return;

        float z = Mathf.Abs(cam.transform.position.z - transform.position.z);
        float x = cam.ViewportToWorldPoint(new Vector3(startViewportX, 0.5f, z)).x;
        float y = lockY ? SurfaceY : transform.position.y;

        transform.position = new Vector3(x, y, transform.position.z);
    }
#endif
}