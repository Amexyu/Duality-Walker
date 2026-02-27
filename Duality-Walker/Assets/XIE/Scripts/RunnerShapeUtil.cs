using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class RunnerShapeUtil
{
    public static string MakeSignature(IReadOnlyList<Vector2Int> cells)
    {
        if (cells == null || cells.Count == 0) return string.Empty;

        int minX = int.MaxValue;
        int minY = int.MaxValue;

        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i].x < minX) minX = cells[i].x;
            if (cells[i].y < minY) minY = cells[i].y;
        }

        var normalized = new List<Vector2Int>(cells.Count);
        for (int i = 0; i < cells.Count; i++)
            normalized.Add(new Vector2Int(cells[i].x - minX, cells[i].y - minY));

        normalized.Sort((a, b) =>
        {
            int cx = a.x.CompareTo(b.x);
            return cx != 0 ? cx : a.y.CompareTo(b.y);
        });

        var sb = new StringBuilder();
        for (int i = 0; i < normalized.Count; i++)
            sb.Append(normalized[i].x).Append('_').Append(normalized[i].y).Append(';');

        return sb.ToString();
    }
}