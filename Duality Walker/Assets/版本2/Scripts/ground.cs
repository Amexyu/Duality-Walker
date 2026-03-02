using System.Collections.Generic;
using UnityEngine;

public class ground : MonoBehaviour
{
    [Header("玩家（可不填，自动找 BLACKMOVE）")]
    [SerializeField] private Transform player;

    [Header("基础网格")]
    [SerializeField] private float blockSize = 1f;
    [SerializeField] private float surfaceYOffset = 0f;

    [Header("生成范围")]
    [SerializeField] private int spawnAheadColumns = 50;
    [SerializeField] private int spawnBehindColumns = 15;
    [SerializeField] private int keepBehindColumns = 22;

    [Header("上下填充行数")]
    [SerializeField] private int whiteRowsAbove = 12;
    [SerializeField] private int blackRowsBelow = 14;

    [Header("地形特征延迟")]
    [SerializeField] private float featureStartDelaySeconds = 5f;
    [SerializeField] private int featureStartSafeColumns = 12;

    [Header("凸起（仅地表线附近）")]
    [SerializeField] private float bumpChance = 0.16f;

    [Header("凹陷（仅地表线附近）")]
    [SerializeField] private float dipChance = 0.14f;

    [Header("地形间隔")]
    [SerializeField] private int minFlatColumnsBetweenFeatures = 3;

    [Header("贴地修正（用于消除角色与地面细缝）")]
    [SerializeField] private float surfaceSnapTolerance = 0.01f;

    [Header("合成器重建间隔")]
    [SerializeField] private float compositeRebuildInterval = 0.08f;

    [Header("测试模式（限制障碍/坑形状）")]
    [SerializeField] private bool testMode = true;
    [SerializeField] private int testObstacleShapeIndex = 0;
    [SerializeField] private int testPitShapeIndex = 0;

    [Header("清除映射")]
    [SerializeField] private bool reverseClearMapping = true; // false: 黑->障碍 白->坑；true: 黑->坑 白->障碍

    private readonly Queue<ColumnRecord> spawnedColumns = new();
    private readonly Dictionary<int, GameObject> columnRoots = new();
    private readonly Dictionary<Vector2Int, GameObject> cellObjects = new();

    private int nextColumnIndex;
    private int featureCooldown;

    private float originX;
    private int baseSurfaceUnits;
    private Sprite runtimeSprite;

    private float featureStartTime;
    private bool featureUnlocked;
    private int featureStartColumn;

    private Rigidbody2D worldRb;
    private CompositeCollider2D worldComposite;
    private bool colliderDirty;
    private float nextCompositeRebuildTime;

    private enum FeatureType
    {
        None,
        Obstacle, // 白区黑障碍
        Pit       // 黑区白坑
    }

    private FeatureType activeFeatureType = FeatureType.None;
    private Vector2Int[] activeFeatureCells;
    private int activeFeatureStartX;
    private int activeFeatureEndX;

    // 障碍形状（y>=1，放在白区）
    private static readonly Vector2Int[][] ObstacleShapes =
    {
        new[] { new Vector2Int(0, 1) },
        new[] { new Vector2Int(0, 1), new Vector2Int(1, 1) },
        new[] { new Vector2Int(0, 1), new Vector2Int(0, 2) },
        new[] { new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(0, 2) },
        new[] { new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(1, 2) },

        // 新增难形状1：高墙+底座（更难绕过）
        new[]
        {
            new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1),
            new Vector2Int(2, 2), new Vector2Int(2, 3)
        },

