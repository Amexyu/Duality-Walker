using DualityWalker.Blocks;
using DualityWalker.World;
using UnityEngine;

namespace DualityWalker.Placement
{
    public class PlacementController : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private BlockAssembly activeAssembly;
        [SerializeField] private ZoneResolver zoneResolver;
        [SerializeField] private float cellSize = 1f;

        private void Start()
        {
            targetCamera = targetCamera != null ? targetCamera : Camera.main;
            activeAssembly = activeAssembly != null ? activeAssembly : FindFirstObjectByType<BlockAssembly>();
            zoneResolver = zoneResolver != null ? zoneResolver : FindFirstObjectByType<ZoneResolver>();
        }

        private void Update()
        {
            if (!Input.GetMouseButtonDown(1) || targetCamera == null || activeAssembly == null)
            {
                return;
            }

            var world = targetCamera.ScreenToWorldPoint(Input.mousePosition);
            var snapped = new Vector3(
                Mathf.Round(world.x / cellSize) * cellSize,
                Mathf.Round(world.y / cellSize) * cellSize,
                0f);

            activeAssembly.transform.position = snapped;
            TryResolveCurrentOverlaps();
        }

        private void TryResolveCurrentOverlaps()
        {
            if (zoneResolver == null)
            {
                return;
            }

            foreach (var cell in activeAssembly.Occupied)
            {
                var block = cell.Value;
                var hits = Physics2D.OverlapPointAll(block.transform.position);
                foreach (var hit in hits)
                {
                    var zone = hit.GetComponent<ZoneCell>();
                    if (zone != null)
                    {
                        zoneResolver.TryResolve(zone, block);
                    }
                }
            }
        }
    }
}
