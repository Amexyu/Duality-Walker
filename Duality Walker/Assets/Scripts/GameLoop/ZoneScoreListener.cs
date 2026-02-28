using DualityWalker.Placement;
using DualityWalker.World;
using UnityEngine;

namespace DualityWalker.GameLoop
{
    public class ZoneScoreListener : MonoBehaviour
    {
        private ZoneResolver resolver;
        private ScoreSystem score;

        public void Initialize(ZoneResolver zoneResolver, ScoreSystem scoreSystem)
        {
            resolver = zoneResolver;
            score = scoreSystem;
            resolver.OnZoneResolved += OnZoneResolved;
        }

        private void OnDestroy()
        {
            if (resolver != null)
            {
                resolver.OnZoneResolved -= OnZoneResolved;
            }
        }

        private void OnZoneResolved(ZoneType zone)
        {
            if (score == null)
            {
                return;
            }

            if (zone == ZoneType.Pit)
            {
                score.AddPitScore();
            }
            else if (zone == ZoneType.Obstacle)
            {
                score.AddObstacleScore();
            }
        }
    }
}