        // 新增难形状2：阶梯凸起
        new[]
        {
            new Vector2Int(0, 1), new Vector2Int(1, 1),
            new Vector2Int(1, 2), new Vector2Int(2, 2), new Vector2Int(2, 3)
        }
    };

    // 坑形状（y<=0，挖黑区）
    private static readonly Vector2Int[][] PitShapes =
    {
        // 删除了原来的 1x1 小坑，避免“掉不进去”
        new[] { new Vector2Int(0, 0), new Vector2Int(0, -1) },
        new[] { new Vector2Int(0, 0), new Vector2Int(1, 0) },
        new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(1, -1) },
        new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(1, -1) },

        // 新增难形状1：三宽双深漏斗
        new[]
        {
            new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0),
            new Vector2Int(1, -1), new Vector2Int(1, -2)
        },

        // 新增难形状2：四宽双深大坑
        new[]
        {
            new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0),
            new Vector2Int(1, -1), new Vector2Int(2, -1),
            new Vector2Int(1, -2), new Vector2Int(2, -2)
        }
    };

    private struct ColumnRecord
    {
        public int xIndex;
        public GameObject root;
    }

    private void Start()
    {
        if (player == null)
        {
            BLACKMOVE auto = FindFirstObjectByType<BLACKMOVE>();
            if (auto != null)
            {
                player = auto.transform;
            }
        }

        if (player == null)
        {
            Debug.LogWarning("ground: 未找到玩家，请绑定 player 或保证场景中有 BLACKMOVE。");
            enabled = false;
            return;
        }

        if (blockSize <= 0f)
        {
            blockSize = 1f;
        }

        EnsureCompositeCollider();

        featureStartTime = Time.time + Mathf.Max(0f, featureStartDelaySeconds);
        featureUnlocked = featureStartDelaySeconds <= 0f;
        featureStartColumn = int.MaxValue;

        runtimeSprite = CreateRuntimeWhiteSprite();
        originX = transform.position.x;
        baseSurfaceUnits = GetInitialSurfaceUnits();

        int playerCol = WorldXToColumn(player.position.x);
        int startCol = playerCol - spawnBehindColumns;
        int endCol = playerCol + spawnAheadColumns;

        nextColumnIndex = startCol;
        while (nextColumnIndex <= endCol)
        {
            GenerateColumn(nextColumnIndex);
            nextColumnIndex++;
        }

        FlushCompositeGeometryIfDirty(true);
    }

    private void Update()
    {
        TryUnlockFeatures();

        int playerCol = WorldXToColumn(player.position.x);
        int targetCol = playerCol + spawnAheadColumns;

        Camera cam = Camera.main;
        if (cam != null)
        {
            float z = -cam.transform.position.z;
            float rightX = cam.ViewportToWorldPoint(new Vector3(1f, 0.5f, z)).x;
            int cameraCol = WorldXToColumn(rightX) + spawnAheadColumns;
            targetCol = Mathf.Max(targetCol, cameraCol);
        }

        while (nextColumnIndex <= targetCol)
        {
            GenerateColumn(nextColumnIndex);
            nextColumnIndex++;
        }

        int playerKeepCol = playerCol - keepBehindColumns;
        int minKeepCol = playerKeepCol;

        if (cam != null)
        {
            float z = -cam.transform.position.z;
            float leftX = cam.ViewportToWorldPoint(new Vector3(0f, 0.5f, z)).x;
            int cameraKeepCol = WorldXToColumn(leftX) - 2;
            minKeepCol = Mathf.Min(playerKeepCol, cameraKeepCol);
        }

        while (spawnedColumns.Count > 0 && spawnedColumns.Peek().xIndex < minKeepCol)
        {
            ColumnRecord oldCol = spawnedColumns.Dequeue();
            if (oldCol.root != null)
            {
                RemoveColumnCellsFromMap(oldCol.root);
                columnRoots.Remove(oldCol.xIndex);
                Destroy(oldCol.root);
                colliderDirty = true;
            }
        }

        FlushCompositeGeometryIfDirty(false);
    }

    private void EnsureCompositeCollider()
    {
        worldRb = GetComponent<Rigidbody2D>();
        if (worldRb == null)
        {
            worldRb = gameObject.AddComponent<Rigidbody2D>();
        }

        worldRb.bodyType = RigidbodyType2D.Static;
        worldRb.simulated = true;

        worldComposite = GetComponent<CompositeCollider2D>();
        if (worldComposite == null)
        {
            worldComposite = gameObject.AddComponent<CompositeCollider2D>();
        }

        worldComposite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        worldComposite.generationType = CompositeCollider2D.GenerationType.Manual;
    }

    private void FlushCompositeGeometryIfDirty(bool force)
    {
        if (!colliderDirty || worldComposite == null)
        {
            return;
        }

        worldComposite.GenerateGeometry();
        colliderDirty = false;
    }

    private void TryUnlockFeatures()
    {
        if (featureUnlocked || Time.time < featureStartTime)
        {
            return;
        }

        featureUnlocked = true;
        featureCooldown = 0;

        int playerCol = WorldXToColumn(player.position.x);
        featureStartColumn = playerCol + Mathf.Max(1, featureStartSafeColumns);
    }

    private void GenerateColumn(int xIndex)
    {
        TryStartFeatureAtColumn(xIndex);

        float worldX = ColumnToWorldX(xIndex);

        GameObject root = new("col_" + xIndex);
        root.transform.SetParent(transform, true);
        columnRoots[xIndex] = root;

        int topBlackY = baseSurfaceUnits;

        for (int y = baseSurfaceUnits - blackRowsBelow; y <= topBlackY; y++)
        {
            Vector3 pos = new(worldX, y * blockSize, 0f);
            SpawnOrReplaceBlock(root.transform, pos, Color.black, true, "Black");
        }

        for (int y = topBlackY + 1; y <= topBlackY + whiteRowsAbove; y++)
        {
            Vector3 pos = new(worldX, y * blockSize, 0f);
            SpawnOrReplaceBlock(root.transform, pos, Color.white, false, "White");
        }

        ApplyFeatureToColumn(root.transform, xIndex, worldX);

        spawnedColumns.Enqueue(new ColumnRecord { xIndex = xIndex, root = root });

        if (activeFeatureType != FeatureType.None && xIndex >= activeFeatureEndX)
        {
            activeFeatureType = FeatureType.None;
            activeFeatureCells = null;
            featureCooldown = minFlatColumnsBetweenFeatures;
        }
    }

    private void TryStartFeatureAtColumn(int xIndex)
    {
        if (!featureUnlocked || xIndex < featureStartColumn)
        {
            return;
        }

        if (activeFeatureType != FeatureType.None)
        {
            return;
        }

        if (featureCooldown > 0)
        {
            featureCooldown--;
            return;
        }

        float r = Random.value;

        if (r < bumpChance)
        {
            StartFeature(xIndex, FeatureType.Obstacle, PickObstacleShape());
            return;
        }

        if (r < bumpChance + dipChance)
        {
            StartFeature(xIndex, FeatureType.Pit, PickPitShape());
        }
    }

    private void StartFeature(int startX, FeatureType type, Vector2Int[] shape)
    {
        activeFeatureType = type;
        activeFeatureCells = shape;
        activeFeatureStartX = startX;
        activeFeatureEndX = startX + GetShapeWidth(shape) - 1;
    }

    private int GetShapeWidth(Vector2Int[] shape)
    {
        int maxX = 0;
        for (int i = 0; i < shape.Length; i++)
        {
            if (shape[i].x > maxX)
            {
                maxX = shape[i].x;
            }
        }

        return maxX + 1;
    }

    private void ApplyFeatureToColumn(Transform root, int xIndex, float worldX)
    {
        if (activeFeatureType == FeatureType.None || activeFeatureCells == null)
        {
            return;
        }

        int localX = xIndex - activeFeatureStartX;
        if (localX < 0 || xIndex > activeFeatureEndX)
        {
            return;
        }

        for (int i = 0; i < activeFeatureCells.Length; i++)
        {
            Vector2Int c = activeFeatureCells[i];
            if (c.x != localX)
            {
                continue;
            }

            int yUnits = baseSurfaceUnits + c.y;
            Vector3 pos = new(worldX, yUnits * blockSize, 0f);

            if (activeFeatureType == FeatureType.Obstacle)
            {
                if (yUnits <= baseSurfaceUnits)
                {
                    continue;
                }

                SpawnOrReplaceBlock(root, pos, Color.black, true, "Obstacle");
            }
            else
            {
                if (yUnits > baseSurfaceUnits)
                {
                    continue;
                }

                SpawnOrReplaceBlock(root, pos, Color.white, false, "Pit");
            }
        }
    }

    private void SpawnOrReplaceBlock(Transform parent, Vector3 worldPos, Color color, bool hasCollider, string namePrefix)
    {
        int xIndex = WorldXToCellIndex(worldPos.x);
        int yIndex = Mathf.RoundToInt(worldPos.y / blockSize);

        worldPos.x = ColumnToWorldX(xIndex);
        worldPos.y = yIndex * blockSize;

        Vector2Int key = MakeCellKey(worldPos.x, worldPos.y);

        if (cellObjects.TryGetValue(key, out GameObject old) && old != null)
        {
            if (old.GetComponent<Collider2D>() != null)
            {
                colliderDirty = true;
            }

            Destroy(old);
            cellObjects.Remove(key);
        }

        GameObject go = new(namePrefix);
        go.transform.SetParent(parent, true);
        go.transform.position = worldPos;
        go.transform.localScale = new Vector3(blockSize, blockSize, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = runtimeSprite;
        sr.color = color;
        sr.sortingOrder = 0;

        FeatureCell marker = go.AddComponent<FeatureCell>();
        if (namePrefix == "Obstacle")
        {
            marker.cellType = FeatureCellType.Obstacle;
        }
        else if (namePrefix == "Pit")
        {
            marker.cellType = FeatureCellType.Pit;
        }
        else
        {
            marker.cellType = FeatureCellType.None;
        }

        if (hasCollider)
        {
            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.compositeOperation = Collider2D.CompositeOperation.Merge;
            col.isTrigger = false;
            colliderDirty = true;
        }

        cellObjects[key] = go;
    }

    private void RemoveColumnCellsFromMap(GameObject columnRoot)
    {
        if (columnRoot == null)
        {
            return;
        }

        for (int i = 0; i < columnRoot.transform.childCount; i++)
        {
            Transform ch = columnRoot.transform.GetChild(i);
            Vector2Int key = MakeCellKey(ch.position.x, ch.position.y);

            if (cellObjects.TryGetValue(key, out GameObject exist) && exist == ch.gameObject)
            {
                cellObjects.Remove(key);
            }
        }
    }

    private int GetInitialSurfaceUnits()
    {
        float feetY;

        Collider2D playerCol = player.GetComponent<Collider2D>();
        if (playerCol != null)
        {
            feetY = playerCol.bounds.min.y;
        }
        else
        {
            feetY = player.position.y - blockSize * 0.5f;
        }

        float blackCenterY = feetY - blockSize * 0.5f + surfaceYOffset;

        int units = Mathf.FloorToInt(blackCenterY / blockSize);
        float topFaceY = units * blockSize + blockSize * 0.5f;

        if (topFaceY < feetY - surfaceSnapTolerance)
        {
            units += 1;
        }

        return units;
    }

    private int WorldXToColumn(float worldX)
    {
        return Mathf.FloorToInt((worldX - originX) / blockSize);
    }

    private int WorldXToCellIndex(float worldX)
    {
        return Mathf.RoundToInt((worldX - originX) / blockSize);
    }

    private float ColumnToWorldX(int xIndex)
    {
        return originX + xIndex * blockSize;
    }

    private Vector2Int MakeCellKey(float worldX, float worldY)
    {
        return new Vector2Int(
            WorldXToCellIndex(worldX),
            Mathf.RoundToInt(worldY / blockSize)
        );
    }

    private Sprite CreateRuntimeWhiteSprite()
    {
        Texture2D tex = new(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();

        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        return Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }

    private Vector2Int[] PickObstacleShape()
    {
        if (!testMode)
        {
            return ObstacleShapes[Random.Range(0, ObstacleShapes.Length)];
        }

        int idx = Mathf.Clamp(testObstacleShapeIndex, 0, ObstacleShapes.Length - 1);
        return ObstacleShapes[idx];
    }

    private Vector2Int[] PickPitShape()
    {
        if (!testMode)
        {
            return PitShapes[Random.Range(0, PitShapes.Length)];
        }

        int idx = Mathf.Clamp(testPitShapeIndex, 0, PitShapes.Length - 1);
        return PitShapes[idx];
    }

    public float CellSize => blockSize;

    public bool TryClearFeatureCell(Vector3 worldPos, bool isBlackBlock)
    {
        Vector2Int key = MakeCellKey(worldPos.x, worldPos.y);
        if (!cellObjects.TryGetValue(key, out GameObject cell) || cell == null)
        {
            return false;
        }

        FeatureCell feature = cell.GetComponent<FeatureCell>();
        if (feature == null)
        {
            return false;
        }

        bool isObstacleBlock = reverseClearMapping ? !isBlackBlock : isBlackBlock;
        bool canClearObstacle = isObstacleBlock && feature.cellType == FeatureCellType.Obstacle;
        bool canClearPit = !isObstacleBlock && feature.cellType == FeatureCellType.Pit;
        if (!canClearObstacle && !canClearPit)
        {
            return false;
        }

        if (canClearObstacle)
        {
            if (cell.GetComponent<Collider2D>() != null)
            {
                colliderDirty = true;
            }

            cellObjects.Remove(key);
            Destroy(cell);
            return true;
        }

        // 填坑：把坑位替换为黑色可碰撞地块
        FillPitCell(cell, key, worldPos);
        return true;
    }

    private void FillPitCell(GameObject pitCell, Vector2Int key, Vector3 worldPos)
    {
        Transform parent = pitCell != null ? pitCell.transform.parent : transform;

        if (pitCell != null)
        {
            cellObjects.Remove(key);
            Destroy(pitCell);
        }

        GameObject go = new("FilledPit");
        go.transform.SetParent(parent, true);
        go.transform.position = new Vector3(
            ColumnToWorldX(WorldXToCellIndex(worldPos.x)),
            Mathf.RoundToInt(worldPos.y / blockSize) * blockSize,
            0f
        );
        go.transform.localScale = new Vector3(blockSize, blockSize, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = runtimeSprite;
        sr.color = Color.black;
        sr.sortingOrder = 0;

        FeatureCell marker = go.AddComponent<FeatureCell>();
        marker.cellType = FeatureCellType.None;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.compositeOperation = Collider2D.CompositeOperation.Merge;
        col.isTrigger = false;

        cellObjects[key] = go;
        colliderDirty = true;

        TryLiftPlayerFromFilledPit(go.transform.position.y);
    }

    private void TryLiftPlayerFromFilledPit(float filledCellCenterY)
    {
        if (player == null)
        {
            return;
        }

        Collider2D playerCol = player.GetComponent<Collider2D>();
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerCol == null)
        {
            return;
        }

        float cellTop = filledCellCenterY + blockSize * 0.5f;
        float footY = playerCol.bounds.min.y;

        // 只有脚在地块顶部以下，才做抬升
        if (footY >= cellTop)
        {
            return;
        }

        float footOffset = player.position.y - footY;
        Vector3 p = player.position;
        p.y = cellTop + footOffset + 0.02f;
        player.position = p;

        if (playerRb != null)
        {
            Vector2 v = playerRb.linearVelocity;
            if (v.y < 0f) v.y = 0f;
            playerRb.linearVelocity = v;
        }
    }

    public void RefreshCompositeCollider()
    {
        FlushCompositeGeometryIfDirty(true);
    }

    public bool IsTestMode => testMode;

    public int CurrentObstacleShapeIndex
    {
        get { return Mathf.Clamp(testObstacleShapeIndex, 0, ObstacleShapes.Length - 1); }
    }

    public int CurrentPitShapeIndex
    {
        get { return Mathf.Clamp(testPitShapeIndex, 0, PitShapes.Length - 1); }
    }
}