using UnityEngine;

public class RunnerGameController : MonoBehaviour
{
    public enum GameState
    {
        Running,
        Paused,
        Failed
    }

    [SerializeField] private RunnerMotor runnerMotor;
    [SerializeField] private GameState initialState = GameState.Running;

    public GameState CurrentState { get; private set; }

    private void Awake()
    {
        SetState(initialState);
    }

    public void SetState(GameState newState)
    {
        CurrentState = newState;

        if (runnerMotor == null)
        {
            return;
        }

        bool shouldRun = CurrentState == GameState.Running;
        runnerMotor.SetRunning(shouldRun);
    }

    public void PauseGame()
    {
        if (CurrentState == GameState.Failed)
        {
            return;
        }

        SetState(GameState.Paused);
    }

    public void ResumeGame()
    {
        if (CurrentState == GameState.Failed)
        {
            return;
        }

        SetState(GameState.Running);
    }

    public void FailGame()
    {
        SetState(GameState.Failed);
    }
}
