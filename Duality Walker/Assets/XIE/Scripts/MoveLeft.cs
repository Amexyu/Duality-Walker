using UnityEngine;

public class MoveLeft : MonoBehaviour
{
    [Header("可选：自动加刚体2D来稳定物理同步")]
    public bool useRigidbody = true;

    [Header("离屏销毁（后面生成器用）")]
    public bool destroyOffscreen = false;
    public float destroyX = -30f;

    Rigidbody2D _rb;

    void Awake()
    {
        if (useRigidbody)
        {
            _rb = GetComponent<Rigidbody2D>();
            if (_rb == null) _rb = gameObject.AddComponent<Rigidbody2D>();

            _rb.bodyType = RigidbodyType2D.Kinematic;   // 中文：主体类型=运动学
            _rb.gravityScale = 0f;                      // 重力缩放=0
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate; // 插值=插值
        }
    }

    void FixedUpdate()
    {
        var gm = GameManager.I;
        if (gm != null && gm.IsGameOver) return;

        float speed = gm != null ? gm.CurrentSpeed : 0f;
        Vector2 delta = Vector2.left * speed * Time.fixedDeltaTime;

        if (_rb != null)
            _rb.MovePosition(_rb.position + delta);
        else
            transform.position += (Vector3)delta;

        if (destroyOffscreen && transform.position.x < destroyX)
            Destroy(gameObject);
    }
}