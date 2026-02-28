using UnityEngine;

namespace DualityWalker.GameLoop
{
    public class ScoreSystem : MonoBehaviour
    {
        [SerializeField] private int pitResolveScore = 100;
        [SerializeField] private int obstacleResolveScore = 80;

        private int score;

        public int CurrentScore => score;

        public void AddPitScore() => AddScore(pitResolveScore);

        public void AddObstacleScore() => AddScore(obstacleResolveScore);

        public void AddScore(int amount)
        {
            score += Mathf.Max(0, amount);
            Debug.Log($"Score: {score}");
        }
    }
}
