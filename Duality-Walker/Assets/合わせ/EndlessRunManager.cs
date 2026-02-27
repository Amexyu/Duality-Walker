using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class EndlessRunManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform autoRunner;
    [SerializeField] private Transform scrollingRoot;

    [Header("Scroll Speed")]
    [SerializeField] private float startScrollSpeed = 1.8f;
    [SerializeField] private float maxScrollSpeed = 7.5f;
    [SerializeField] private float accelerationPerSecond = 0.08f;

    [Header("Runner")]
    [SerializeField] private float autoRunSpeed = 3.2f;
    [SerializeField] private float penaltyDistance = 0.8f;

    [Header("Game Over")]
    [SerializeField] private float leftOutOfScreenThreshold = -0.05f;
    [SerializeField] private string gameOverSceneName = "GAMEOVERUI";

    [Header("Score")]
    [SerializeField] private float distanceMultiplier = 10f;

    public float CurrentScrollSpeed { get; private set; }
    public float Distance { get; private set; }
    public bool IsGameOver { get; private set; }

    private Camera mainCam;
    private float fixedRunnerY;

    private void Awake()
    {
        mainCam = Camera.main;
        CurrentScrollSpeed = startScrollSpeed;

        if (autoRunner == null)
        {
            autoRunner = CreateFallbackRunner();
        }

        if (autoRunner != null)
        {
            fixedRunnerY = autoRunner.position.y;
            EnsureRunnerSetup(autoRunner.gameObject);
        }
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

        DriveRunner();
        CheckOutOfScreen();
    }

    public void ApplyPenalty(float multiplier)
    {
        if (autoRunner == null) return;

        float push = Mathf.Max(0.1f, penaltyDistance * Mathf.Max(0.2f, multiplier));
        autoRunner.position += Vector3.left * push;
    }

    public void AddDistanceBonus(int bonus)
    {
        Distance += Mathf.Max(0, bonus);
    }

    private void DriveRunner()
    {
        if (autoRunner == null) return;

        Vector3 pos = autoRunner.position;
        pos.x += autoRunSpeed * Time.deltaTime;
        pos.y = fixedRunnerY;
        autoRunner.position = pos;
    }

    private void CheckOutOfScreen()
    {
        if (autoRunner == null)
        {
            return;
        }

        if (mainCam == null)
        {
            mainCam = Camera.main;
            if (mainCam == null) return;
        }

        Vector3 vp = mainCam.WorldToViewportPoint(autoRunner.position);
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

    private Transform CreateFallbackRunner()
    {
        GameObject go = new GameObject("AutoRunnerNPC");
        go.transform.position = new Vector3(-2.5f, 0f, 0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSolidSprite();
        sr.color = new Color(0.95f, 0.45f, 0.2f, 1f);
        sr.sortingOrder = 10;
        go.transform.localScale = new Vector3(0.6f, 0.6f, 1f);

        EnsureRunnerSetup(go);
        return go.transform;
    }

    private void EnsureRunnerSetup(GameObject runner)
    {
        if (runner.GetComponent<BoxCollider2D>() == null)
        {
            runner.AddComponent<BoxCollider2D>();
        }

        Rigidbody2D rb = runner.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = runner.AddComponent<Rigidbody2D>();
        }

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;

        if (runner.GetComponent<AutoRunnerAgent>() == null)
        {
            runner.AddComponent<AutoRunnerAgent>();
        }
    }

    private static Sprite CreateSolidSprite()
    {
        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        tex.SetPixel(0, 0, Color.white);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}
