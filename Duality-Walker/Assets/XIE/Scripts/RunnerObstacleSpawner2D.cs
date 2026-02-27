using System;
using UnityEngine;

public class RunnerObstacleSpawner2D : MonoBehaviour
{
    [Serializable]
    public struct ShapeDef
    {
        public string id;
        public Vector2Int[] cells;
        public int weight;
    }

    [Header("Refs")]
    [SerializeField] private Camera cam;
    [SerializeField] private Transform worldRoot;
    [SerializeField] private Sprite blockSprite;
    [SerializeField] private BoundaryGround2D boundaryGround;

    [Header("Spawn")]
    [SerializeField] private float interval = 1.2f;
    [SerializeField] private int maxAlive = 12;
    [SerializeField] private float spawnMarginRight = 1.2f;

    [Header("Zone Y")]
    [SerializeField] private float whiteZoneY = 1.2f;
    [SerializeField] private float blackZoneY = -1.2f;
    [SerializeField] private bool clampPitYToGround = true;
    [SerializeField] private float pitYOffsetFromGround = 0f; // 0=贴地生成，>0=高于地面

    [Header("Shape Pools")]
    [SerializeField] private ShapeDef[] whiteShapes;
    [SerializeField] private ShapeDef[] blackShapes;

    [Header("Visual")]
    [SerializeField] private Color blockColor = new Color(0.85f, 0.85f, 0.85f, 1f);
    [SerializeField] private Color pitColor = new Color(0.7f, 0.15f, 0.9f, 0.85f);

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private float timer;
    private bool warnedMissingCam;
    private bool warnedMissingSprite;
    private bool warnedMissingWorldRoot;

    private float spriteRetryTimer;
    private const float SpriteRetryInterval = 1f;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (boundaryGround == null && cam != null) boundaryGround = cam.GetComponent<BoundaryGround2D>();

        if (worldRoot == null)
        {
            worldRoot = transform;
            if (debugLog && !warnedMissingWorldRoot)
            {
                warnedMissingWorldRoot = true;
                Debug.LogWarning("[RunnerObstacleSpawner2D] worldRoot 未设置，已回退到当前物体。建议把 worldRoot 指向挂有 ScrollingLayer2D 的 WorldRoot。");
            }
        }

        TryAutoAssignBlockSprite();
        timer = interval;

        if (whiteShapes == null || whiteShapes.Length == 0)
            whiteShapes = CreateDefaultShapes();

