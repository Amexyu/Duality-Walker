using System.Collections.Generic;
using UnityEngine;

public class PlayerGroup2D : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private float gridSize = 1f;

    [Header("Auto Grid From Sprite")]
    [SerializeField] private bool autoGridFromSprite = true;
    [SerializeField] private Sprite blockSprite;

    [Header("Auto Attach Control")]
    [SerializeField] private float detectRadiusCells = 3.0f; // 检测范围（建议先 3~6 验证吸附OK）
    [SerializeField] private float maxSnapCells = 1.2f;      // 吸附力度（建议先 1.0~1.6 验证吸附OK）
    [SerializeField] private float attachDelay = 0.2f;       // 生成后延迟（建议先 0~0.3）

    [Header("Snap Animation")]
    [SerializeField] private float snapAnimTime = 0.12f;

    [Header("Bias")]
    [SerializeField] private float moveBias = 1.0f;
    [SerializeField] private float holeBonus = 1.0f;
    [SerializeField] private float squareBias = 0.35f;

    [Header("Rules")]
    [SerializeField] private bool requireNeighbor = true;

    [Header("Square")]
    [SerializeField] private bool enableSquareCheck = true;

    [Header("Square Consume (n×n -> (n-2)×(n-2))")]
    [SerializeField] private bool consumeAfterSquare = true;
    [SerializeField] private int consumeRings = 1;

    [Header("Scale Sync (Free pieces follow group size)")]
    [SerializeField] private bool syncFreePieceScale = true;
    [SerializeField] private float scaleSyncInterval = 0.10f;

    [Header("Tetris-like Insert Settings")]
    [Tooltip("进入目标格附近后需要停留的时间（秒），超过该时间才允许吸附")]
    [SerializeField] private float holdTimeToAttach = 0.18f;
    [Tooltip("自由块的刚体速度必须低于该值才允许吸附（避免高速碰到就吸）")]
    [SerializeField] private float snapSpeedMax = 1.2f;
    [Tooltip("更小的吸附半径（格）：降低到更贴近才会吸附，建议 0.6~1.0")]
    [SerializeField] private float snapRadiusCellsTight = 0.8f;
    [Tooltip("只在严格“内部洞”位置给予额外优先级")]
    [SerializeField] private bool preferStrictHoles = true;

    [Header("Debug")]
    [SerializeField] private bool debugAttach = false;

    [Header("Audio")]
    [Tooltip("方块吸附成功时播放的音效")]
    [SerializeField] private AudioClip attachClip;
    [SerializeField] [Range(0f, 1f)] private float attachVolume = 0.8f;
    [SerializeField] [Range(0.5f, 2f)] private float attachPitch = 1.0f;
    [Tooltip("若存在，将使用该 AudioSource 播放（可复用、受混音控制）；为空则临时创建播放一次。")]
    [SerializeField] private AudioSource audioSource;

    [Header("Audio - Square")]
    [Tooltip("拼出完美正方形时播放的音效")]
    [SerializeField] private AudioClip squareClip;
    [SerializeField, Range(0f, 1f)] private float squareVolume = 1.0f;
    [SerializeField, Range(0.5f, 2f)] private float squarePitch = 1.0f;

    [Header("Visual")]
    [Tooltip("块体统一颜色（自由块与玩家组内块均生效）")]
    [SerializeField] private Color blockColor = new Color(0.85f, 0.85f, 0.85f, 1f);

    private float scaleSyncTimer = 0f;

    private readonly HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();
    private int lastAttachFrame = -1;
    private int lastSquareFrame = -1;

    // 记录每个自由块在“最近一次满足邻近条件”的起始时间
    private readonly Dictionary<FreePiece2D, float> nearTargetSince = new Dictionary<FreePiece2D, float>();

    // 吸附后延迟补检的协程（避免漏判）
    private Coroutine squareCheckCo;

    private void Start()
    {
        EnsureGridSize();
        SyncOccupiedFromChildren();
        ApplyBlockColorTo(transform);
    }

    private void Update()
    {
        EnsureGridSize();
        if (gridSize <= 0.0001f) return;

        if (syncFreePieceScale)
        {
            scaleSyncTimer -= Time.deltaTime;
            if (scaleSyncTimer <= 0f)
            {
                scaleSyncTimer = Mathf.Max(0.02f, scaleSyncInterval);
                SyncAllFreePieceScaleToGroup();
            }
        }

        if (lastAttachFrame == Time.frameCount) return;

        SyncOccupiedFromChildren();

        if (TryAttachBestFreePiece())
            lastAttachFrame = Time.frameCount;
    }

    private void EnsureGridSize()
    {
        if (!autoGridFromSprite) return;

        if (blockSprite != null)
        {
            gridSize = blockSprite.bounds.size.x;
            FreePiece2D.GlobalGridSize = gridSize;
            return;
        }

        if (FreePiece2D.GlobalGridSize > 0.0001f)
            gridSize = FreePiece2D.GlobalGridSize;
    }

    private void SyncAllFreePieceScaleToGroup()
    {
        float s = transform.lossyScale.x;
        for (int i = 0; i < FreePiece2D.Active.Count; i++)
        {
            var p = FreePiece2D.Active[i];
            if (p == null || p.Attached) continue;
            p.transform.localScale = new Vector3(s, s, 1f);
        }
    }

    private float GetWorldPerCell()
    {
        Vector3 a = transform.TransformPoint(Vector3.zero);
        Vector3 b = transform.TransformPoint(new Vector3(gridSize, 0f, 0f));
        return Mathf.Max(0.0001f, Vector3.Distance(a, b));
    }

    private bool TryAttachBestFreePiece()
    {
        int dummy;
        bool wasSquare = enableSquareCheck && IsPerfectSquare(occupied, out dummy);

        // 1) 生成所有目标格（四邻空位）
        HashSet<Vector2Int> targets = new HashSet<Vector2Int>();
        foreach (var o in occupied)
        {
            AddIfEmpty(o + Vector2Int.up, targets);
            AddIfEmpty(o + Vector2Int.down, targets);
            AddIfEmpty(o + Vector2Int.left, targets);
            AddIfEmpty(o + Vector2Int.right, targets);
        }
        if (targets.Count == 0) return false;

        float worldPerCell = GetWorldPerCell();

        // 2) 全局搜索最优（piece + offset）
        FreePiece2D bestPiece = null;
        Vector2Int bestOffset = Vector2Int.zero;
        float bestScore = float.PositiveInfinity;

        int skipDelay = 0, skipDetect = 0, skipSnap = 0, skipPlace = 0, skipSpeed = 0, skipHold = 0;

        for (int pi = 0; pi < FreePiece2D.Active.Count; pi++)
        {
            var piece = FreePiece2D.Active[pi];
            if (piece == null || piece.Attached) continue;

            if (Time.time - piece.BornTime < attachDelay) { skipDelay++; continue; }

            float speed = 0f;
            var rb = piece.GetComponent<Rigidbody2D>();
            if (rb != null) speed = rb.linearVelocity.magnitude;
            if (rb != null && speed > snapSpeedMax) { skipSpeed++; continue; }

            if (!TryGetPieceWorldAABB(piece, out Vector2 pMin, out Vector2 pMax))
            {
                pMin = pMax = piece.transform.position;
            }

            GetWorldAABB(out Vector2 gMin, out Vector2 gMax);
            float detectWorld = DistanceAABB_AABB(pMin, pMax, gMin, gMax);
            float detectCells = detectWorld / worldPerCell;
            if (detectCells > detectRadiusCells) { skipDetect++; continue; }

            bool anyNearCandidate = false;
            float localBestScore = float.PositiveInfinity;
            Vector2Int localBestOffset = Vector2Int.zero;

            foreach (var t in targets)
            {
                bool isHoleStrict = IsStrictHole(t);

                for (int ci = 0; ci < piece.Cells.Count; ci++)
                {
                    Vector2Int c = piece.Cells[ci];
                    Vector2Int offset = t - c;

                    Vector2 cellWorld = piece.transform.TransformPoint(new Vector3(c.x * gridSize, c.y * gridSize, 0f));
                    Vector2 targetWorld = transform.TransformPoint(new Vector3(t.x * gridSize, t.y * gridSize, 0f));
                    float moveDistCells = Vector2.Distance(cellWorld, targetWorld) / worldPerCell;

                    if (moveDistCells > snapRadiusCellsTight) { skipSnap++; continue; }
                    if (!CanPlace(piece, offset)) { skipPlace++; continue; }

                    anyNearCandidate = true;

                    float shapePenalty = PredictSquarePenalty(piece, offset);
                    float score = moveDistCells * moveBias + shapePenalty - ((preferStrictHoles && isHoleStrict) ? holeBonus : 0f);

                    if (score < localBestScore)
                    {
                        localBestScore = score;
                        localBestOffset = offset;
                    }
                }
            }

            if (anyNearCandidate)
            {
                if (!nearTargetSince.ContainsKey(piece))
                    nearTargetSince[piece] = Time.time;
            }
            else
            {
                if (nearTargetSince.ContainsKey(piece))
                    nearTargetSince.Remove(piece);
                skipHold++;
                continue;
            }

            float held = Time.time - nearTargetSince[piece];
            if (held < holdTimeToAttach)
            {
                skipHold++;
                continue;
            }

            if (localBestScore < bestScore)
            {
                bestScore = localBestScore;
                bestPiece = piece;
                bestOffset = localBestOffset;
            }
        }

        if (bestPiece == null)
        {
            if (debugAttach)
            {
                Debug.Log($"[AttachDebug] NO ATTACH | Active={FreePiece2D.Active.Count} " +
                          $"skipDelay={skipDelay} skipDetect={skipDetect} skipSnap={skipSnap} skipPlace={skipPlace} skipSpeed={skipSpeed} skipHold={skipHold} " +
                          $"detectR={detectRadiusCells} snapTight={snapRadiusCellsTight} hold={holdTimeToAttach}s speedMax={snapSpeedMax}");
            }
            return false;
        }

        // 3) 执行吸附（先启动动画）
        float s = transform.lossyScale.x;
        bestPiece.transform.localScale = new Vector3(s, s, 1f);
        bestPiece.AttachTo(transform, bestOffset, snapAnimTime);

        // 立即用“预测占用”判定是否已成正方形（避免等动画结束被中断）
        bool preAwarded = false;
        if (enableSquareCheck)
        {
            var predicted = new HashSet<Vector2Int>(occupied);
            for (int i = 0; i < bestPiece.Cells.Count; i++)
                predicted.Add(bestOffset + bestPiece.Cells[i]);

            if (!wasSquare && IsPerfectSquare(predicted, out int nPred))
            {
                if (lastSquareFrame != Time.frameCount)
                {
                    lastSquareFrame = Time.frameCount;

                    PlaySquareSfx();

                    var pc0 = GetComponentInParent<PlayerController>();
                    if (pc0 != null)
                        pc0.OnSquareCompleted(addScore: nPred * nPred, n: nPred);

                    // 缩圈放到动画结束后执行，视觉更顺滑
                    if (consumeAfterSquare)
                        StartCoroutine(ConsumeAfterDelay(nPred, consumeRings, snapAnimTime));

                    preAwarded = true;
                }
            }
        }

        // 着色刚吸附进来的块
        ApplyBlockColorTo(bestPiece.transform);

        // 播放吸附音效
        PlayAttachSfx();

        if (nearTargetSince.ContainsKey(bestPiece))
            nearTargetSince.Remove(bestPiece);

        // 同步一次占用（若动画很短，当帧可能已到位）
        SyncOccupiedFromChildren();

        bool immediateHandled = preAwarded;

        // 4) （补充）当帧基于实际位置再判一遍
        if (!immediateHandled && enableSquareCheck)
        {
            if (IsPerfectSquare(occupied, out int n) && !wasSquare)
            {
                if (lastSquareFrame != Time.frameCount)
                {
                    lastSquareFrame = Time.frameCount;

                    PlaySquareSfx();

                    var pc = GetComponentInParent<PlayerController>();
                    if (pc != null)
                        pc.OnSquareCompleted(addScore: n * n, n: n);

                    if (consumeAfterSquare)
                        StartCoroutine(ConsumeAfterDelay(n, consumeRings, snapAnimTime));

                    immediateHandled = true;
                }
            }
        }

        // 5) 若仍未触发，动画结束后再补检（用不受 TimeScale 影响的实时等待）
        if (!immediateHandled && squareCheckCo == null)
            squareCheckCo = StartCoroutine(SquareCheckAfterDelay(snapAnimTime * 1.05f, wasSquare));

        return true;
    }

    private System.Collections.IEnumerator SquareCheckAfterDelay(float delay, bool wasSquareBefore)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);

        SyncOccupiedFromChildren();

        if (enableSquareCheck && IsPerfectSquare(occupied, out int n) && !wasSquareBefore)
        {
            if (lastSquareFrame != Time.frameCount)
            {
                lastSquareFrame = Time.frameCount;

                PlaySquareSfx();

                var pc = GetComponentInParent<PlayerController>();
                if (pc != null)
                    pc.OnSquareCompleted(addScore: n * n, n: n);

                if (consumeAfterSquare)
                    ConsumeSquareToInnerSquare(n, consumeRings);
            }
        }

        squareCheckCo = null;
    }

    private System.Collections.IEnumerator ConsumeAfterDelay(int n, int rings, float delay)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        if (enableSquareCheck && consumeAfterSquare)
            ConsumeSquareToInnerSquare(n, rings);
    }

    private void PlayAttachSfx()
    {
        if (attachClip == null) return;

        if (audioSource != null)
        {
            float originalPitch = audioSource.pitch;
            audioSource.pitch = attachPitch;
            audioSource.PlayOneShot(attachClip, attachVolume);
            audioSource.pitch = originalPitch;
            return;
        }

        GameObject temp = new GameObject("AttachSFX");
        temp.transform.position = transform.position;
        var src = temp.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        src.pitch = attachPitch;
        src.volume = attachVolume;
        src.clip = attachClip;
        src.loop = false;
        src.rolloffMode = AudioRolloffMode.Linear;

        src.Play();
        Object.Destroy(temp, attachClip.length / Mathf.Max(0.01f, attachPitch));
    }

    private void PlaySquareSfx()
    {
        if (squareClip == null) return;

        if (audioSource != null)
        {
            float originalPitch = audioSource.pitch;
            audioSource.pitch = squarePitch;
            audioSource.PlayOneShot(squareClip, squareVolume);
            audioSource.pitch = originalPitch;
            return;
        }

        GameObject temp = new GameObject("SquareSFX");
        temp.transform.position = transform.position;
        var src = temp.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        src.pitch = squarePitch;
        src.volume = squareVolume;
        src.clip = squareClip;
        src.loop = false;
        src.rolloffMode = AudioRolloffMode.Linear;

        src.Play();
        Object.Destroy(temp, squareClip.length / Mathf.Max(0.01f, squarePitch));
    }

    // 统一着色
    public void ApplyBlockColorTo(Transform root)
    {
        if (root == null) return;
        var srs = root.GetComponentsInChildren<SpriteRenderer>(true);
        var c = blockColor;
        for (int i = 0; i < srs.Length; i++)
            srs[i].color = c;
    }

    private bool TryGetPieceWorldAABB(FreePiece2D p, out Vector2 min, out Vector2 max)
    {
        var srs = p.GetComponentsInChildren<SpriteRenderer>();
        if (srs == null || srs.Length == 0)
        {
            min = max = p.transform.position;
            return false;
        }

        Bounds b = srs[0].bounds;
        for (int i = 1; i < srs.Length; i++)
            b.Encapsulate(srs[i].bounds);

        min = new Vector2(b.min.x, b.min.y);
        max = new Vector2(b.max.x, b.max.y);
        return true;
    }

    private static float DistanceAABB_AABB(Vector2 aMin, Vector2 aMax, Vector2 bMin, Vector2 bMax)
    {
        float dx = 0f;
        if (aMax.x < bMin.x) dx = bMin.x - aMax.x;
        else if (aMin.x > bMax.x) dx = aMin.x - bMax.x;

        float dy = 0f;
        if (aMax.y < bMin.y) dy = bMin.y - aMax.y;
        else if (aMin.y > bMax.y) dy = aMin.y - bMax.y;

        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    private bool IsStrictHole(Vector2Int t)
    {
        GetBounds(occupied, out int minX, out int maxX, out int minY, out int maxY);
        return (t.x > minX && t.x < maxX && t.y > minY && t.y < maxY);
    }

    private void AddIfEmpty(Vector2Int cell, HashSet<Vector2Int> set)
    {
        if (!occupied.Contains(cell)) set.Add(cell);
    }

    private bool CanPlace(FreePiece2D piece, Vector2Int offset)
    {
        bool hasNeighbor = false;

        for (int i = 0; i < piece.Cells.Count; i++)
        {
            Vector2Int p = offset + piece.Cells[i];
            if (occupied.Contains(p)) return false;

            if (requireNeighbor && !hasNeighbor)
            {
                if (occupied.Contains(p + Vector2Int.up) ||
                    occupied.Contains(p + Vector2Int.down) ||
                    occupied.Contains(p + Vector2Int.left) ||
                    occupied.Contains(p + Vector2Int.right))
                {
                    hasNeighbor = true;
                }
            }
        }

        return requireNeighbor ? hasNeighbor : true;
    }

    private void SyncOccupiedFromChildren()
    {
        occupied.Clear();
        occupied.Add(Vector2Int.zero);

        if (gridSize <= 0.0001f) return;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform pieceRoot = transform.GetChild(i);
            if (pieceRoot.GetComponent<FreePiece2D>() == null) continue;

            for (int c = 0; c < pieceRoot.childCount; c++)
            {
                Transform cell = pieceRoot.GetChild(c);

                Vector3 local = transform.InverseTransformPoint(cell.position);
                int gx = Mathf.RoundToInt(local.x / gridSize);
                int gy = Mathf.RoundToInt(local.y / gridSize);

                occupied.Add(new Vector2Int(gx, gy));
            }
        }
    }

    public void GetWorldAABB(out Vector2 worldMin, out Vector2 worldMax)
    {
        SyncOccupiedFromChildren();
        GetBounds(occupied, out int minX, out int maxX, out int minY, out int maxY);

        float half = gridSize * 0.5f;

        Vector3 a = transform.TransformPoint(new Vector3(minX * gridSize - half, minY * gridSize - half, 0));
        Vector3 b = transform.TransformPoint(new Vector3(maxX * gridSize + half, minY * gridSize - half, 0));
        Vector3 c = transform.TransformPoint(new Vector3(minX * gridSize - half, maxY * gridSize + half, 0));
        Vector3 d = transform.TransformPoint(new Vector3(maxX * gridSize + half, maxY * gridSize + half, 0));

        float minWX = Mathf.Min(Mathf.Min(a.x, b.x), Mathf.Min(c.x, d.x));
        float maxWX = Mathf.Max(Mathf.Max(a.x, b.x), Mathf.Max(c.x, d.x));
        float minWY = Mathf.Min(Mathf.Min(a.y, b.y), Mathf.Min(c.y, d.y));
        float maxWY = Mathf.Max(Mathf.Max(a.y, b.y), Mathf.Max(c.y, d.y));

        worldMin = new Vector2(minWX, minWY);
        worldMax = new Vector2(maxWX, maxWY);
    }

    private float PredictSquarePenalty(FreePiece2D piece, Vector2Int offset)
    {
        if (squareBias <= 0f) return 0f;

        GetBounds(occupied, out int minX, out int maxX, out int minY, out int maxY);

        for (int i = 0; i < piece.Cells.Count; i++)
        {
            Vector2Int p = offset + piece.Cells[i];
            if (p.x < minX) minX = p.x;
            if (p.x > maxX) maxX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.y > maxY) maxY = p.y;
        }

        int w = maxX - minX + 1;
        int h = maxY - minY + 1;
        return Mathf.Abs(w - h) * squareBias;
    }

    private static void GetBounds(HashSet<Vector2Int> set, out int minX, out int maxX, out int minY, out int maxY)
    {
        minX = int.MaxValue; maxX = int.MinValue;
        minY = int.MaxValue; maxY = int.MinValue;

        foreach (var p in set)
        {
            if (p.x < minX) minX = p.x;
            if (p.x > maxX) maxX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.y > maxY) maxY = p.y;
        }

        if (minX == int.MaxValue)
        {
            minX = maxX = 0;
            minY = maxY = 0;
        }
    }

    private static bool IsPerfectSquare(HashSet<Vector2Int> set, out int n)
    {
        n = 0;
        if (set == null || set.Count <= 1) return false;

        GetBounds(set, out int minX, out int maxX, out int minY, out int maxY);

        int w = maxX - minX + 1;
        int h = maxY - minY + 1;
        if (w != h) return false;

        n = w;
        if (set.Count != n * n) return false;

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                if (!set.Contains(new Vector2Int(x, y))) return false;
            }
        }

        return true;
    }

    private void ConsumeSquareToInnerSquare(int n, int rings)
    {
        EnsureGridSize();

        int target = Mathf.Max(1, n - 2 * Mathf.Max(1, rings));
        if (target == n) return;

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform ch = transform.GetChild(i);
            if (ch.GetComponent<FreePiece2D>() != null)
            {
                ch.SetParent(null);
                Destroy(ch.gameObject);
            }
        }

        if (blockSprite == null)
        {
            Debug.LogWarning("[PlayerGroup2D] blockSprite is null. Assign it in Inspector.");
            return;
        }

        int min = -target / 2;
        int max = min + target - 1;

        List<Vector2Int> cells = new List<Vector2Int>(target * target - 1);
        for (int x = min; x <= max; x++)
        {
            for (int y = min; y <= max; y++)
            {
                if (x == 0 && y == 0) continue;
                cells.Add(new Vector2Int(x, y));
            }
        }

        var seed = new GameObject($"Seed_Square_{target}");
        seed.transform.SetParent(transform, false);
        seed.transform.localPosition = Vector3.zero;
        seed.transform.localRotation = Quaternion.identity;
        seed.transform.localScale = Vector3.one;

        var fp = seed.AddComponent<FreePiece2D>();
        fp.Init(cells.ToArray(), blockSprite, gridSize);
        fp.AttachTo(transform, Vector2Int.zero, 0f);

        ApplyBlockColorTo(seed.transform);

        SyncOccupiedFromChildren();
    }

    public void CarveHoleAtWorld(Vector2 worldPos, float radiusCells)
    {
        EnsureGridSize();
        if (gridSize <= 0.0001f) return;

        float worldPerCell = GetWorldPerCell();
        float radiusWorld = Mathf.Max(0f, radiusCells) * worldPerCell;

        Vector2 hit = worldPos;

        List<Transform> toDeleteCells = new List<Transform>();
        List<Transform> emptyPieceRoots = new List<Transform>();

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform pieceRoot = transform.GetChild(i);
            var p = pieceRoot.GetComponent<FreePiece2D>();
            if (p == null) continue;

            for (int c = 0; c < pieceRoot.childCount; c++)
            {
                Transform cell = pieceRoot.GetChild(c);
                float distW = Vector2.Distance(cell.position, hit);
                if (distW <= radiusWorld + 1e-5f) toDeleteCells.Add(cell);
            }
        }

        for (int i = 0; i < toDeleteCells.Count; i++)
        {
            var cell = toDeleteCells[i];
            if (cell != null) Destroy(cell.gameObject);
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform pieceRoot = transform.GetChild(i);
            if (pieceRoot.GetComponent<FreePiece2D>() == null) continue;
            if (pieceRoot.childCount == 0) emptyPieceRoots.Add(pieceRoot);
        }
        for (int i = 0; i < emptyPieceRoots.Count; i++)
        {
            var pr = emptyPieceRoots[i];
            pr.SetParent(null);
            Destroy(pr.gameObject);
        }

        SyncOccupiedFromChildren();
    }
}
