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
            TryBindReferences();
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
            TryBindReferences();
            runStateMachine?.SetState(RunState.Running);
            HandleStateChange(RunState.Running);
        }

        private void Update()
        {
            // 运行时兜底：若初始化顺序导致引用为空，自动重绑。
            if (runnerMotor == null || runStateMachine == null)
            {
                TryBindReferences();
                if (runStateMachine != null && runStateMachine.CurrentState == RunState.Running)
                {
                    HandleStateChange(RunState.Running);
                }
            }
        }

        private void OnDisable()
        {
            if (runStateMachine != null)
            {
                runStateMachine.OnStateChanged -= HandleStateChange;
            }
        }

        private void TryBindReferences()
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

        private void HandleStateChange(RunState state)
        {
            if (runnerMotor == null)
            {
                runnerMotor = FindFirstObjectByType<RunnerMotor>();
                if (runnerMotor == null)
                {
                    return;
                }
            }

            runnerMotor.SetRunning(state == RunState.Running);
        }
    }
}
