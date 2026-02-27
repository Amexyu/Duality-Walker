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
    [SerializeField] private float runnerDrag = 4f;
    [SerializeField] private float penaltyImpulse = 4f;

    [Header("Game Over")]
    [SerializeField] private float leftOutOfScreenThreshold = -0.05f;
    [SerializeField] private string gameOverSceneName = "GAMEOVERUI";

    [Header("Score")]
    [SerializeField] private float distanceMultiplier = 10f;

    public float CurrentScrollSpeed { get; private set; }
    public float Distance { get; private set; }
    public bool IsGameOver { get; private set; }

    private Camera mainCam;
    private Rigidbody2D runnerRb;

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
            runnerRb = autoRunner.GetComponent<Rigidbody2D>();
            if (runnerRb == null)
            {
                runnerRb = autoRunner.gameObject.AddComponent<Rigidbody2D>();
            }

            runnerRb.gravityScale = 0f;
            runnerRb.drag = runnerDrag;
            runnerRb.constraints = RigidbodyConstraints2D.FreezeRotation;

            if (autoRunner.GetComponent<AutoRunnerAgent>() == null)
            {
                autoRunner.gameObject.AddComponent<AutoRunnerAgent>();
            }
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
        if (runnerRb == null) return;

        float impulse = Mathf.Max(0.1f, penaltyImpulse * Mathf.Max(0.2f, multiplier));
        runnerRb.AddForce(Vector2.left * impulse, ForceMode2D.Impulse);
    }

    public void AddDistanceBonus(int bonus)
    {
        Distance += Mathf.Max(0, bonus);
    }

    private void DriveRunner()
    {
        if (runnerRb == null) return;

        Vector2 vel = runnerRb.velocity;
        vel.x = Mathf.Max(vel.x, autoRunSpeed);
        runnerRb.velocity = vel;
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

        BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
        collider.isTrigger = false;

        return go.transform;
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
