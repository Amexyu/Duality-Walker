using System;
using UnityEngine;

public class ShapeSpawner2D : MonoBehaviour
{
    [Serializable]
    public struct ShapeDef
    {
        public string id;
        public Vector2Int[] cells;
        public int weight;
    }

    [Header("Refs")]
    [SerializeField] private Transform groupRoot;     // 拖 GroupRoot
    [SerializeField] private PlayerGroup2D group;     // 拖 GroupRoot 上的 PlayerGroup2D
    [SerializeField] private Sprite blockSprite;
    [SerializeField] private Camera cam;

    [Header("Spawn")]
    [SerializeField] private float interval = 1.0f;   // ✅生成速度（秒）
    [SerializeField] private int maxAlive = 12;

    [Header("Spacing (World Units)")]
    [SerializeField] private float minGapWorld = 0.8f; // ✅形状之间额外空隙（越大越分散）
    [SerializeField] private int tryCount = 80;

    [Header("Keep Distance From Player")]
    [SerializeField] private float minDistanceToGroupBox = 3.0f; // ✅离玩家主体外框至少这么远（世界单位）

    [Header("Area")]
    [SerializeField] private float screenMargin = 1.2f;

    [Header("Grid")]
    [SerializeField] private float gridSize = 1f;

    [Header("Shapes (Rect/Square Only)")]
    [SerializeField] private ShapeDef[] shapes;

    [SerializeField] private bool forceDefaultShapes = true; // ✅强制覆盖Inspector shapes

    private float timer;

    private void Reset()
    {
        ApplyDefaultShapes();
    }

    // ✅只生成：合适的长方形 + 正方形（方便拼更大正方形）
    private void ApplyDefaultShapes()
    {
        shapes = new ShapeDef[]
        {
            // ===== 1x1（补洞必备）=====
            new ShapeDef { id="Square1", cells=new []{
                new Vector2Int(0,0)
            }, weight=45 },

            // ===== 2x2（最核心）=====
            new ShapeDef { id="Square2", cells=new []{
                new Vector2Int(0,0), new Vector2Int(1,0),
                new Vector2Int(0,1), new Vector2Int(1,1)
            }, weight=18 },

            // ===== 1x2（横/竖都要，因为系统不旋转）=====
            new ShapeDef { id="Rect_1x2_H", cells=new []{
                new Vector2Int(0,0), new Vector2Int(1,0)
            }, weight=22 },

            new ShapeDef { id="Rect_1x2_V", cells=new []{
                new Vector2Int(0,0), new Vector2Int(0,1)
            }, weight=22 },

            // ===== 1x3（可选：拉边用，权重低一点更稳）=====
            new ShapeDef { id="Rect_1x3_H", cells=new []{
                new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0)
            }, weight=10 },

            new ShapeDef { id="Rect_1x3_V", cells=new []{
                new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2)
            }, weight=10 },

