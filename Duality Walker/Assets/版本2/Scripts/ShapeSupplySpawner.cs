using System.Collections.Generic;
using UnityEngine;

public class ShapeSupplySpawner : MonoBehaviour
{
    [SerializeField] private float respawnDelay = 0.8f;
    [SerializeField] private int maxBlackSupply = 3;
    [SerializeField] private int maxWhiteSupply = 3;

    [Header("供给区（Viewport Rect: x,y,w,h）")]
    [SerializeField] private Rect blackAreaViewport = new Rect(0.80f, 0.62f, 0.18f, 0.33f);
    [SerializeField] private Rect whiteAreaViewport = new Rect(0.80f, 0.05f, 0.18f, 0.33f);

    [SerializeField] private float areaPaddingInCells = 0.25f;
    [SerializeField] private float itemGapInCells = 0.2f;

    // 字段区新增
    [SerializeField] private bool easyMode = true;
    [SerializeField] private bool fixedShapeForTest = true;
    [SerializeField] private int blackFixedShapeIndex = 0;
    [SerializeField] private int whiteFixedShapeIndex = 0;
    [SerializeField] private bool testMode = true;
    [SerializeField] private int testBlackShapeIndex = 0; // 障碍形状索引
    [SerializeField] private int testWhiteShapeIndex = 0; // 坑形状索引

    [Header("供给区边框")]
    [SerializeField] private bool showAreaFrame = true;
    [SerializeField] private Color blackAreaFrameColor = Color.black;
    [SerializeField] private Color whiteAreaFrameColor = Color.white;
    [SerializeField] private float frameWidth = 0.03f;

    private Camera cam;
    private ground worldGround;
    private float cellSize = 1f;
    private float blackTimer;
    private float whiteTimer;
    private float gridOriginX;

    private Material frameMaterial;

    private LineRenderer blackFrame;
    private LineRenderer whiteFrame;

    private readonly List<DraggableAssembly> blackAssemblies = new();
    private readonly List<DraggableAssembly> whiteAssemblies = new();

    // 黑块：与 ground 的 ObstacleShapes 对齐
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

    // 白块：与 ground 的 PitShapes 对齐
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

        EnsureAreaFrames();
        UpdateAreaFrames();

