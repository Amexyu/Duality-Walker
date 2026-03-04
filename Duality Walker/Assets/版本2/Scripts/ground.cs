using System.Collections.Generic;
using UnityEngine;

public class ground : MonoBehaviour
{
    [Header("��ң��ɲ���Զ��� BLACKMOVE��")]
    [SerializeField] private Transform player;

    [Header("��������")]
    [SerializeField] private float blockSize = 1f;
    [SerializeField] private float surfaceYOffset = 0f;

    [Header("���ɷ�Χ")]
    [SerializeField] private int spawnAheadColumns = 50;
    [SerializeField] private int spawnBehindColumns = 15;
    [SerializeField] private int keepBehindColumns = 22;

    [Header("�����������")]
    [SerializeField] private int whiteRowsAbove = 12;
    [SerializeField] private int blackRowsBelow = 14;

    [Header("���������ӳ�")]
    [SerializeField] private float featureStartDelaySeconds = 5f;
    [SerializeField] private int featureStartSafeColumns = 12;

    [Header("͹�𣨽��ر��߸�����")]
    [SerializeField] private float bumpChance = 0.16f;

    [Header("���ݣ����ر��߸�����")]
    [SerializeField] private float dipChance = 0.14f;

    [Header("���μ��")]
    [SerializeField] private int minFlatColumnsBetweenFeatures = 3;

    [Header("����΢���Σ�1͹1��ѭ����")]
    [SerializeField] private bool enableAlternatingMicroPattern = true;
    [SerializeField][Range(0f, 1f)] private float alternatingPatternChance = 0.08f;
    [SerializeField] private int alternatingPatternMinLength = 6;
    [SerializeField] private int alternatingPatternMaxLength = 14;
    [SerializeField] private bool alternatingPatternRandomStartType = true;

    [Header("�������ǰ��ͻ�����Σ�������ǰ����")]
    [SerializeField] private bool enableCameraFrontSurprise = true;
    [SerializeField][Range(0f, 1f)] private float cameraFrontSurpriseChance = 0.22f;
    [SerializeField][Range(0f, 1f)] private float cameraFrontBumpRatio = 0.5f; // 0=ȫ���� 1=ȫ͹��
    [SerializeField] private float assistLineViewportX = 0.5f;
    [SerializeField] private int cameraFrontStartOffsetColumns = 2;
    [SerializeField] private int cameraFrontRightPaddingColumns = 2;

    [Header("��������������������ɫ�����ϸ�죩")]
    [SerializeField] private float surfaceSnapTolerance = 0.01f;

    [Header("��Ⱦ��϶���������Ӿ���")]
    [SerializeField] private float blockOverlap = 0.01f;

    [Header("�ϳ����ؽ����")]
    [SerializeField] private float compositeRebuildInterval = 0.08f;

    [Header("����ģʽ�������ϰ�/����״��")]
    [SerializeField] private bool testMode = true;
    [SerializeField] private int testObstacleShapeIndex = 0;
    [SerializeField] private int testPitShapeIndex = 0;

    [Header("���ӳ��")]
    [SerializeField] private bool reverseClearMapping = true; // false: ��->�ϰ� ��->�ӣ�true: ��->�� ��->�ϰ�

    [Header("������߿��ϰ�/��/��ӣ�")]
    [SerializeField] private bool useFeatureCellBorder = true;
    [SerializeField] private float featureBorderScale = 1.1f;
    [SerializeField] private int featureBorderSortingOffset = -1;
    [SerializeField] private Color obstacleBorderColor = Color.white;
    [SerializeField] private Color pitBorderColor = Color.black;
    [SerializeField] private Color filledPitBorderColor = Color.white;

    [Header("�ϰ����ɫ����")]
    [SerializeField] private bool useObstacleFillColorTransition = true;
    [SerializeField] private float obstacleFillColorTransitionDuration = 0.2f;
    [SerializeField] private bool fadeOutObstacleBorderOnFill = true;

    [Header("�����ɫ����")]
    [SerializeField] private bool usePitFillColorTransition = true;
    [SerializeField] private float pitFillColorTransitionDuration = 0.2f;

