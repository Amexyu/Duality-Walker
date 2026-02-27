using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class EndlessRunManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform scrollingRoot;

    [Header("Speed")]
    [SerializeField] private float startScrollSpeed = 1.8f;
    [SerializeField] private float maxScrollSpeed = 7.5f;
    [SerializeField] private float accelerationPerSecond = 0.08f;

    [Header("Game Over")]
    [SerializeField] private float leftOutOfScreenThreshold = -0.05f;
    [SerializeField] private string gameOverSceneName = "GAMEOVERUI";

    [Header("Score")]
    [SerializeField] private float distanceMultiplier = 10f;

    public float CurrentScrollSpeed { get; private set; }
    public float Distance { get; private set; }
    public bool IsGameOver { get; private set; }

    private Camera mainCam;

    private void Awake()
    {
        mainCam = Camera.main;
        CurrentScrollSpeed = startScrollSpeed;
    }

    private void Update()
    {
        if (IsGameOver) return;

        CurrentScrollSpeed = Mathf.Min(maxScrollSpeed, CurrentScrollSpeed + accelerationPerSecond * Time.deltaTime);
        Distance += CurrentScrollSpeed * distanceMultiplier * Time.deltaTime;

        if (scrollingRoot != null)
        {
            scrollingRoot.position += Vector3.left * (CurrentScrollSpeed * Time.deltaTime);
        }

        CheckOutOfScreen();
    }

    public void ApplyPenalty(float pushLeft)
    {
        if (player == null) return;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(-Mathf.Abs(pushLeft), rb.linearVelocity.y);
        }
        else
        {
            player.position += Vector3.left * Mathf.Abs(pushLeft) * 0.15f;
        }
    }

    private void CheckOutOfScreen()
    {
        if (player == null)
        {
            return;
        }

        if (mainCam == null)
        {
            mainCam = Camera.main;
            if (mainCam == null) return;
        }

        Vector3 vp = mainCam.WorldToViewportPoint(player.position);
        if (vp.x < leftOutOfScreenThreshold)
        {
            TriggerGameOver();
        }
    }

    private void TriggerGameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;

        ScoreStore.Set(Mathf.RoundToInt(Distance));

        if (!string.IsNullOrWhiteSpace(gameOverSceneName))
        {
            SceneManager.LoadScene(gameOverSceneName);
        }
    }
}
