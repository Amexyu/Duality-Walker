using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PitController : MonoBehaviour
{
    [Header("Runner（NPC）")]
    public Transform runner;

    [Header("到达NPC后锁定在NPC的X偏移")]
    public float lockOffsetX = 0f;

    [Header("填坑后是否自动销毁（由Runner爬回地面后触发销毁）")]
    public bool destroyWhenFilled = true;

    public bool IsFilled { get; private set; }

    Rigidbody2D _rb;
    BoxCollider2D _trigger;
    MoveLeft _moveLeft;
    bool _locked;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.gravityScale = 0f;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        _trigger = GetComponent<BoxCollider2D>();
        _trigger.isTrigger = true;

        _moveLeft = GetComponent<MoveLeft>();
    }

    void FixedUpdate()
    {
        var gm = GameManager.I;
        if (gm != null && gm.IsGameOver) return;
        if (IsFilled) return;
        if (runner == null) return;

        float targetX = runner.position.x + lockOffsetX;

        // 坑到达NPC位置后锁住（不再继续往左跑）
        if (!_locked && transform.position.x <= targetX)
        {
            _locked = true;
            if (_moveLeft) _moveLeft.enabled = false;
        }

        // 锁住后跟随NPC（NPC被推走时坑也跟着，避免“坑跑掉”）
        if (_locked)
        {
            var p = _rb.position;
            p.x = targetX;
            _rb.MovePosition(p);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (IsFilled) return;
        if (!other.CompareTag("Player")) return;

        var runnerCtrl = other.GetComponent<RunnerPressureController>();
        if (runnerCtrl != null)
            runnerCtrl.EnterPit(this);
    }

    /// <summary>外部（未来你的拼块系统）调用：填坑</summary>
    public void Fill()
    {
        if (IsFilled) return;
        IsFilled = true;

        // 不再触发掉坑
        if (_trigger) _trigger.enabled = false;

        // 视觉占位：先把坑“涂黑”当作填平（你后面换成素材即可）
        var sr = GetComponent<SpriteRenderer>();
        if (sr) sr.color = Color.black;
    }

    /// <summary>Runner 爬回地面后调用：销毁/释放坑</summary>
    public void ReleaseAfterRecovered()
    {
        if (destroyWhenFilled)
            Destroy(gameObject);
        else
        {
            // 不销毁就让它继续滚动离开（可选）
            if (_moveLeft) _moveLeft.enabled = true;
            _locked = false;
        }
    }
}