            // 如果你觉得仍然难补洞：把下面两条删掉（2x3容易制造缺口）
            // new ShapeDef { id="Rect_2x3_H", cells=new []{
            //     new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0),
            //     new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1)
            // }, weight=3 },
            //
            // new ShapeDef { id="Rect_2x3_V", cells=new []{
            //     new Vector2Int(0,0), new Vector2Int(0,1),
            //     new Vector2Int(1,0), new Vector2Int(1,1),
            //     new Vector2Int(2,0), new Vector2Int(2,1)
            // }, weight=3 },
        };
    }

    private void Awake()
    {
        // ✅关键：运行时强制覆盖，保证“改代码就生效”
        if (forceDefaultShapes || shapes == null || shapes.Length == 0)
            ApplyDefaultShapes();

        if (cam == null) cam = Camera.main;
        timer = interval;

        if (group == null && groupRoot != null)
            group = groupRoot.GetComponent<PlayerGroup2D>();

        if (blockSprite != null)
        {
            gridSize = blockSprite.bounds.size.x;
            FreePiece2D.GlobalGridSize = gridSize;
        }

        Debug.Log($"[Spawner] force={forceDefaultShapes}, shapesCount={(shapes == null ? 0 : shapes.Length)}");
        if (shapes != null && shapes.Length > 0)
            Debug.Log($"[Spawner] firstShape={shapes[0].id}");
    }

    private void Update()
    {
        if (blockSprite == null) return;
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        int alive = 0;
        for (int i = 0; i < FreePiece2D.Active.Count; i++)
            if (FreePiece2D.Active[i] != null && !FreePiece2D.Active[i].Attached) alive++;

        if (alive >= maxAlive) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        timer = interval;
        SpawnOne();
    }

    // ✅外部可调用：调节生成速度
    public void SetInterval(float seconds)
    {
        interval = Mathf.Max(0.05f, seconds);
        timer = Mathf.Min(timer, interval);
    }

    private void SpawnOne()
    {
        if (blockSprite == null) return;
        if (shapes == null || shapes.Length == 0) return;

        gridSize = blockSprite.bounds.size.x;
        FreePiece2D.GlobalGridSize = gridSize;

        ShapeDef def = PickWeighted();

        float s = groupRoot != null ? groupRoot.lossyScale.x : 1f;
        Vector2 pos = PickPosition(def, s);

        var go = new GameObject($"Free_{def.id}");
        go.transform.position = pos;

        // ✅生成大小跟随玩家主体缩放
        go.transform.localScale = new Vector3(s, s, 1f);

        var piece = go.AddComponent<FreePiece2D>();
        piece.Init(def.cells, blockSprite, gridSize);

        // 统一着色新生成的自由块
        if (group != null) group.ApplyBlockColorTo(go.transform);
        else
        {
            var srs = go.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < srs.Length; i++)
                srs[i].color = new Color(0.85f, 0.85f, 0.85f, 1f);
        }
    }

    private Vector2 PickPosition(ShapeDef def, float s)
    {
        // 本次形状世界半径（对角线一半）
        GetShapeSizeWorld(def, s, out float wWorld, out float hWorld);
        float diag = Mathf.Sqrt(wWorld * wWorld + hWorld * hWorld);
        float radius = diag * 0.5f;

        float z = -cam.transform.position.z;
        Vector3 vmin = cam.ViewportToWorldPoint(new Vector3(0, 0, z));
        Vector3 vmax = cam.ViewportToWorldPoint(new Vector3(1, 1, z));

        // 为了不出屏：按形状半径缩进
        float minX = vmin.x + screenMargin + radius;
        float maxX = vmax.x - screenMargin - radius;
        float minY = vmin.y + screenMargin + radius;
        float maxY = vmax.y - screenMargin - radius;

        if (minX > maxX) { float mid = (vmin.x + vmax.x) * 0.5f; minX = maxX = mid; }
        if (minY > maxY) { float mid = (vmin.y + vmax.y) * 0.5f; minY = maxY = mid; }

        for (int t = 0; t < tryCount; t++)
        {
            Vector2 p = new Vector2(UnityEngine.Random.Range(minX, maxX), UnityEngine.Random.Range(minY, maxY));

            // ✅远离玩家主体外框（并考虑形状半径）
            if (group != null)
            {
                group.GetWorldAABB(out Vector2 bmin, out Vector2 bmax);
                if (DistancePointAABB(p, bmin, bmax) < (minDistanceToGroupBox + radius))
                    continue;
            }

            // ✅与其它自由块保持间隔：考虑双方尺寸 + 额外空隙
            bool ok = true;
            for (int i = 0; i < FreePiece2D.Active.Count; i++)
            {
                var other = FreePiece2D.Active[i];
                if (other == null || other.Attached) continue;

                float otherRadius = GetApproxRadiusWorld(other);
                float need = radius + otherRadius + minGapWorld;

                if (Vector2.Distance(p, other.transform.position) < need)
                {
                    ok = false;
                    break;
                }
            }

            if (ok) return p;
        }

        // 找不到就退化随机（避免卡死）
        return new Vector2(UnityEngine.Random.Range(minX, maxX), UnityEngine.Random.Range(minY, maxY));
    }

    private void GetShapeSizeWorld(ShapeDef def, float s, out float wWorld, out float hWorld)
    {
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        for (int i = 0; i < def.cells.Length; i++)
        {
            var c = def.cells[i];
            if (c.x < minX) minX = c.x;
            if (c.x > maxX) maxX = c.x;
            if (c.y < minY) minY = c.y;
            if (c.y > maxY) maxY = c.y;
        }

        int wCells = (maxX - minX + 1);
        int hCells = (maxY - minY + 1);

        float cellWorld = gridSize * s;
        wWorld = wCells * cellWorld;
        hWorld = hCells * cellWorld;
    }

    private float GetApproxRadiusWorld(FreePiece2D p)
    {
        var srs = p.GetComponentsInChildren<SpriteRenderer>();
        if (srs == null || srs.Length == 0) return gridSize * 0.5f;

        Bounds b = srs[0].bounds;
        for (int i = 1; i < srs.Length; i++) b.Encapsulate(srs[i].bounds);

        float w = b.size.x;
        float h = b.size.y;
        float d = Mathf.Sqrt(w * w + h * h);
        return d * 0.5f;
    }

    private static float DistancePointAABB(Vector2 p, Vector2 bmin, Vector2 bmax)
    {
        float dx = 0f;
        if (p.x < bmin.x) dx = bmin.x - p.x;
        else if (p.x > bmax.x) dx = p.x - bmax.x;

        float dy = 0f;
        if (p.y < bmin.y) dy = bmin.y - p.y;
        else if (p.y > bmax.y) dy = p.y - bmax.y;

        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    private ShapeDef PickWeighted()
    {
        int total = 0;
        foreach (var s in shapes) total += Mathf.Max(1, s.weight);

        int roll = UnityEngine.Random.Range(0, total);
        int acc = 0;
        foreach (var s in shapes)
        {
            acc += Mathf.Max(1, s.weight);
            if (roll < acc) return s;
        }
        return shapes[0];
    }
}
