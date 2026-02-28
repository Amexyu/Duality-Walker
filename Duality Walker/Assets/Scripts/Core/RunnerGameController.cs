using DualityWalker.GameLoop;
using UnityEngine;

namespace DualityWalker.Core
{
    public class RunnerGameController : MonoBehaviour
    {
        [SerializeField] private RunnerMotor runnerMotor;
        [SerializeField] private RunStateMachine runStateMachine;

        private void Awake()
        {
            if (runStateMachine == null)
            {
                runStateMachine = FindFirstObjectByType<RunStateMachine>();
            }

            if (runnerMotor == null)
            {
                runnerMotor = FindFirstObjectByType<RunnerMotor>();
            }
        }

        private void OnEnable()
        {
            if (runStateMachine != null)
            {
                runStateMachine.OnStateChanged += HandleStateChange;
            }
        }

        private void Start()
        {
            runStateMachine?.SetState(RunState.Ready);
            runStateMachine?.SetState(RunState.Running);
        }

        private void OnDisable()
        {
            if (runStateMachine != null)
            {
                runStateMachine.OnStateChanged -= HandleStateChange;
            }
        }

        private void HandleStateChange(RunState state)
        {
            if (runnerMotor == null)
            {
                return;
            }

            runnerMotor.SetRunning(state == RunState.Running);
        }
    }
}
