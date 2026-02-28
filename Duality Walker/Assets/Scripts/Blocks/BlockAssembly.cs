using System.Collections.Generic;
using UnityEngine;

namespace DualityWalker.Blocks
{
    public class BlockAssembly : MonoBehaviour
    {
        private readonly Dictionary<Vector2Int, BlockUnit> occupied = new();

        public IReadOnlyDictionary<Vector2Int, BlockUnit> Occupied => occupied;

        public bool TryAddBlock(BlockUnit block, Vector2Int cell)
        {
            if (occupied.ContainsKey(cell))
            {
                return false;
            }

            occupied[cell] = block;
            block.transform.SetParent(transform);
            block.transform.localPosition = new Vector3(cell.x, cell.y, 0f);
            return true;
        }

        public BoundsInt GetBounds()
        {
            if (occupied.Count == 0)
            {
                return new BoundsInt(0, 0, 0, 1, 1, 1);
            }

            var min = new Vector2Int(int.MaxValue, int.MaxValue);
            var max = new Vector2Int(int.MinValue, int.MinValue);
            foreach (var pair in occupied)
            {
                min = Vector2Int.Min(min, pair.Key);
                max = Vector2Int.Max(max, pair.Key);
            }
            return new BoundsInt(min.x, min.y, 0, max.x - min.x + 1, max.y - min.y + 1, 1);
        }
    }
}
