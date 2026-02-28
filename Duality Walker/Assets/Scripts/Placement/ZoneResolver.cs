using System;
using DualityWalker.Blocks;
using DualityWalker.World;
using UnityEngine;

namespace DualityWalker.Placement
{
    public class ZoneResolver : MonoBehaviour
    {
        public event Action<ZoneType> OnZoneResolved;

        public bool TryResolve(ZoneCell zoneCell, BlockUnit block)
        {
            if (zoneCell == null || block == null || zoneCell.Resolved)
            {
                return false;
            }

            var matched =
                (zoneCell.ZoneType == ZoneType.Pit && block.Kind == BlockKind.Black) ||
                (zoneCell.ZoneType == ZoneType.Obstacle && block.Kind == BlockKind.White);

            if (!matched)
            {
                return false;
            }

            zoneCell.Resolve();
            OnZoneResolved?.Invoke(zoneCell.ZoneType);
            return true;
        }
    }
}
