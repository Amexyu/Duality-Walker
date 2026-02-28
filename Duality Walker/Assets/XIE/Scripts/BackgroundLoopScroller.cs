using UnityEngine;

public class BackgroundLoopScroller : MonoBehaviour
{
    [Header("两个背景段（Transform）")]
    public Transform segA;
    public Transform segB;

    [Header("一段背景的宽度（世界单位）")]
    public float segmentWidth = 20f; // 你之前把方块拉伸成 20，就填 20

    [Header("像素/网格对齐（防止累计误差）")]
    public float snapUnit = 0.01f;   // 0=不对齐；建议 0.01 或 0.001

    Camera _cam;

    void Awake()
    {
        _cam = Camera.main;
    }

    void FixedUpdate()
    {
        var gm = GameManager.I;
        if (gm != null && gm.IsGameOver) return;
        if (segA == null || segB == null) return;

        float speed = gm != null ? gm.CurrentSpeed : 0f;
        float dt = Time.fixedDeltaTime;

        // 1) 统一移动
        Vector3 delta = Vector3.left * speed * dt;
        segA.position += delta;
        segB.position += delta;

        // 2) 计算相机左边界（用于判断哪段出屏）
        if (_cam == null) _cam = Camera.main;
        float camLeft = 0f;
        if (_cam != null)
        {
            float z = Mathf.Abs(_cam.transform.position.z);
            camLeft = _cam.ViewportToWorldPoint(new Vector3(0f, 0.5f, z)).x;
        }

        // 3) 循环：谁的“右边缘”跑到相机左边，就把它放到另一段右侧紧贴
        float rightA = segA.position.x + segmentWidth * 0.5f;
        float rightB = segB.position.x + segmentWidth * 0.5f;

        if (rightA < camLeft)
            segA.position = new Vector3(segB.position.x + segmentWidth, segA.position.y, segA.position.z);

        if (rightB < camLeft)
            segB.position = new Vector3(segA.position.x + segmentWidth, segB.position.y, segB.position.z);

        // 4) 防累计误差：强制对齐到网格（可选但非常有效）
        if (snapUnit > 0f)
        {
            segA.position = Snap(segA.position, snapUnit);
            segB.position = Snap(segB.position, snapUnit);
        }
    }

    static Vector3 Snap(Vector3 p, float unit)
    {
        p.x = Mathf.Round(p.x / unit) * unit;
        return p;
    }
}