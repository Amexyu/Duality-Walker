using UnityEngine;

namespace DualityWalker.Blocks
{
    public class BlockComposer : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private LayerMask blockLayer;
        [SerializeField] private BlockAssembly assembly;

        private BlockUnit heldBlock;

        private void Start()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (assembly == null)
            {
                assembly = FindFirstObjectByType<BlockAssembly>();
            }
        }

        private void Update()
        {
            if (!Input.GetMouseButtonDown(0) || targetCamera == null)
            {
                return;
            }

            var world = targetCamera.ScreenToWorldPoint(Input.mousePosition);
            var selected = PickBlockAt(world);
            if (selected == null)
            {
                return;
            }

            if (heldBlock == null)
            {
                heldBlock = selected;
                return;
            }

            if (assembly == null)
            {
                return;
            }

            var direction = selected.transform.position - heldBlock.transform.position;
            var cellOffset = ToCellOffset(direction);
            var targetCell = GetBlockCell(selected) + cellOffset;

            if (!assembly.TryAddBlock(heldBlock, targetCell))
            {
                Debug.Log("拼接失败：目标格子已被占用");
            }

            heldBlock = null;
        }

        private BlockUnit PickBlockAt(Vector3 worldPos)
        {
            // blockLayer 未设置时，默认拾取全部层，避免空场景无法点击。
            if (blockLayer.value == 0)
            {
                var overlap = Physics2D.OverlapPoint(worldPos);
                return overlap != null ? overlap.GetComponent<BlockUnit>() : null;
            }

            var hit = Physics2D.Raycast(worldPos, Vector2.zero, 0.01f, blockLayer);
            return hit.collider != null ? hit.collider.GetComponent<BlockUnit>() : null;
        }

        private Vector2Int ToCellOffset(Vector3 delta)
        {
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                return delta.x >= 0 ? Vector2Int.right : Vector2Int.left;
            }

            return delta.y >= 0 ? Vector2Int.up : Vector2Int.down;
        }

        private Vector2Int GetBlockCell(BlockUnit unit)
        {
            var local = assembly.transform.InverseTransformPoint(unit.transform.position);
            return new Vector2Int(Mathf.RoundToInt(local.x), Mathf.RoundToInt(local.y));
        }
    }
}
