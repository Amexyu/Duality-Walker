using System.Collections;
using UnityEngine;

public class ShapeSupplySpawner : MonoBehaviour
{
    [Header("节奏输入")]
    [SerializeField] private KeyCode shootObstacleKey = KeyCode.UpArrow;   // 上键：填障碍（白块）
    [SerializeField] private KeyCode shootPitKey = KeyCode.DownArrow;      // 下键：填坑（黑块）
    [SerializeField] private bool enableMouseInput = true;
    [SerializeField] private float inputCooldown = 0.06f;

    [Header("提示线（屏幕中线）")]
    [SerializeField] private float fireLineViewportX = 0.5f;
    [SerializeField] private float fireLineWidth = 0.04f;
    [SerializeField] private int fireLineSortingOrder = 200;

    // 自动反色（基于上下区域底色）
    [SerializeField] private bool autoInvertFireLineColor = true;
    [SerializeField] private Color upperAreaBaseColor = Color.white; // 上区底色（默认白）
    [SerializeField] private Color lowerAreaBaseColor = Color.black; // 下区底色（默认黑）
    [SerializeField] private bool areaSwapped = false;               // 未来反转时改这个值

    // 自动反色关闭时使用的手动颜色
    [SerializeField] private Color upperFireLineColor = Color.black;
    [SerializeField] private Color lowerFireLineColor = Color.white;

    // 防串区间隙（单位：世界坐标）
    // <=0 时自动按线宽计算
    [SerializeField] private float splitGapWorld = -1f;

    [Header("发射参数")]
    [SerializeField] private float launchOffsetInCells = 4f;   // 从黑白交界线两侧起射的偏移
    [SerializeField] private float impactTravelTime = 0.18f;   // 到达坑位/障碍位所需时间
    [SerializeField] private float passTravelTime = 0.10f;     // 越过交界线后消失前的时间
    [SerializeField] private float passDistanceInCells = 1.2f; // 越界继续前进距离
    [SerializeField] private int projectileSortingOrder = 25;

    [Header("吸附修正")]
    [SerializeField] private bool snapToNearestFeatureCell = true;
    [SerializeField] private float clearSnapRadiusInCells = 0.6f;

    [SerializeField] private bool followFireLineWhileFlying = true;

    private Camera cam;
    private ground worldGround;
    private float cellSize = 1f;
    private float gridOriginX;
    private float nextShootTime;
    private Sprite runtimeSprite;

