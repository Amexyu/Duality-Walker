using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreePiece2D : MonoBehaviour
{
    public static readonly List<FreePiece2D> Active = new List<FreePiece2D>();

    // 让 group/spawner 共享格子尺寸
    public static float GlobalGridSize = 1f;

    public IReadOnlyList<Vector2Int> Cells => cells;
    public bool Attached { get; private set; }

    public Vector2Int Offset { get; private set; }
    public bool HasOffset { get; private set; }

    // ✅生成时间（用于生成后延迟吸附）
    public float BornTime { get; private set; }

    private readonly List<Vector2Int> cells = new List<Vector2Int>();
    private float gridSize = 1f;

    private void OnEnable()
    {
        BornTime = Time.time;
        if (!Active.Contains(this)) Active.Add(this);
    }

    private void OnDisable()
    {
        Active.Remove(this);
    }

    // ✅无缝：子格不缩放，缩放交给父物体（FreePiece 的 transform.localScale）
    public void Init(Vector2Int[] shapeCells, Sprite blockSprite, float gridSize)
    {
        this.gridSize = gridSize;
        GlobalGridSize = gridSize;

        cells.Clear();
        cells.AddRange(shapeCells);

        // 清旧子物体（防止重复 Init）
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        for (int i = 0; i < cells.Count; i++)
        {
            var c = cells[i];

            var child = new GameObject($"Cell_{c.x}_{c.y}");
            child.transform.SetParent(transform, false);
            child.transform.localPosition = new Vector3(c.x * gridSize, c.y * gridSize, 0f);
            child.transform.localScale = Vector3.one;

            var sr = child.AddComponent<SpriteRenderer>();
            sr.sprite = blockSprite;
        }
    }

    public Vector2Int GetOffsetFallback(float gridSize)
    {
        if (HasOffset) return Offset;
        Vector2 localF = (Vector2)transform.localPosition / gridSize;
        return new Vector2Int(Mathf.RoundToInt(localF.x), Mathf.RoundToInt(localF.y));
    }

    public void AttachTo(Transform groupRoot, Vector2Int offsetCell, float animateTime = 0f)
    {
        Attached = true;
        Offset = offsetCell;
        HasOffset = true;

        // ✅确保使用全局一致的 gridSize（避免“看起来贴了但格子不准”）
        if (GlobalGridSize > 0.0001f)
            gridSize = GlobalGridSize;

        // 从 Active 移除
        Active.Remove(this);

        // 目标 local 坐标（严格格子倍数）
        Vector3 targetLocal = new Vector3(offsetCell.x * gridSize, offsetCell.y * gridSize, 0f);

        // ✅关键：先记录当前世界位置
        Vector3 worldPos = transform.position;

        // ✅保持世界位置进行 reparent（便于动画从当前位置开始）
        transform.SetParent(groupRoot, true);
        transform.localRotation = Quaternion.identity;

        // ✅强制 localScale=1（让世界缩放完全由 parent 决定）
        // 但这一步可能改变世界位置，所以我们马上把世界位置恢复回来
        transform.localScale = Vector3.one;
        transform.position = worldPos;

        // 现在的 localPosition 是“恢复世界位置后”的正确起点
        Vector3 startLocal = transform.localPosition;

        if (animateTime <= 0f)
        {
            transform.localPosition = targetLocal;
        }
        else
        {
            StopAllCoroutines();
            StartCoroutine(MoveLocalTo(startLocal, targetLocal, animateTime));
        }
    }

    private IEnumerator MoveLocalTo(Vector3 start, Vector3 target, float time)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            transform.localPosition = Vector3.Lerp(start, target, Mathf.Clamp01(t / time));
            yield return null;
        }
        transform.localPosition = target;
    }
}
