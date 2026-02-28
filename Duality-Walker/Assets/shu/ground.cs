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
    [SerializeField] private int minBumpWidth = 1;
    [SerializeField] private int maxBumpWidth = 3;
    [SerializeField] private int maxBumpHeight = 2;

    [Header("凹陷（仅地表线附近）")]
    [SerializeField] private float dipChance = 0.14f;
    [SerializeField] private int minDipWidth = 1;
    [SerializeField] private int maxDipWidth = 3;
    [SerializeField] private int maxDipDepth = 2;

    [Header("地形间隔")]
    [SerializeField] private int minFlatColumnsBetweenFeatures = 3;

    private readonly Queue<ColumnRecord> spawnedColumns = new Queue<ColumnRecord>();
    private readonly Dictionary<int, GameObject> columnRoots = new Dictionary<int, GameObject>();
    private readonly Dictionary<Vector2Int, GameObject> cellObjects = new Dictionary<Vector2Int, GameObject>();

    private int nextColumnIndex;
    private int featureRemaining;
    private int featureCooldown;
    private FeatureType currentFeatureType;
    private int currentFeatureSize;

    private float originX;
    private int baseSurfaceUnits;
    private Sprite runtimeSprite;

    private float featureStartTime;
    private bool featureUnlocked;
    private int featureStartColumn;

    private Rigidbody2D worldRb;
    private CompositeCollider2D worldComposite;
    private bool colliderDirty;

    private enum FeatureType
    {
        None,
        Bump,
        Dip
    }

    private struct ColumnRecord
    {
        public int xIndex;
        public GameObject root;
    }

    private void Start()
    {
        if (player == null)
        {
            BLACKMOVE auto = FindObjectOfType<BLACKMOVE>();
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

        FlushCompositeGeometryIfDirty();
    }

    private void Update()
    {
        TryUnlockFeatures();

        int playerCol = WorldXToColumn(player.position.x);
        int targetCol = playerCol + spawnAheadColumns;

        while (nextColumnIndex <= targetCol)
        {
            GenerateColumn(nextColumnIndex);
            nextColumnIndex++;
        }

        int playerKeepCol = playerCol - keepBehindColumns;

        int minKeepCol = playerKeepCol;
        Camera cam = Camera.main;
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

        FlushCompositeGeometryIfDirty();
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

    private void FlushCompositeGeometryIfDirty()
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
        featureRemaining = 0;
        featureCooldown = 0;
        currentFeatureType = FeatureType.None;
        currentFeatureSize = 0;

        int playerCol = WorldXToColumn(player.position.x);
        featureStartColumn = playerCol + Mathf.Max(1, featureStartSafeColumns);
    }

    private void GenerateColumn(int xIndex)
    {
        GetFeatureForCurrentColumn(xIndex, out FeatureType featureType, out int featureSize);
        float worldX = ColumnToWorldX(xIndex);

        GameObject root = new GameObject("col_" + xIndex);
        root.transform.SetParent(transform, true);
        columnRoots[xIndex] = root;

        int topBlackY = baseSurfaceUnits;

        // 凹陷：把该列地表下移（不是挖空）
        if (featureType == FeatureType.Dip)
        {
            topBlackY = baseSurfaceUnits - Mathf.Max(1, featureSize);
        }

        // 黑色实心区（有碰撞）——一直填到 topBlackY
        for (int y = baseSurfaceUnits - blackRowsBelow; y <= topBlackY; y++)
        {
            Vector3 pos = new Vector3(worldX, y * blockSize, 0f);
            SpawnOrReplaceBlock(root.transform, pos, Color.black, true, "Black");
        }

        // 白色区（无碰撞）——从 topBlackY+1 往上填
        for (int y = topBlackY + 1; y <= topBlackY + whiteRowsAbove; y++)
        {
            Vector3 pos = new Vector3(worldX, y * blockSize, 0f);
            SpawnOrReplaceBlock(root.transform, pos, Color.white, false, "White");
        }

        // 凸起：在当前地表 topBlackY 之上继续长黑块
        if (featureType == FeatureType.Bump)
        {
            for (int h = 1; h <= Mathf.Max(1, featureSize); h++)
            {
                Vector3 pos = new Vector3(worldX, (topBlackY + h) * blockSize, 0f);
                SpawnOrReplaceBlock(root.transform, pos, Color.black, true, "Bump");
            }
        }

        spawnedColumns.Enqueue(new ColumnRecord { xIndex = xIndex, root = root });
    }

    private void GetFeatureForCurrentColumn(int xIndex, out FeatureType type, out int size)
    {
        type = FeatureType.None;
        size = 0;

        if (!featureUnlocked)
        {
            currentFeatureType = FeatureType.None;
            currentFeatureSize = 0;
            return;
        }

        if (xIndex < featureStartColumn)
        {
            return;
        }

        if (featureRemaining > 0)
        {
            featureRemaining--;
            type = currentFeatureType;
            size = currentFeatureSize;
            return;
        }

        if (featureCooldown > 0)
        {
            featureCooldown--;
            currentFeatureType = FeatureType.None;
            currentFeatureSize = 0;
            return;
        }

        float r = Random.value;

        if (r < bumpChance)
        {
            currentFeatureType = FeatureType.Bump;
            currentFeatureSize = Random.Range(1, maxBumpHeight + 1);
            featureRemaining = Random.Range(minBumpWidth, maxBumpWidth + 1) - 1;
            featureCooldown = minFlatColumnsBetweenFeatures;

            type = currentFeatureType;
            size = currentFeatureSize;
            return;
        }

        if (r < bumpChance + dipChance)
        {
            currentFeatureType = FeatureType.Dip;
            currentFeatureSize = Random.Range(1, maxDipDepth + 1);
            featureRemaining = Random.Range(minDipWidth, maxDipWidth + 1) - 1;
            featureCooldown = minFlatColumnsBetweenFeatures;

            type = currentFeatureType;
            size = currentFeatureSize;
            return;
        }

        currentFeatureType = FeatureType.None;
        currentFeatureSize = 0;
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

        GameObject go = new GameObject(namePrefix);
        go.transform.SetParent(parent, true);
        go.transform.position = worldPos;
        go.transform.localScale = new Vector3(blockSize, blockSize, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = runtimeSprite;
        sr.color = color;
        sr.sortingOrder = 0;

        if (hasCollider)
        {
            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.usedByComposite = true;
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
        Collider2D playerCol = player.GetComponent<Collider2D>();
        if (playerCol != null)
        {
            float feetY = playerCol.bounds.min.y;
            float blackTopY = feetY - blockSize * 0.5f + surfaceYOffset;
            return Mathf.RoundToInt(blackTopY / blockSize);
        }

        float fallbackY = player.position.y - blockSize + surfaceYOffset;
        return Mathf.RoundToInt(fallbackY / blockSize);
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
        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();

        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        return Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }
}