    // 黑块：与 ground 的 ObstacleShapes 对齐（用于白色发射物去填障碍）
    private static readonly Vector2Int[][] BlackSupplyShapes =
    {
        new[] { new Vector2Int(0, 1) },
        new[] { new Vector2Int(0, 1), new Vector2Int(1, 1) },
        new[] { new Vector2Int(0, 1), new Vector2Int(0, 2) },
        new[] { new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(0, 2) },
        new[] { new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(1, 2) },
        new[]
        {
            new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1),
            new Vector2Int(2, 2), new Vector2Int(2, 3)
        },
        new[]
        {
            new Vector2Int(0, 1), new Vector2Int(1, 1),
            new Vector2Int(1, 2), new Vector2Int(2, 2), new Vector2Int(2, 3)
        }
    };

    // 白块：与 ground 的 PitShapes 对齐（用于黑色发射物去填坑）
    private static readonly Vector2Int[][] WhiteSupplyShapes =
    {
        new[] { new Vector2Int(0, 0), new Vector2Int(0, -1) },
        new[] { new Vector2Int(0, 0), new Vector2Int(1, 0) },
        new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(1, -1) },
        new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(1, -1) },
        new[]
        {
            new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0),
            new Vector2Int(1, -1), new Vector2Int(1, -2)
        },
        new[]
        {
            new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0),
            new Vector2Int(1, -1), new Vector2Int(2, -1),
            new Vector2Int(1, -2), new Vector2Int(2, -2)
        }
    };

    private void Start()
    {
        cam = Camera.main;
        worldGround = FindFirstObjectByType<ground>();

        if (worldGround != null)
        {
            cellSize = worldGround.CellSize;
            gridOriginX = worldGround.transform.position.x;
        }

        runtimeSprite = CreatePixelSprite();
        EnsureFireLine();
        UpdateFireLine();
    }

    private void Update()
    {
        UpdateFireLine();
        HandleShootInput();
    }

    private void HandleShootInput()
    {
        if (Time.time < nextShootTime)
        {
            return;
        }

        bool shootObstacle = Input.GetKeyDown(shootObstacleKey) || (enableMouseInput && Input.GetMouseButtonDown(0));
        bool shootPit = Input.GetKeyDown(shootPitKey) || (enableMouseInput && Input.GetMouseButtonDown(1));

        if (shootObstacle)
        {
            nextShootTime = Time.time + inputCooldown;
            ShootToObstacle();
            return;
        }

        if (shootPit)
        {
            nextShootTime = Time.time + inputCooldown;
            ShootToPit();
        }
    }

    // 上键/左键：白块 -> 填障碍
    private void ShootToObstacle()
    {
        float x = GetFireLineWorldX();
        float surfaceY = GetSurfaceY();

        Vector3 start = new Vector3(x, surfaceY - launchOffsetInCells * cellSize, 0f);
        Vector3 impact = new Vector3(x, surfaceY, 0f);
        Vector3 passEnd = new Vector3(x, surfaceY + passDistanceInCells * cellSize, 0f);

        Vector2Int[] shape = PickBlackShape();
        GameObject projectile = CreateProjectileAssembly("Shot_Obstacle", false, start, shape, false);

        StartCoroutine(TravelResolveAndDisappear(projectile, impact, passEnd, x));
    }

    // 下键/右键：黑块 -> 填坑
    private void ShootToPit()
    {
        float x = GetFireLineWorldX();
        float surfaceY = GetSurfaceY();

        Vector3 start = new Vector3(x, surfaceY + launchOffsetInCells * cellSize, 0f);
        Vector3 impact = new Vector3(x, surfaceY, 0f);
        Vector3 passEnd = new Vector3(x, surfaceY - passDistanceInCells * cellSize, 0f);

        Vector2Int[] shape = PickWhiteShape();
        GameObject projectile = CreateProjectileAssembly("Shot_Pit", true, start, shape, true);

        StartCoroutine(TravelResolveAndDisappear(projectile, impact, passEnd, x));
    }

    private IEnumerator TravelResolveAndDisappear(GameObject projectile, Vector3 impact, Vector3 passEnd, float lockedX)
    {
        if (projectile == null)
        {
            yield break;
        }

        Vector3 impactPos = new Vector3(lockedX, Mathf.Round(impact.y / cellSize) * cellSize, 0f);
        Vector3 passPos = new Vector3(lockedX, Mathf.Round(passEnd.y / cellSize) * cellSize, 0f);

        yield return MoveRoot(projectile.transform, projectile.transform.position, impactPos, impactTravelTime, lockedX);

        if (projectile != null)
        {
            TryResolveProjectile(projectile);
        }

        if (projectile != null)
        {
            yield return MoveRoot(projectile.transform, projectile.transform.position, passPos, passTravelTime, lockedX);
        }

        if (projectile != null)
        {
            Destroy(projectile);
        }
    }

    private IEnumerator MoveRoot(Transform t, Vector3 from, Vector3 to, float duration, float lockedX)
    {
        float d = Mathf.Max(0.01f, duration);
        float timer = 0f;

        float fromY = from.y;
        float toY = to.y;

        while (timer < d)
        {
            timer += Time.deltaTime;
            float p = Mathf.Clamp01(timer / d);

            float y = Mathf.Lerp(fromY, toY, p);
            float x = followFireLineWhileFlying ? GetFireLineWorldX() : lockedX;

            t.position = new Vector3(x, y, 0f);
            yield return null;
        }

        float endX = followFireLineWhileFlying ? GetFireLineWorldX() : lockedX;
        t.position = new Vector3(endX, toY, 0f);
    }

    private void TryResolveProjectile(GameObject projectile)
    {
        if (worldGround == null || projectile == null)
        {
            return;
        }

        bool anyCleared = false;
        ShapeBlock[] blocks = projectile.GetComponentsInChildren<ShapeBlock>();

        for (int i = 0; i < blocks.Length; i++)
        {
            ShapeBlock b = blocks[i];
            if (b == null)
            {
                continue;
            }

            bool cleared = TryClearWithSnap(b.transform.position, b.IsBlack);
            if (cleared)
            {
                anyCleared = true;
                Destroy(b.gameObject);
            }
        }

        if (anyCleared)
        {
            worldGround.RefreshCompositeCollider();
        }
    }

    private bool TryClearWithSnap(Vector3 worldPos, bool isBlackBlock)
    {
        Vector3 snapped = SnapToGroundGrid(worldPos);

        if (worldGround.TryClearFeatureCell(snapped, isBlackBlock))
        {
            return true;
        }

        if (!snapToNearestFeatureCell)
        {
            return false;
        }

        float r = Mathf.Max(0f, clearSnapRadiusInCells) * cellSize;
        if (r <= 0f)
        {
            return false;
        }

        Vector3[] offsets =
        {
            new Vector3(r, 0f, 0f),  new Vector3(-r, 0f, 0f),
            new Vector3(0f, r, 0f),  new Vector3(0f, -r, 0f),
            new Vector3(r, r, 0f),   new Vector3(-r, r, 0f),
            new Vector3(r, -r, 0f),  new Vector3(-r, -r, 0f)
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            if (worldGround.TryClearFeatureCell(snapped + offsets[i], isBlackBlock))
            {
                return true;
            }
        }

        return false;
    }

    private GameObject CreateProjectileAssembly(string name, bool black, Vector3 worldPos, Vector2Int[] shape, bool centerOnLine)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(null, true);
        root.transform.position = SnapToGroundGrid(worldPos);

        Vector2Int anchor = shape[0];

        int minX = int.MaxValue;
        int maxX = int.MinValue;
        int minY = int.MaxValue;
        int maxY = int.MinValue;

        for (int i = 0; i < shape.Length; i++)
        {
            minX = Mathf.Min(minX, shape[i].x);
            maxX = Mathf.Max(maxX, shape[i].x);
            minY = Mathf.Min(minY, shape[i].y);
            maxY = Mathf.Max(maxY, shape[i].y);
        }

        float pivotX = centerOnLine ? (minX + maxX) * 0.5f : anchor.x;
        float pivotY = centerOnLine ? (minY + maxY) * 0.5f : anchor.y;

        for (int i = 0; i < shape.Length; i++)
        {
            Vector2Int c = shape[i];

            GameObject go = new GameObject("Block");
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = new Vector3((c.x - pivotX) * cellSize, (c.y - pivotY) * cellSize, 0f);
            go.transform.localScale = Vector3.one * cellSize;

            ShapeBlock b = go.AddComponent<ShapeBlock>();
            b.Initialize(black, runtimeSprite);

            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = projectileSortingOrder;
            }

            Transform border = go.transform.Find("Border");
            if (border != null)
            {
                SpriteRenderer borderSr = border.GetComponent<SpriteRenderer>();
                if (borderSr != null)
                {
                    borderSr.sortingOrder = projectileSortingOrder - 1;
                }
            }
        }

        return root;
    }

    // ====== 替换/新增字段（放在提示线相关字段附近）======
    private Material lineMaterial;
    private LineRenderer upperFireLine;
    private LineRenderer lowerFireLine;

    [SerializeField] private float fireLineExtraYOffsetInCells = 0f; // 在真实分界(半格)基础上的微调


    // ====== 用这个替换 EnsureFireLine ======
    private void EnsureFireLine()
    {
        if (lineMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                lineMaterial = new Material(shader);
            }
        }

        if (upperFireLine == null)
        {
            upperFireLine = CreateFireLineRenderer("RhythmFireLine_Upper");
        }

        if (lowerFireLine == null)
        {
            lowerFireLine = CreateFireLineRenderer("RhythmFireLine_Lower");
        }

        RefreshFireLineColors();
    }

    private LineRenderer CreateFireLineRenderer(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = false;
        lr.positionCount = 2;
        lr.startWidth = fireLineWidth;
        lr.endWidth = fireLineWidth;
        lr.sortingOrder = fireLineSortingOrder;
        lr.numCapVertices = 0;
        lr.numCornerVertices = 0;

        if (lineMaterial != null)
        {
            lr.material = lineMaterial;
        }

        return lr;
    }


    // ====== 新增：自动反色与区域反转接口 ======
    public void SetAreaSwapped(bool swapped)
    {
        areaSwapped = swapped;
        RefreshFireLineColors();
    }

    private void RefreshFireLineColors()
    {
        if (upperFireLine == null || lowerFireLine == null)
        {
            return;
        }

        Color upperArea = areaSwapped ? lowerAreaBaseColor : upperAreaBaseColor;
        Color lowerArea = areaSwapped ? upperAreaBaseColor : lowerAreaBaseColor;

        Color upperLine = autoInvertFireLineColor ? InvertKeepAlpha(upperArea) : upperFireLineColor;
        Color lowerLine = autoInvertFireLineColor ? InvertKeepAlpha(lowerArea) : lowerFireLineColor;

        upperFireLine.startColor = upperLine;
        upperFireLine.endColor = upperLine;
        lowerFireLine.startColor = lowerLine;
        lowerFireLine.endColor = lowerLine;
    }

    private static Color InvertKeepAlpha(Color c)
    {
        return new Color(1f - c.r, 1f - c.g, 1f - c.b, c.a);
    }


    // ====== 用这个替换 UpdateFireLine（防串区）======
    private void UpdateFireLine()
    {
        if (upperFireLine == null || lowerFireLine == null)
        {
            return;
        }

        RefreshFireLineColors();

        float x = GetFireLineWorldX();
        float splitY = GetSurfaceY() + 0.5f * cellSize + fireLineExtraYOffsetInCells * cellSize;

        Vector3 top = ViewportToWorld(fireLineViewportX, 1f);
        Vector3 bottom = ViewportToWorld(fireLineViewportX, 0f);

        float gap = splitGapWorld > 0f ? splitGapWorld : Mathf.Max(fireLineWidth * 0.8f, 0.02f);
        float upperStartY = splitY + gap;
        float lowerEndY = splitY - gap;

        if (upperStartY > top.y) upperStartY = top.y;
        if (lowerEndY < bottom.y) lowerEndY = bottom.y;

        upperFireLine.SetPosition(0, new Vector3(x, upperStartY, 0f));
        upperFireLine.SetPosition(1, new Vector3(x, top.y, 0f));

        lowerFireLine.SetPosition(0, new Vector3(x, bottom.y, 0f));
        lowerFireLine.SetPosition(1, new Vector3(x, lowerEndY, 0f));
    }

    private float GetFireLineWorldX()
    {
        return ViewportToWorld(fireLineViewportX, 0.5f).x;
    }

    private Vector3 ViewportToWorld(float vx, float vy)
    {
        if (cam == null)
        {
            cam = Camera.main;
        }

        if (cam == null)
        {
            return Vector3.zero;
        }

        float z = Mathf.Abs(cam.transform.position.z);
        Vector3 w = cam.ViewportToWorldPoint(new Vector3(vx, vy, z));
        w.z = 0f;
        return w;
    }

    private float GetSurfaceY()
    {
        if (worldGround != null)
        {
            return worldGround.SurfaceY;
        }

        return 0f;
    }

    private float SnapX(float x)
    {
        return gridOriginX + Mathf.Round((x - gridOriginX) / cellSize) * cellSize;
    }

    private Vector3 SnapToGroundGrid(Vector3 p)
    {
        p.x = SnapX(p.x);
        p.y = Mathf.Round(p.y / cellSize) * cellSize;
        p.z = 0f;
        return p;
    }

    private Vector2Int[] PickBlackShape()
    {
        if (worldGround != null && worldGround.IsTestMode)
        {
            return BlackSupplyShapes[Mathf.Clamp(worldGround.CurrentObstacleShapeIndex, 0, BlackSupplyShapes.Length - 1)];
        }

        return BlackSupplyShapes[Random.Range(0, BlackSupplyShapes.Length)];
    }

    private Vector2Int[] PickWhiteShape()
    {
        if (worldGround != null && worldGround.IsTestMode)
        {
            return WhiteSupplyShapes[Mathf.Clamp(worldGround.CurrentPitShapeIndex, 0, WhiteSupplyShapes.Length - 1)];
        }

        return WhiteSupplyShapes[Random.Range(0, WhiteSupplyShapes.Length)];
    }

    private Sprite CreatePixelSprite()
    {
        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }

    private void OnDestroy()
    {
        if (lineMaterial != null)
        {
            Destroy(lineMaterial);
        }
    }
}