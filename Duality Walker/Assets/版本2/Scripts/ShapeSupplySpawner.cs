using System.Collections;
using UnityEngine;

public class ShapeSupplySpawner : MonoBehaviour
{
    [Header("节奏输入")]
    [SerializeField] private KeyCode shootObstacleKey = KeyCode.UpArrow;   // 上键：填障碍（白块）
    [SerializeField] private KeyCode shootPitKey = KeyCode.DownArrow;      // 下键：填坑（黑块）
    [SerializeField] private bool enableMouseInput = true;
    [SerializeField] private float inputCooldown = 0.06f;

    [Header("输入音效")]
    [SerializeField] private AudioSource inputSfxSource;
    [SerializeField] private AudioClip defaultInputSfx;
    [SerializeField] private AudioClip shootObstacleSfx; // 上键 / 鼠标左键
    [SerializeField] private AudioClip shootPitSfx;      // 下键 / 鼠标右键
    [SerializeField] [Min(0f)] private float inputSfxVolume = 1f;

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

    [Header("即时判定（瞄准线）")]
    [SerializeField] private bool useInstantAimLineResolve = true;
    [SerializeField] private int aimResolveRangeInCells = 8;
    [SerializeField] private bool projectileVisualOnly = true;

    [Header("发射物造型")]
    [SerializeField] private ProjectileShapeMode projectileShapeMode = ProjectileShapeMode.Fixed1x1;
    [SerializeField] private Vector2Int[] customObstacleProjectileShape = { new Vector2Int(0, 1) };
    [SerializeField] private Vector2Int[] customPitProjectileShape = { new Vector2Int(0, 0) };

    private enum ProjectileShapeMode
    {
        FollowGroundShape, // 跟随 ground 测试/随机逻辑（旧行为）
        Fixed1x1,          // 固定 1x1
        Custom             // 使用自定义数组
    }

    private static readonly Vector2Int[] FixedObstacleShape =
    {
        new Vector2Int(0, 1)
    };

    private static readonly Vector2Int[] FixedPitShape =
    {
        new Vector2Int(0, 0)
    };

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