        if (blackShapes == null || blackShapes.Length == 0)
            blackShapes = CreateDefaultShapes();
    }

    private void Update()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null)
        {
            if (debugLog && !warnedMissingCam)
            {
                warnedMissingCam = true;
                Debug.LogError("[RunnerObstacleSpawner2D] 找不到 Camera，无法生成障碍。");
            }
            return;
        }

        if (blockSprite == null)
        {
            spriteRetryTimer -= Time.deltaTime;
            if (spriteRetryTimer <= 0f)
            {
                spriteRetryTimer = SpriteRetryInterval;
                TryAutoAssignBlockSprite();
            }

            if (blockSprite == null)
            {
                if (debugLog && !warnedMissingSprite)
                {
                    warnedMissingSprite = true;
                    Debug.LogError("[RunnerObstacleSpawner2D] blockSprite 未设置，且自动查找失败。请手动拖入一个方块 Sprite。");
                }
                return;
            }
        }

        int alive = 0;
        var all = FindObjectsOfType<RunnerObstacle2D>();
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null) alive++;

        if (alive >= maxAlive) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = interval;

        SpawnOne();
    }

    private ShapeDef[] CreateDefaultShapes()
    {
        return new[]
        {
            new ShapeDef { id = "1x1", cells = new[] { new Vector2Int(0, 0) }, weight = 40 },
            new ShapeDef { id = "1x2", cells = new[] { new Vector2Int(0, 0), new Vector2Int(1, 0) }, weight = 25 },
            new ShapeDef { id = "2x1", cells = new[] { new Vector2Int(0, 0), new Vector2Int(0, 1) }, weight = 20 },
            new ShapeDef
            {
                id = "2x2",
                cells = new[]
                {
                    new Vector2Int(0, 0), new Vector2Int(1, 0),
                    new Vector2Int(0, 1), new Vector2Int(1, 1)
                },
                weight = 12
            },
        };
    }

    private void TryAutoAssignBlockSprite()
    {
        // 1) 优先从玩家组内块体找
        var pg = FindObjectOfType<PlayerGroup2D>();
        if (pg != null)
        {
            var srs = pg.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < srs.Length; i++)
            {
                if (srs[i] != null && srs[i].sprite != null)
                {
                    blockSprite = srs[i].sprite;
                    if (debugLog) Debug.Log($"[RunnerObstacleSpawner2D] 已自动绑定 blockSprite（来源 PlayerGroup2D）: {blockSprite.name}");
                    warnedMissingSprite = false;
                    return;
                }
            }
        }

        // 2) 兜底：从场景里任意可用 SpriteRenderer 找
        var allSr = FindObjectsOfType<SpriteRenderer>(true);
        for (int i = 0; i < allSr.Length; i++)
        {
            var sr = allSr[i];
            if (sr == null || sr.sprite == null) continue;

            string n = sr.gameObject.name;
            // 排除背景生成对象，减少误判
            if (n == "BackgroundTop" || n == "BackgroundBottom" || n == "GradientBackground") continue;

            blockSprite = sr.sprite;
            if (debugLog) Debug.Log($"[RunnerObstacleSpawner2D] 已自动绑定 blockSprite（来源场景 SpriteRenderer）: {blockSprite.name}");
            warnedMissingSprite = false;
            return;
        }
    }

    private void SpawnOne()
    {
        float z = -cam.transform.position.z;
        Vector3 vmax = cam.ViewportToWorldPoint(new Vector3(1f, 1f, z));

        bool spawnWhite = UnityEngine.Random.value > 0.5f;
        RunnerObstacle2D.ObstacleKind kind = spawnWhite ? RunnerObstacle2D.ObstacleKind.Block : RunnerObstacle2D.ObstacleKind.Pit;

        ShapeDef[] defs = spawnWhite ? whiteShapes : blackShapes;
        ShapeDef def = PickWeighted(defs);

        float spawnY = spawnWhite ? whiteZoneY : blackZoneY;
        if (!spawnWhite && clampPitYToGround)
        {
            float groundTop = GetGroundTopY();
            spawnY = Mathf.Max(spawnY, groundTop + pitYOffsetFromGround);
        }

        bool worldHasScroll = worldRoot != null && worldRoot.GetComponent<ScrollingLayer2D>() != null;
        float margin = worldHasScroll ? spawnMarginRight : 0f;
        Vector3 rootPos = new Vector3(vmax.x + margin, spawnY, 0f);

        GameObject root = new GameObject($"Obstacle_{kind}_{def.id}");
        root.transform.position = rootPos;
        if (worldRoot != null) root.transform.SetParent(worldRoot, true);

        float cell = Mathf.Max(0.01f, blockSprite.bounds.size.x);
        bool isPit = kind == RunnerObstacle2D.ObstacleKind.Pit;

        for (int i = 0; i < def.cells.Length; i++)
        {
            Vector2Int c = def.cells[i];
            GameObject ch = new GameObject($"Cell_{c.x}_{c.y}");
            ch.transform.SetParent(root.transform, false);
            ch.transform.localPosition = new Vector3(c.x * cell, c.y * cell, 0f);

            SpriteRenderer sr = ch.AddComponent<SpriteRenderer>();
            sr.sprite = blockSprite;
            sr.color = isPit ? pitColor : blockColor;

            BoxCollider2D col = ch.AddComponent<BoxCollider2D>();
            col.isTrigger = isPit; // Block=实心，Pit=触发
        }

        RunnerObstacle2D ob = root.AddComponent<RunnerObstacle2D>();
        ob.Init(kind, def.cells);

        if (debugLog)
            Debug.Log($"[RunnerObstacleSpawner2D] Spawn -> {root.name}, zone={(spawnWhite ? "White" : "Black")}, kind={kind}, pos={rootPos}");
    }

    private float GetGroundTopY()
    {
        if (boundaryGround == null && cam != null)
            boundaryGround = cam.GetComponent<BoundaryGround2D>();

        return boundaryGround != null ? boundaryGround.GetGroundTopY() : blackZoneY;
    }

    private ShapeDef PickWeighted(ShapeDef[] defs)
    {
        if (defs == null || defs.Length == 0) defs = CreateDefaultShapes();

        int total = 0;
        for (int i = 0; i < defs.Length; i++)
            total += Mathf.Max(1, defs[i].weight);

        int roll = UnityEngine.Random.Range(0, total);
        int acc = 0;

        for (int i = 0; i < defs.Length; i++)
        {
            acc += Mathf.Max(1, defs[i].weight);
            if (roll < acc) return defs[i];
        }

        return defs[0];
    }
}