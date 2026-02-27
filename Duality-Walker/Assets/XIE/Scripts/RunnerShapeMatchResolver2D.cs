using System.Collections.Generic;
using UnityEngine;

public class RunnerShapeMatchResolver2D : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform playerGroupRoot;
    [SerializeField] private Camera cam;

    [Header("Grid")]
    [SerializeField] private float gridSize = 1f;

    [Header("Input")]
    [SerializeField] private int mouseButton = 0;

    private readonly List<Vector2Int> tempCells = new List<Vector2Int>();

    private void Update()
    {
        if (!Input.GetMouseButtonUp(mouseButton)) return;
        TryResolveMatch();
    }

    private void TryResolveMatch()
    {
        if (playerGroupRoot == null) return;
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        CollectPlayerCells();
        if (tempCells.Count == 0) return;

        string playerSig = RunnerShapeUtil.MakeSignature(tempCells);

        RunnerObstacle2D[] all = FindObjectsOfType<RunnerObstacle2D>();
        RunnerObstacle2D best = null;
        float bestDx = float.MaxValue;

        for (int i = 0; i < all.Length; i++)
        {
            var o = all[i];
            if (o == null) continue;

            float dx = o.transform.position.x - playerGroupRoot.position.x;
            if (dx < -0.5f) continue;
            if (o.GetSignature() != playerSig) continue;

            if (dx < bestDx)
            {
                bestDx = dx;
                best = o;
            }
        }

        if (best != null)
        {
            Destroy(best.gameObject);
            Debug.Log("[Match] 形状匹配成功，已消除障碍/坑。");
        }
    }

    private void CollectPlayerCells()
    {
        tempCells.Clear();
        if (gridSize <= 0.0001f) gridSize = 1f;

        for (int i = 0; i < playerGroupRoot.childCount; i++)
        {
            Transform pieceRoot = playerGroupRoot.GetChild(i);

            for (int c = 0; c < pieceRoot.childCount; c++)
            {
                Transform cell = pieceRoot.GetChild(c);
                Vector3 local = playerGroupRoot.InverseTransformPoint(cell.position);

                int gx = Mathf.RoundToInt(local.x / gridSize);
                int gy = Mathf.RoundToInt(local.y / gridSize);

                var v = new Vector2Int(gx, gy);
                if (!tempCells.Contains(v)) tempCells.Add(v);
            }
        }

        if (!tempCells.Contains(Vector2Int.zero))
            tempCells.Add(Vector2Int.zero);
    }
}