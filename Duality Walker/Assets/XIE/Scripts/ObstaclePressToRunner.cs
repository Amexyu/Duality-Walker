using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ObstaclePressToRunner : MonoBehaviour
{
    [Header("要顶住的NPC")]
    public Transform runner;

    [Header("锁定在NPC前方的距离")]
    public float lockOffsetX = 0.9f;

    [Header("是否锁定Y（让障碍固定在白区某高度）")]
    public bool lockY = false;
    public float lockedY = 0.5f;

    Rigidbody2D _rb;
    bool _locked;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.gravityScale = 0f;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void FixedUpdate()
    {
        var gm = GameManager.I;
        if (gm != null && gm.IsGameOver) return;

        float speed = gm != null ? gm.CurrentSpeed : 0f;
        float dt = Time.fixedDeltaTime;

        Vector2 pos = _rb.position;

        // runner 没绑就只左移
        if (runner == null)
        {
            pos += Vector2.left * speed * dt;
            if (lockY) pos.y = lockedY;
            _rb.MovePosition(pos);
            return;
        }

        float targetX = runner.position.x + lockOffsetX;

        if (!_locked)
        {
            // 先正常左移靠近NPC
            pos += Vector2.left * speed * dt;

            // 一旦到达NPC前方位置，就锁定（开始“顶住”）
            if (pos.x <= targetX)
                _locked = true;
        }

        if (_locked)
        {
            // 始终贴在NPC前面（NPC后退时障碍也跟着压着）
            pos.x = targetX;
        }

        if (lockY) pos.y = lockedY;

        _rb.MovePosition(pos);
    }
}