        SpawnBlack();
        SpawnWhite();
    }

    private void Update()
    {
        CleanupNulls();

        blackTimer += Time.deltaTime;
        if (blackTimer >= respawnDelay)
        {
            blackTimer = 0f;
            if (blackAssemblies.Count < maxBlackSupply) SpawnBlack();
        }

        whiteTimer += Time.deltaTime;
        if (whiteTimer >= respawnDelay)
        {
            whiteTimer = 0f;
            if (whiteAssemblies.Count < maxWhiteSupply) SpawnWhite();
        }
    }

    private void LateUpdate()
    {
        KeepSupplyInSlot();
        UpdateAreaFrames();
    }

    private void CleanupNulls()
    {
        for (int i = blackAssemblies.Count - 1; i >= 0; i--)
        {
            if (blackAssemblies[i] == null) blackAssemblies.RemoveAt(i);
        }

        for (int i = whiteAssemblies.Count - 1; i >= 0; i--)
        {
            if (whiteAssemblies[i] == null) whiteAssemblies.RemoveAt(i);
        }
    }

    private void KeepSupplyInSlot()
    {
        LayoutAssembliesInArea(blackAssemblies, blackAreaViewport);
        LayoutAssembliesInArea(whiteAssemblies, whiteAreaViewport);
    }

    private void LayoutAssembliesInArea(List<DraggableAssembly> assemblies, Rect viewportArea)
    {
        Rect worldArea = ViewportRectToWorldRect(viewportArea);

        float pad = areaPaddingInCells * cellSize;
        float gap = itemGapInCells * cellSize;

        float minX = worldArea.xMin + pad;
        float maxX = worldArea.xMax - pad;
        float minY = worldArea.yMin + pad;
        float maxY = worldArea.yMax - pad;

        float cursorX = minX;
        float cursorY = maxY;
        float rowHeight = 0f;

        for (int i = 0; i < assemblies.Count; i++)
        {
            var a = assemblies[i];
            if (a == null || a.IsDragging) continue;

            GetAssemblyWorldSize(a, out float w, out float h);

            if (cursorX + w > maxX)
            {
                cursorX = minX;
                cursorY -= rowHeight + gap;
                rowHeight = 0f;
            }

            if (cursorY - h < minY)
            {
                a.transform.position = new Vector3(maxX - w * 0.5f, minY + h * 0.5f, 0f);
                continue;
            }

            a.transform.position = new Vector3(cursorX + w * 0.5f, cursorY - h * 0.5f, 0f);

            cursorX += w + gap;
            rowHeight = Mathf.Max(rowHeight, h);
        }
    }

    private void GetAssemblyWorldSize(DraggableAssembly assembly, out float width, out float height)
    {
        var blocks = assembly.GetComponentsInChildren<ShapeBlock>();
        if (blocks.Length == 0)
        {
            width = cellSize;
            height = cellSize;
            return;
        }

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        for (int i = 0; i < blocks.Length; i++)
        {
            Vector3 lp = blocks[i].transform.localPosition;
            minX = Mathf.Min(minX, lp.x);
            maxX = Mathf.Max(maxX, lp.x);
            minY = Mathf.Min(minY, lp.y);
            maxY = Mathf.Max(maxY, lp.y);
        }

        width = (maxX - minX) + cellSize;
        height = (maxY - minY) + cellSize;
    }

    private Rect ViewportRectToWorldRect(Rect vpRect)
    {
        Vector3 bl = ViewportToWorld(vpRect.xMin, vpRect.yMin);
        Vector3 tr = ViewportToWorld(vpRect.xMax, vpRect.yMax);

        return Rect.MinMaxRect(
            Mathf.Min(bl.x, tr.x),
            Mathf.Min(bl.y, tr.y),
            Mathf.Max(bl.x, tr.x),
            Mathf.Max(bl.y, tr.y)
        );
    }

    private void SpawnBlack()
    {
        Vector3 p = ViewportToWorld(blackAreaViewport.center.x, blackAreaViewport.center.y);

        // 黑块要去填坑 => 用“坑形状”
        var shape = PickWhiteShape();

        var assembly = CreateAssembly("BlackSupply", true, p, shape);
        assembly.SetInSupplySlot(true);
        assembly.DragStarted += OnBlackDragStarted;
        blackAssemblies.Add(assembly);
    }

    private void SpawnWhite()
    {
        Vector3 p = ViewportToWorld(whiteAreaViewport.center.x, whiteAreaViewport.center.y);

        // 白块要去填障碍 => 用“障碍形状”
        var shape = PickBlackShape();

        var assembly = CreateAssembly("WhiteSupply", false, p, shape);
        assembly.SetInSupplySlot(true);
        assembly.DragStarted += OnWhiteDragStarted;
        whiteAssemblies.Add(assembly);
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

    private void OnBlackDragStarted(DraggableAssembly assembly)
    {
        if (assembly == null) return;
        assembly.DragStarted -= OnBlackDragStarted;
        blackAssemblies.Remove(assembly);
    }

    private void OnWhiteDragStarted(DraggableAssembly assembly)
    {
        if (assembly == null) return;
        assembly.DragStarted -= OnWhiteDragStarted;
        whiteAssemblies.Remove(assembly);
    }

    private DraggableAssembly CreateAssembly(string name, bool black, Vector3 worldPos, Vector2Int[] shape)
    {
        var root = new GameObject(name);
        root.transform.position = SnapToGroundGrid(worldPos);

        var assembly = root.AddComponent<DraggableAssembly>();
        var sprite = CreatePixelSprite();

        Vector2Int anchor = shape[0];

        for (int i = 0; i < shape.Length; i++)
        {
            var c = shape[i];
            var go = new GameObject("Block");
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = new Vector3((c.x - anchor.x) * cellSize, (c.y - anchor.y) * cellSize, 0f);
            go.transform.localScale = Vector3.one * cellSize;

            var b = go.AddComponent<ShapeBlock>();
            b.Initialize(black, sprite);
        }

        return assembly;
    }

    private Vector3 ViewportToWorld(float vx, float vy)
    {
        if (cam == null) cam = Camera.main;
        float z = Mathf.Abs(cam.transform.position.z);
        var w = cam.ViewportToWorldPoint(new Vector3(vx, vy, z));
        w.z = 0f;
        return w;
    }

    private Vector3 SnapToGroundGrid(Vector3 p)
    {
        p.x = gridOriginX + Mathf.Round((p.x - gridOriginX) / cellSize) * cellSize;
        p.y = Mathf.Round(p.y / cellSize) * cellSize;
        p.z = 0f;
        return p;
    }

    private Sprite CreatePixelSprite()
    {
        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }

    private void EnsureAreaFrames()
    {
        if (!showAreaFrame)
        {
            return;
        }

        if (frameMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                frameMaterial = new Material(shader);
            }
        }

        if (blackFrame == null)
        {
            blackFrame = CreateAreaFrame("BlackAreaFrame", blackAreaFrameColor, 120);
        }

        if (whiteFrame == null)
        {
            whiteFrame = CreateAreaFrame("WhiteAreaFrame", whiteAreaFrameColor, 120);
        }
    }

    private LineRenderer CreateAreaFrame(string name, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);

        var lr = go.AddComponent<LineRenderer>();
        lr.loop = true;
        lr.useWorldSpace = true;
        lr.positionCount = 4;
        lr.startWidth = frameWidth;
        lr.endWidth = frameWidth;
        lr.startColor = color;
        lr.endColor = color;
        lr.sortingOrder = order;
        lr.numCapVertices = 0;
        lr.numCornerVertices = 0;

        if (frameMaterial != null)
        {
            lr.material = frameMaterial;
        }

        return lr;
    }

    private void UpdateAreaFrames()
    {
        if (!showAreaFrame)
        {
            if (blackFrame != null) blackFrame.enabled = false;
            if (whiteFrame != null) whiteFrame.enabled = false;
            return;
        }

        EnsureAreaFrames();

        if (blackFrame != null)
        {
            blackFrame.enabled = true;
            SetFrameRect(blackFrame, ViewportRectToWorldRect(blackAreaViewport));
        }

        if (whiteFrame != null)
        {
            whiteFrame.enabled = true;
            SetFrameRect(whiteFrame, ViewportRectToWorldRect(whiteAreaViewport));
        }
    }

    private void SetFrameRect(LineRenderer lr, Rect r)
    {
        lr.SetPosition(0, new Vector3(r.xMin, r.yMin, 0f));
        lr.SetPosition(1, new Vector3(r.xMin, r.yMax, 0f));
        lr.SetPosition(2, new Vector3(r.xMax, r.yMax, 0f));
        lr.SetPosition(3, new Vector3(r.xMax, r.yMin, 0f));
    }

    private void OnDestroy()
    {
        if (frameMaterial != null)
        {
            Destroy(frameMaterial);
        }
    }
}