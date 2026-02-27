using UnityEngine;

public class BombManager : MonoBehaviour
{
    public static BombManager Instance { get; private set; } 
    
    [Header("Bomb Spawn Settings")]
    public GameObject bombPrefab;
    public float minSpawnInterval = 5f;
    public float maxSpawnInterval = 10f;
    public bool autoSpawn = true;
    
    [Header("Spawn Limit Settings")]
    public int maxBombs = 20;
    public bool limitBombs = true;
    
    private float spawnTimer;
    private float nextSpawnTime;
    private int currentBombCount = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        InitializeSpawnSystem();
    }

    void Update()
    {
        if (autoSpawn && bombPrefab != null)
        {
            UpdateSpawnTimer();
        }
        UpdateBombCount();
    }
    
    private void InitializeSpawnSystem()
    {
        spawnTimer = 0f;
        SetNextSpawnTime();
    }
    
    private void UpdateSpawnTimer()
    {
        spawnTimer += Time.deltaTime;
        
        if (spawnTimer >= nextSpawnTime && CanSpawnBomb())
        {
            SpawnBomb();
            spawnTimer = 0f;
            SetNextSpawnTime();
        }
    }
    
    private bool CanSpawnBomb()
    {
        return !limitBombs || currentBombCount < maxBombs;
    }
    
    private void SetNextSpawnTime()
    {
        nextSpawnTime = Random.Range(minSpawnInterval, maxSpawnInterval);
    }
    
    private void SpawnBomb()
    {
        if (bombPrefab != null)
        {
            Instantiate(bombPrefab);
            currentBombCount++;
            Debug.Log($"”š’e‚ğ¶¬‚µ‚Ü‚µ‚½BŒ»İ‚Ì”š’e”: {currentBombCount}");
        }
    }
    
    private void UpdateBombCount()
    {
        currentBombCount = FindObjectsOfType<Bomb>().Length;
    }
    
    public void OnBombDestroyed()
    {
        currentBombCount = Mathf.Max(0, currentBombCount - 1);
    }
    
    [ContextMenu("è“®‚Å”š’e¶¬")]
    public void ManualSpawnBomb()
    {
        if (CanSpawnBomb())
        {
            SpawnBomb();
        }
       
    }
    
    [ContextMenu("‘S”š’eíœ")]
    public void DestroyAllBombs()
    {
        Bomb[] bombs = FindObjectsOfType<Bomb>();
        foreach (Bomb bomb in bombs)
        {
            if (bomb != null) Destroy(bomb.gameObject);
        }
        currentBombCount = 0;
    }
}