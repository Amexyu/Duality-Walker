using UnityEngine;

public sealed class DualZoneHazardSpawner : MonoBehaviour
{
    [SerializeField] private EndlessRunManager runManager;
    [SerializeField] private Transform hazardsRoot;

    [Header("Spawn")]
    [SerializeField] private float spawnEverySeconds = 1.2f;
    [SerializeField] private float spawnAheadViewportX = 1.12f;
    [SerializeField] private float cleanupViewportX = -0.3f;

    [Header("Zones")]
    [SerializeField] private float topYMin = 0.8f;
    [SerializeField] private float topYMax = 3.8f;
    [SerializeField] private float bottomYMin = -3.8f;
    [SerializeField] private float bottomYMax = -0.8f;

    [Header("Pattern")]
    [SerializeField] private int minShapeSize = 2;
    [SerializeField] private int maxShapeSize = 5;

    private float timer;
    private Camera cam;
    private static Sprite sprite1x1;

    private void Awake()
    {
        cam = Camera.main;
        if (hazardsRoot == null) hazardsRoot = transform;
    }

    private void Update()
    {
        if (runManager == null || runManager.IsGameOver) return;

        timer += Time.deltaTime;
        if (timer >= spawnEverySeconds)
        {
            timer = 0f;
            SpawnTopObstacle();
            SpawnBottomPit();
        }

        CleanupOffscreen();
    }

    private void SpawnTopObstacle()
    {
        Vector3 pos = new Vector3(GetSpawnX(), Random.Range(topYMin, topYMax), 0f);
        var go = CreateHazard("WhiteObstacle", pos, new Vector2(Random.Range(0.6f, 1.1f), Random.Range(0.6f, 1.5f)), Color.white);

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = false;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        int shapeSize = Random.Range(minShapeSize, maxShapeSize + 1);
        go.AddComponent<HazardPattern>().Setup(HazardZone.Top, shapeSize);

        var hit = go.AddComponent<HazardHit>();
        hit.Setup(runManager, 1.0f);
    }

    private void SpawnBottomPit()
    {
        Vector3 pos = new Vector3(GetSpawnX(), Random.Range(bottomYMin, bottomYMax), 0f);
        var go = CreateHazard("BlackPit", pos, new Vector2(Random.Range(0.8f, 1.6f), Random.Range(0.45f, 0.9f)), new Color(0f, 0f, 0f, 0.9f));

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        int shapeSize = Random.Range(minShapeSize, maxShapeSize + 1);
        go.AddComponent<HazardPattern>().Setup(HazardZone.Bottom, shapeSize);

        var hit = go.AddComponent<HazardHit>();
        hit.Setup(runManager, 1.4f);
    }

    private GameObject CreateHazard(string name, Vector3 worldPos, Vector2 size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(hazardsRoot, false);
        go.transform.position = worldPos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetSprite();
        sr.color = color;
        sr.sortingOrder = -20;
        return go;
    }

    private float GetSpawnX()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return 10f;

        float z = -cam.transform.position.z;
        return cam.ViewportToWorldPoint(new Vector3(spawnAheadViewportX, 0.5f, z)).x;
    }

    private void CleanupOffscreen()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        float z = -cam.transform.position.z;
        float cleanupX = cam.ViewportToWorldPoint(new Vector3(cleanupViewportX, 0.5f, z)).x;

        for (int i = hazardsRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = hazardsRoot.GetChild(i);
            if (child.position.x < cleanupX)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private static Sprite GetSprite()
    {
        if (sprite1x1 != null) return sprite1x1;

        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        tex.SetPixel(0, 0, Color.white);
        tex.Apply();

        sprite1x1 = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return sprite1x1;
    }
}