        EnsureInputSfxSource();

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
            PlayInputSfx(shootObstacleSfx);
            ShootToObstacle();
            return;
        }

        if (shootPit)
        {
            nextShootTime = Time.time + inputCooldown;
            PlayInputSfx(shootPitSfx);
            ShootToPit();
        }
    }

    // 上键/左键：白块 -> 填障碍
    private void ShootToObstacle()
    {
        if (useInstantAimLineResolve)
        {
            ResolveByAimLine(clearObstacle: true);
        }

        float x = GetFireLineWorldX();
        float surfaceY = GetSurfaceY();

        Vector3 start = new Vector3(x, surfaceY - launchOffsetInCells * cellSize, 0f);
        Vector3 impact = new Vector3(x, surfaceY, 0f);
        Vector3 passEnd = new Vector3(x, surfaceY + passDistanceInCells * cellSize, 0f);

        Vector2Int[] shape = PickBlackShape();
        GameObject projectile = CreateProjectileAssembly("Shot_Obstacle", false, start, shape, false);

        bool resolveOnImpact = !useInstantAimLineResolve && !projectileVisualOnly;
        StartCoroutine(TravelResolveAndDisappear(projectile, impact, passEnd, x, resolveOnImpact));
    }

    // 下键/右键：黑块 -> 填坑
    private void ShootToPit()
    {
        if (useInstantAimLineResolve)
        {
            ResolveByAimLine(clearObstacle: false);
        }

        float x = GetFireLineWorldX();
        float surfaceY = GetSurfaceY();

        Vector3 start = new Vector3(x, surfaceY + launchOffsetInCells * cellSize, 0f);
        Vector3 impact = new Vector3(x, surfaceY, 0f);
        Vector3 passEnd = new Vector3(x, surfaceY - passDistanceInCells * cellSize, 0f);

        Vector2Int[] shape = PickWhiteShape();
        GameObject projectile = CreateProjectileAssembly("Shot_Pit", true, start, shape, true);

        bool resolveOnImpact = !useInstantAimLineResolve && !projectileVisualOnly;
        StartCoroutine(TravelResolveAndDisappear(projectile, impact, passEnd, x, resolveOnImpact));
    }

    private IEnumerator TravelResolveAndDisappear(GameObject projectile, Vector3 impact, Vector3 passEnd, float lockedX, bool resolveOnImpact)
    {
        if (projectile == null)
        {
            yield break;
        }

        Vector3 impactPos = new Vector3(lockedX, Mathf.Round(impact.y / cellSize) * cellSize, 0f);
        Vector3 passPos = new Vector3(lockedX, Mathf.Round(passEnd.y / cellSize) * cellSize, 0f);

        yield return MoveRoot(projectile.transform, projectile.transform.position, impactPos, impactTravelTime, lockedX);

        if (resolveOnImpact && projectile != null)
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

    private bool ResolveByAimLine(bool clearObstacle)
    {
        if (worldGround == null)
        {
            return false;
        }

        float x = GetFireLineWorldX();
        float surfaceY = GetSurfaceY();

        bool isBlackBlock = !clearObstacle;
        int range = Mathf.Max(1, aimResolveRangeInCells);

        if (clearObstacle)
        {
            for (int i = 1; i <= range; i++)
            {
                Vector3 p = new Vector3(x, surfaceY + i * cellSize, 0f);
                if (TryClearWithSnap(p, isBlackBlock))
                {
                    worldGround.RefreshCompositeCollider();
                    return true;
                }
            }
        }
        else
        {
            for (int i = 0; i <= range; i++)
            {
                Vector3 p = new Vector3(x, surfaceY - i * cellSize, 0f);
                if (TryClearWithSnap(p, isBlackBlock))
                {
                    worldGround.RefreshCompositeCollider();
                    return true;
                }
            }
        }

        return false;
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

            // 取消弹射物边框效果
            Transform border = go.transform.Find("Border");
            if (border != null)
            {
                Destroy(border.gameObject);
            }
        }

        return root;
    }

    private Material lineMaterial;
    private LineRenderer upperArrowLine;
    private LineRenderer lowerArrowLine;

    [Header("辅助线短箭头造型（仅视觉）")]
    [SerializeField] private float fireLineExtraYOffsetInCells = 0f;     // 在真实分界(半格)基础上的微调
    [SerializeField] private float arrowShaftLengthInCells = 1.2f;       // 箭杆长度（只控制箭头大小）
    [SerializeField] private float arrowHeadLengthInCells = 0.45f;       // 箭头长度
    [SerializeField] private float arrowHeadHalfWidthMultiplier = 1.8f;  // 箭头半宽 = fireLineWidth * 倍率
    [SerializeField] private float blackArrowOffsetInCells = 3f;         // 黑箭头整体上移（格）
    [SerializeField] private float whiteArrowOffsetInCells = 3f;         // 白箭头整体下移（格）

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

        if (upperArrowLine == null)
        {
            upperArrowLine = CreateFireLineRenderer("RhythmArrow_Upper", 5);
        }

        if (lowerArrowLine == null)
        {
            lowerArrowLine = CreateFireLineRenderer("RhythmArrow_Lower", 5);
        }

        RefreshFireLineColors();
    }

    private LineRenderer CreateFireLineRenderer(string name, int pointCount)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = false;
        lr.positionCount = Mathf.Max(2, pointCount);
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

    public void SetAreaSwapped(bool swapped)
    {
        areaSwapped = swapped;
        RefreshFireLineColors();
    }

    private void RefreshFireLineColors()
    {
        if (upperArrowLine == null || lowerArrowLine == null)
        {
            return;
        }

        Color upperArea = areaSwapped ? lowerAreaBaseColor : upperAreaBaseColor;
        Color lowerArea = areaSwapped ? upperAreaBaseColor : lowerAreaBaseColor;

        Color upperLine = autoInvertFireLineColor ? InvertKeepAlpha(upperArea) : upperFireLineColor;
        Color lowerLine = autoInvertFireLineColor ? InvertKeepAlpha(lowerArea) : lowerFireLineColor;

        upperArrowLine.startColor = upperLine;
        upperArrowLine.endColor = upperLine;
        lowerArrowLine.startColor = lowerLine;
        lowerArrowLine.endColor = lowerLine;
    }

    private static Color InvertKeepAlpha(Color c)
    {
        return new Color(1f - c.r, 1f - c.g, 1f - c.b, c.a);
    }

    private void UpdateFireLine()
    {
        if (upperArrowLine == null || lowerArrowLine == null)
        {
            return;
        }

        RefreshFireLineColors();

        float x = GetFireLineWorldX();
        float splitY = GetSurfaceY() + 0.5f * cellSize + fireLineExtraYOffsetInCells * cellSize;

        float gap = splitGapWorld > 0f ? splitGapWorld : Mathf.Max(fireLineWidth * 0.8f, 0.02f);

        float shaftLen = Mathf.Max(0.05f, arrowShaftLengthInCells * cellSize);
        float headLen = Mathf.Clamp(arrowHeadLengthInCells * cellSize, 0.02f, shaftLen * 0.9f);
        float headHalfW = Mathf.Max(fireLineWidth * 0.5f, fireLineWidth * arrowHeadHalfWidthMultiplier);

        // 上箭头（黑）：整体上移，但箭头朝下（指向中线）
        float blackOffsetY = Mathf.Max(0f, blackArrowOffsetInCells) * cellSize;
        float upperTipY = splitY + gap + blackOffsetY;            // 箭尖在下
        float upperBaseY = upperTipY + shaftLen;                  // 箭杆起点在上
        float upperHeadBaseY = upperTipY + headLen;               // 箭头两翼在箭尖上方

        upperArrowLine.SetPosition(0, new Vector3(x, upperBaseY, 0f));
        upperArrowLine.SetPosition(1, new Vector3(x, upperTipY, 0f));
        upperArrowLine.SetPosition(2, new Vector3(x - headHalfW, upperHeadBaseY, 0f));
        upperArrowLine.SetPosition(3, new Vector3(x, upperTipY, 0f));
        upperArrowLine.SetPosition(4, new Vector3(x + headHalfW, upperHeadBaseY, 0f));

        // 下箭头（白）：整体下移，但箭头朝上（指向中线）
        float whiteOffsetY = Mathf.Max(0f, whiteArrowOffsetInCells) * cellSize;
        float lowerTipY = splitY - gap - whiteOffsetY;            // 箭尖在上
        float lowerBaseY = lowerTipY - shaftLen;                  // 箭杆起点在下
        float lowerHeadBaseY = lowerTipY - headLen;               // 箭头两翼在箭尖下方

        lowerArrowLine.SetPosition(0, new Vector3(x, lowerBaseY, 0f));
        lowerArrowLine.SetPosition(1, new Vector3(x, lowerTipY, 0f));
        lowerArrowLine.SetPosition(2, new Vector3(x - headHalfW, lowerHeadBaseY, 0f));
        lowerArrowLine.SetPosition(3, new Vector3(x, lowerTipY, 0f));
        lowerArrowLine.SetPosition(4, new Vector3(x + headHalfW, lowerHeadBaseY, 0f));
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
        if (projectileShapeMode == ProjectileShapeMode.Fixed1x1)
        {
            return FixedObstacleShape;
        }

        if (projectileShapeMode == ProjectileShapeMode.Custom)
        {
            return GetValidShapeOrFallback(customObstacleProjectileShape, FixedObstacleShape);
        }

        if (worldGround != null && worldGround.IsTestMode)
        {
            return BlackSupplyShapes[Mathf.Clamp(worldGround.CurrentObstacleShapeIndex, 0, BlackSupplyShapes.Length - 1)];
        }

        return BlackSupplyShapes[Random.Range(0, BlackSupplyShapes.Length)];
    }

    private Vector2Int[] PickWhiteShape()
    {
        if (projectileShapeMode == ProjectileShapeMode.Fixed1x1)
        {
            return FixedPitShape;
        }

        if (projectileShapeMode == ProjectileShapeMode.Custom)
        {
            return GetValidShapeOrFallback(customPitProjectileShape, FixedPitShape);
        }

        if (worldGround != null && worldGround.IsTestMode)
        {
            return WhiteSupplyShapes[Mathf.Clamp(worldGround.CurrentPitShapeIndex, 0, WhiteSupplyShapes.Length - 1)];
        }

        return WhiteSupplyShapes[Random.Range(0, WhiteSupplyShapes.Length)];
    }

    private static Vector2Int[] GetValidShapeOrFallback(Vector2Int[] shape, Vector2Int[] fallback)
    {
        if (shape == null || shape.Length == 0)
        {
            return fallback;
        }

        return shape;
    }

    private void EnsureInputSfxSource()
    {
        if (inputSfxSource == null)
        {
            inputSfxSource = GetComponent<AudioSource>();
        }

        if (inputSfxSource == null)
        {
            inputSfxSource = gameObject.AddComponent<AudioSource>();
        }

        inputSfxSource.playOnAwake = false;
        inputSfxSource.loop = false;
        inputSfxSource.spatialBlend = 0f;
    }

    private void PlayInputSfx(AudioClip clip)
    {
        if (inputSfxSource == null)
        {
            return;
        }

        AudioClip finalClip = clip != null ? clip : defaultInputSfx;
        if (finalClip == null)
        {
            return;
        }

        float volume = Mathf.Max(0f, inputSfxVolume);
        inputSfxSource.PlayOneShot(finalClip, volume);
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