    [Header("������ͼ���л�")]
    [SerializeField] private bool areaSwapped;

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
        Obstacle,
        Pit
    }

    private FeatureType activeFeatureType = FeatureType.None;
    private Vector2Int[] activeFeatureCells;
    private int activeFeatureStartX;
    private int activeFeatureEndX;

    private bool activeAlternatingPattern;
    private int alternatingPatternStartX;
    private int alternatingPatternEndX;
    private bool alternatingPatternStartWithBump;

    private static readonly Vector2Int[][] ObstacleShapes =
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

    private static readonly Vector2Int[][] PitShapes =
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
            Debug.LogWarning("ground: δ�ҵ���ң���� player ��֤�������� BLACKMOVE��");
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
            SpawnOrReplaceBlock(root.transform, pos, true, "Black");
        }

        for (int y = topBlackY + 1; y <= topBlackY + whiteRowsAbove; y++)
        {
            Vector3 pos = new(worldX, y * blockSize, 0f);
            SpawnOrReplaceBlock(root.transform, pos, false, "White");
        }

        if (activeAlternatingPattern)
        {
            ApplyAlternatingPatternToColumn(root.transform, xIndex, worldX);
        }
        else
        {
            ApplyFeatureToColumn(root.transform, xIndex, worldX);
        }

        spawnedColumns.Enqueue(new ColumnRecord { xIndex = xIndex, root = root });

        if (activeAlternatingPattern && xIndex >= alternatingPatternEndX)
        {
            activeAlternatingPattern = false;
            featureCooldown = minFlatColumnsBetweenFeatures;
        }

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

        if (activeAlternatingPattern || activeFeatureType != FeatureType.None)
        {
            return;
        }

        if (featureCooldown > 0)
        {
            featureCooldown--;
            return;
        }

        if (TryStartCameraFrontSurpriseAtColumn(xIndex))
        {
            return;
        }

        if (enableAlternatingMicroPattern && Random.value < alternatingPatternChance)
        {
            StartAlternatingPattern(xIndex);
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

    private bool TryStartCameraFrontSurpriseAtColumn(int xIndex)
    {
        if (!enableCameraFrontSurprise)
        {
            return false;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            return false;
        }

        float z = -cam.transform.position.z;
        float assistX = cam.ViewportToWorldPoint(new Vector3(assistLineViewportX, 0.5f, z)).x;
        float rightX = cam.ViewportToWorldPoint(new Vector3(1f, 0.5f, z)).x;

        float minX = assistX + Mathf.Max(0, cameraFrontStartOffsetColumns) * blockSize;
        float maxX = rightX - Mathf.Max(0, cameraFrontRightPaddingColumns) * blockSize;
        if (maxX <= minX)
        {
            return false;
        }

        float worldX = ColumnToWorldX(xIndex);
        if (worldX < minX || worldX > maxX)
        {
            return false;
        }

        if (Random.value > cameraFrontSurpriseChance)
        {
            return false;
        }

        bool placeBump = Random.value < cameraFrontBumpRatio;
        if (placeBump)
        {
            StartFeature(xIndex, FeatureType.Obstacle, PickObstacleShape());
        }
        else
        {
            StartFeature(xIndex, FeatureType.Pit, PickPitShape());
        }

        return true;
    }

    private void StartFeature(int startX, FeatureType type, Vector2Int[] shape)
    {
        activeFeatureType = type;
        activeFeatureCells = shape;
        activeFeatureStartX = startX;
        activeFeatureEndX = startX + GetShapeWidth(shape) - 1;
    }

    private void StartAlternatingPattern(int startX)
    {
        activeAlternatingPattern = true;
        alternatingPatternStartX = startX;

        int minLen = Mathf.Max(2, alternatingPatternMinLength);
        int maxLen = Mathf.Max(minLen, alternatingPatternMaxLength);
        int length = Random.Range(minLen, maxLen + 1);

        alternatingPatternEndX = startX + length - 1;

        if (alternatingPatternRandomStartType)
        {
            alternatingPatternStartWithBump = Random.value < 0.5f;
        }
        else
        {
            alternatingPatternStartWithBump = true;
        }

        activeFeatureType = FeatureType.None;
        activeFeatureCells = null;
    }

    private void ApplyAlternatingPatternToColumn(Transform root, int xIndex, float worldX)
    {
        int local = xIndex - alternatingPatternStartX;
        if (local < 0 || xIndex > alternatingPatternEndX)
        {
            return;
        }

        bool isEven = (local % 2) == 0;
        bool placeBump = isEven ? alternatingPatternStartWithBump : !alternatingPatternStartWithBump;

        if (placeBump)
        {
            Vector3 bumpPos = new(worldX, (baseSurfaceUnits + 1) * blockSize, 0f);
            SpawnOrReplaceBlock(root, bumpPos, true, "Obstacle");
        }
        else
        {
            Vector3 pitPos = new(worldX, baseSurfaceUnits * blockSize, 0f);
            SpawnOrReplaceBlock(root, pitPos, false, "Pit");
        }
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

                SpawnOrReplaceBlock(root, pos, true, "Obstacle");
            }
            else
            {
                if (yUnits > baseSurfaceUnits)
                {
                    continue;
                }

                SpawnOrReplaceBlock(root, pos, false, "Pit");
            }
        }
    }

    private void SpawnOrReplaceBlock(Transform parent, Vector3 worldPos, bool isBlackBlock, string namePrefix)
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

        float renderSize = blockSize + Mathf.Max(0f, blockOverlap);
        go.transform.localScale = new Vector3(renderSize, renderSize, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = runtimeSprite;
        sr.color = isBlackBlock ? Color.black : Color.white;
        sr.sortingOrder = 0;

        FeatureCell marker = go.AddComponent<FeatureCell>();
        marker.isBlackBlock = isBlackBlock;
        if (namePrefix == "Obstacle")
        {
            marker.cellType = FeatureCellType.Obstacle;
            TryApplyFeatureBorder(go, sr, obstacleBorderColor);
        }
        else if (namePrefix == "Pit")
        {
            marker.cellType = FeatureCellType.Pit;
            TryApplyFeatureBorder(go, sr, pitBorderColor);
        }
        else
        {
            marker.cellType = FeatureCellType.None;
        }

        if (ShouldHaveCollider(isBlackBlock))
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

    public float SurfaceY => baseSurfaceUnits * blockSize;

    public float SplitY => baseSurfaceUnits * blockSize + blockSize * 0.5f;

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
            ReplaceObstacleWithWhiteCell(cell, key, worldPos);
            return true;
        }

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

        float renderSize = blockSize + Mathf.Max(0f, blockOverlap);
        go.transform.localScale = new Vector3(renderSize, renderSize, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = runtimeSprite;
        sr.sortingOrder = 0;

        if (usePitFillColorTransition)
        {
            sr.color = Color.white;
        }
        else
        {
            sr.color = Color.black;
        }

        TryApplyFeatureBorder(go, sr, filledPitBorderColor);

        FeatureCell marker = go.AddComponent<FeatureCell>();
        marker.cellType = FeatureCellType.None;
        marker.isBlackBlock = true;

        if (ShouldHaveCollider(marker.isBlackBlock))
        {
            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.compositeOperation = Collider2D.CompositeOperation.Merge;
            col.isTrigger = false;
            colliderDirty = true;
        }

        cellObjects[key] = go;

        if (usePitFillColorTransition)
        {
            StartCoroutine(AnimatePitFillToBlack(go, sr));
        }

        TryLiftPlayerFromFilledPit(go.transform.position);
    }

    private void TryLiftPlayerFromFilledPit(Vector2 filledCellCenter)
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

        float half = blockSize * 0.5f;
        float cellMinX = filledCellCenter.x - half;
        float cellMaxX = filledCellCenter.x + half;
        Bounds playerBounds = playerCol.bounds;

        if (playerBounds.max.x <= cellMinX || playerBounds.min.x >= cellMaxX)
        {
            return;
        }

        bool inverted = playerRb != null && playerRb.gravityScale < 0f;
        float cellFaceY = inverted ? filledCellCenter.y - half : filledCellCenter.y + half;
        float playerEdgeY = inverted ? playerBounds.max.y : playerBounds.min.y;

        if (inverted ? playerEdgeY <= cellFaceY : playerEdgeY >= cellFaceY)
        {
            return;
        }

        float edgeOffset = player.position.y - playerEdgeY;
        Vector3 p = player.position;
        p.y = cellFaceY + edgeOffset + (inverted ? -0.02f : 0.02f);
        player.position = p;

        if (playerRb != null)
        {
            Vector2 v = playerRb.linearVelocity;
            if (inverted)
            {
                if (v.y > 0f) v.y = 0f;
            }
            else
            {
                if (v.y < 0f) v.y = 0f;
            }

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

    public void SetAreaSwapped(bool swapped)
    {
        if (areaSwapped == swapped)
        {
            return;
        }

        areaSwapped = swapped;
        RefreshAllCellColliders();
    }

    private void RefreshAllCellColliders()
    {
        bool changed = false;

        foreach (var kv in cellObjects)
        {
            GameObject go = kv.Value;
            if (go == null)
            {
                continue;
            }

            FeatureCell marker = go.GetComponent<FeatureCell>();
            if (marker == null)
            {
                continue;
            }

            bool shouldHave = ShouldHaveCollider(marker.isBlackBlock);
            Collider2D col = go.GetComponent<Collider2D>();

            if (shouldHave && col == null)
            {
                BoxCollider2D newCol = go.AddComponent<BoxCollider2D>();
                newCol.compositeOperation = Collider2D.CompositeOperation.Merge;
                newCol.isTrigger = false;
                changed = true;
            }
            else if (!shouldHave && col != null)
            {
                Destroy(col);
                changed = true;
            }
        }

        if (changed)
        {
            colliderDirty = true;
            FlushCompositeGeometryIfDirty(true);
        }
    }

    private bool ShouldHaveCollider(bool isBlackBlock)
    {
        return isBlackBlock ? !areaSwapped : areaSwapped;
    }

    private void TryApplyFeatureBorder(GameObject host, SpriteRenderer srcRenderer, Color borderColor)
    {
        if (!useFeatureCellBorder || host == null || srcRenderer == null || srcRenderer.sprite == null)
        {
            return;
        }

        Transform border = host.transform.Find("Border");
        GameObject borderGo;
        if (border == null)
        {
            borderGo = new GameObject("Border");
            borderGo.transform.SetParent(host.transform, false);
        }
        else
        {
            borderGo = border.gameObject;
        }

        borderGo.transform.localPosition = Vector3.zero;
        borderGo.transform.localRotation = Quaternion.identity;
        borderGo.transform.localScale = new Vector3(featureBorderScale, featureBorderScale, 1f);

        SpriteRenderer borderSr = borderGo.GetComponent<SpriteRenderer>();
        if (borderSr == null)
        {
            borderSr = borderGo.AddComponent<SpriteRenderer>();
        }

        borderSr.sprite = srcRenderer.sprite;
        borderSr.color = borderColor;
        borderSr.sortingOrder = srcRenderer.sortingOrder + featureBorderSortingOffset;
    }

    private void ReplaceObstacleWithWhiteCell(GameObject obstacleCell, Vector2Int key, Vector3 worldPos)
    {
        if (obstacleCell == null)
        {
            return;
        }

        var marker = obstacleCell.GetComponent<FeatureCell>();
        if (marker != null)
        {
            marker.cellType = FeatureCellType.None;
            marker.isBlackBlock = false;
        }

        var col = obstacleCell.GetComponent<Collider2D>();
        if (col != null)
        {
            Destroy(col);
            colliderDirty = true;
        }

        obstacleCell.name = "White";

        var sr = obstacleCell.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = obstacleCell.AddComponent<SpriteRenderer>();
        }

        sr.sprite = runtimeSprite;

        if (useObstacleFillColorTransition)
        {
            StartCoroutine(AnimateObstacleFillToWhite(obstacleCell, sr));
        }
        else
        {
            sr.color = Color.white;

            Transform border = obstacleCell.transform.Find("Border");
            if (border != null)
            {
                Destroy(border.gameObject);
            }
        }

        if (ShouldHaveCollider(false))
        {
            BoxCollider2D newCol = obstacleCell.AddComponent<BoxCollider2D>();
            newCol.compositeOperation = Collider2D.CompositeOperation.Merge;
            newCol.isTrigger = false;
            colliderDirty = true;
        }
    }

    private System.Collections.IEnumerator AnimateObstacleFillToWhite(GameObject host, SpriteRenderer sr)
    {
        if (host == null || sr == null)
        {
            yield break;
        }

        float duration = Mathf.Max(0.01f, obstacleFillColorTransitionDuration);
        float t = 0f;

        Color fromColor = sr.color;
        Color toColor = Color.white;

        SpriteRenderer borderSr = null;
        Transform border = host.transform.Find("Border");
        if (border != null)
        {
            borderSr = border.GetComponent<SpriteRenderer>();
        }

        Color borderFrom = borderSr != null ? borderSr.color : Color.clear;
        Color borderTo = new Color(borderFrom.r, borderFrom.g, borderFrom.b, 0f);

        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            float eased = p * p * (3f - 2f * p);

            if (sr != null)
            {
                sr.color = Color.Lerp(fromColor, toColor, eased);
            }

            if (fadeOutObstacleBorderOnFill && borderSr != null)
            {
                borderSr.color = Color.Lerp(borderFrom, borderTo, eased);
            }

            yield return null;
        }

        if (sr != null)
        {
            sr.color = toColor;
        }

        if (fadeOutObstacleBorderOnFill)
        {
            Transform currentBorder = host.transform.Find("Border");
            if (currentBorder != null)
            {
                Destroy(currentBorder.gameObject);
            }
        }
    }

    private System.Collections.IEnumerator AnimatePitFillToBlack(GameObject host, SpriteRenderer sr)
    {
        if (host == null || sr == null)
        {
            yield break;
        }

        float duration = Mathf.Max(0.01f, pitFillColorTransitionDuration);
        float t = 0f;

        Color fromColor = sr.color;
        Color toColor = Color.black;

        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            float eased = p * p * (3f - 2f * p);

            if (sr != null)
            {
                sr.color = Color.Lerp(fromColor, toColor, eased);
            }

            yield return null;
        }

        if (sr != null)
        {
            sr.color = toColor;
        }
    }
}