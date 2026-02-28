using DualityWalker.Core;
using UnityEngine;

namespace DualityWalker.GameLoop
{
    public class DifficultyDirector : MonoBehaviour
    {
        [SerializeField] private RunnerMotor runnerMotor;
        [SerializeField] private float speedIncreasePerMinute = 0.8f;
        [SerializeField] private float maxSpeed = 12f;

        private float elapsedSeconds;
        private float startSpeed;

        private void Start()
        {
            if (runnerMotor == null)
            {
                runnerMotor = FindFirstObjectByType<RunnerMotor>();
            }

            if (runnerMotor != null)
            {
                startSpeed = runnerMotor.RunSpeed;
            }
        }

        private void Update()
        {
            if (runnerMotor == null)
            {
                return;
            }

            elapsedSeconds += Time.deltaTime;
            var nextSpeed = startSpeed + speedIncreasePerMinute * (elapsedSeconds / 60f);
            runnerMotor.SetRunSpeed(Mathf.Min(maxSpeed, nextSpeed));
        }
    }
}
