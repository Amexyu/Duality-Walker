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
            var hit = Physics2D.Raycast(world, Vector2.zero, 0.01f, blockLayer);
            if (hit.collider == null)
            {
                return;
            }

            var selected = hit.collider.GetComponent<BlockUnit>();
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
