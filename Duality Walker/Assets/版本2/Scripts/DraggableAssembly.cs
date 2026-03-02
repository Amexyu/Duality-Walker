using System;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class DraggableAssembly : MonoBehaviour
{
    [SerializeField] private float mergeDistanceInCells = 0.6f;
    [SerializeField] private bool snapWhileDragging = false;
    [SerializeField] private float dragSmoothSpeed = 25f;

    private ground worldGround;
    private Camera cam;
    private BoxCollider2D hitBox;
    private bool dragging;
    private Vector3 dragOffset;
    private float cellSize = 1f;
    private bool inSupplySlot;
    private float gridOriginX;
    private Vector3 dragTarget;
    private float dragDepthFromCamera;

    public bool IsDragging => dragging;
    public event Action<DraggableAssembly> DragStarted;

    private void Awake()
    {
        hitBox = GetComponent<BoxCollider2D>();
        hitBox.isTrigger = true;
    }

    private void Start()
    {
        worldGround = FindFirstObjectByType<ground>();
        cam = Camera.main;
        if (worldGround != null)
        {
            cellSize = worldGround.CellSize;
            gridOriginX = worldGround.transform.position.x;
        }

        RebuildHitBox();
    }

    public void SetInSupplySlot(bool value)
    {
        inSupplySlot = value;
    }

    private Vector3 GetMouseWorld()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return transform.position;

        var mp = Input.mousePosition;
        mp.z = dragDepthFromCamera;
        var w = cam.ScreenToWorldPoint(mp);
        w.z = 0f;
        return w;
    }

    private void OnMouseDown()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        if (inSupplySlot)
        {
            inSupplySlot = false;
            DragStarted?.Invoke(this);
        }

        dragging = true;
        dragDepthFromCamera = Mathf.Abs(cam.transform.position.z - transform.position.z);

        var mouse = GetMouseWorld();
        dragOffset = transform.position - mouse;
        dragTarget = transform.position;
    }

    // 用 LateUpdate，避免和相机 LateUpdate 打架
    private void LateUpdate()
    {
        if (!dragging || cam == null)
        {
            return;
        }

        var mouse = GetMouseWorld();
        dragTarget = mouse + dragOffset;
        dragTarget.z = 0f;

        if (snapWhileDragging)
        {
            dragTarget = SnapToGroundGrid(dragTarget);
        }

        transform.position = Vector3.Lerp(
            transform.position,
            dragTarget,
            1f - Mathf.Exp(-dragSmoothSpeed * Time.deltaTime)
        );
    }

    // 替换 OnMouseDrag（保留空实现，避免双重位移）
    private void OnMouseDrag()
    {
    }

    private void OnMouseUp()
    {
        dragging = false;

        // 用最终目标点做吸附，不用当前残余插值位置
        transform.position = SnapToGroundGrid(dragTarget);

        TryMergeNearby();
        TryClearFeatures();
        RebuildHitBox();
    }

    private Vector3 SnapToGroundGrid(Vector3 p)
    {
        p.x = gridOriginX + Mathf.Round((p.x - gridOriginX) / cellSize) * cellSize;
        p.y = Mathf.Round(p.y / cellSize) * cellSize;
        p.z = 0f;
        return p;
    }

    private void TryMergeNearby()
    {
        float mergeDistance = mergeDistanceInCells * cellSize;
        var all = FindObjectsByType<DraggableAssembly>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            var other = all[i];
            if (other == null || other == this) continue;
            if (other.inSupplySlot) continue;
            if (Vector2.Distance(transform.position, other.transform.position) > mergeDistance) continue;

            var blocks = other.GetComponentsInChildren<ShapeBlock>();
            for (int j = 0; j < blocks.Length; j++)
            {
                blocks[j].transform.SetParent(transform, true);
            }

            Destroy(other.gameObject);
        }
    }

    private void TryClearFeatures()
    {
        if (worldGround == null) return;

        bool anyCleared = false;
        var blocks = GetComponentsInChildren<ShapeBlock>();

        for (int i = 0; i < blocks.Length; i++)
        {
            var b = blocks[i];
            if (b == null) continue;

            bool cleared = worldGround.TryClearFeatureCell(b.transform.position, b.IsBlack);
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

    private void RebuildHitBox()
    {
        var blocks = GetComponentsInChildren<ShapeBlock>();
        if (blocks.Length == 0)
        {
            hitBox.size = Vector2.zero;
            return;
        }

        var min = new Vector2(float.MaxValue, float.MaxValue);
        var max = new Vector2(float.MinValue, float.MinValue);

        for (int i = 0; i < blocks.Length; i++)
        {
            var lp = transform.InverseTransformPoint(blocks[i].transform.position);
            min = Vector2.Min(min, lp);
            max = Vector2.Max(max, lp);
        }

        hitBox.offset = (min + max) * 0.5f;
        hitBox.size = (max - min) + Vector2.one * 0.9f;
    }
}