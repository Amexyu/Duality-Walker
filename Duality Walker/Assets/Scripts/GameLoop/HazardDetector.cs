using DualityWalker.World;
using UnityEngine;

namespace DualityWalker.GameLoop
{
    [RequireComponent(typeof(Collider2D))]
    public class HazardDetector : MonoBehaviour
    {
        [SerializeField] private RunStateMachine runStateMachine;

        private void Start()
        {
            if (runStateMachine == null)
            {
                runStateMachine = FindFirstObjectByType<RunStateMachine>();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var zone = other.GetComponent<ZoneCell>();
            if (zone != null && !zone.Resolved)
            {
                runStateMachine?.SetState(RunState.Failed);
            }
        }
    }
}
