using System;
using UnityEngine;

namespace DualityWalker.GameLoop
{
    public enum RunState
    {
        Ready,
        Running,
        Failed,
        Revive
    }

    public class RunStateMachine : MonoBehaviour
    {
        public event Action<RunState> OnStateChanged;

        [SerializeField] private RunState currentState = RunState.Ready;

        public RunState CurrentState => currentState;

        public void SetState(RunState next)
        {
            if (currentState == next)
            {
                return;
            }

            currentState = next;
            OnStateChanged?.Invoke(currentState);
        }

        public bool IsRunning() => currentState == RunState.Running;
    }
}
