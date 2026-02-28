using UnityEngine;

namespace DualityWalker.GameLoop
{
    [RequireComponent(typeof(Collider2D))]
    public class HazardDetector : MonoBehaviour
    {
        [SerializeField] private RunStateMachine runStateMachine;
        [SerializeField] private float failY = -6f;

        private void Start()
        {
            if (runStateMachine == null)
            {
                runStateMachine = FindFirstObjectByType<RunStateMachine>();
            }
        }

        private void Update()
        {
            // 仅在掉入坑底时失败，避免触碰障碍触发“看起来卡死”。
            if (transform.position.y < failY)
            {
                runStateMachine?.SetState(RunState.Failed);
            }
        }
    }
}
