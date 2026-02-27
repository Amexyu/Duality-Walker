using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform groupRoot;       // 拖 GroupRoot（挂 PlayerGroup2D 的那个）
    [SerializeField] private PlayerGroup2D group;       // 拖 GroupRoot 上的 PlayerGroup2D（可不拖，会自动找）
    [SerializeField] private Transform playerVisual;    // 拖“玩家显示方块”（可选）

    [Header("Move")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private bool clampInsideScreen = true;
    [SerializeField] private float screenPadding = 0.3f;

    [Header("Score")]
    [SerializeField] private int score = 0;

    [Header("Optional Visual Scale Sync")]
    [SerializeField] private bool syncPlayerVisualScale = true;

    private Rigidbody2D rb;
    private Vector2 input;
    private Camera cam;

    // 防止同一帧重复调用
    private int lastSquareFrame = -1;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cam = Camera.main;

        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (groupRoot == null)
            Debug.LogError("[PlayerController] groupRoot is NOT assigned. Drag GroupRoot.");

        if (group == null && groupRoot != null)
            group = groupRoot.GetComponent<PlayerGroup2D>();
    }

    private void Update()
    {
        input.x = Input.GetAxisRaw("Horizontal");
        input.y = Input.GetAxisRaw("Vertical");
        input = input.normalized;
    }

    private void FixedUpdate()
    {
        Vector2 next = rb.position + input * moveSpeed * Time.fixedDeltaTime;
        if (clampInsideScreen) next = ClampToScreen(next);
        rb.MovePosition(next);
    }

    private void LateUpdate()
    {
        // ✅只做“视觉同步”：避免“玩家显示方块”跟 groupRoot 大小不一致
        if (syncPlayerVisualScale && playerVisual != null && groupRoot != null)
            playerVisual.localScale = groupRoot.localScale;
    }

    // ✅由 PlayerGroup2D 在“满正方形完成”时调用
    public void OnSquareCompleted(int addScore, int n)
    {
        if (lastSquareFrame == Time.frameCount) return;
        lastSquareFrame = Time.frameCount;

        score += addScore;
        Debug.Log($"[Square] n={n} +{addScore} => score={score}");

        // 倒计时 +30 秒
        //Timer.AddTime(30f);
    }

    private Vector2 ClampToScreen(Vector2 worldPos)
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return worldPos;

        float z = -cam.transform.position.z;
        Vector3 min = cam.ViewportToWorldPoint(new Vector3(0, 0, z));
        Vector3 max = cam.ViewportToWorldPoint(new Vector3(1, 1, z));

        float x = Mathf.Clamp(worldPos.x, min.x + screenPadding, max.x - screenPadding);
        float y = Mathf.Clamp(worldPos.y, min.y + screenPadding, max.y - screenPadding);
        return new Vector2(x, y);
    }
}
