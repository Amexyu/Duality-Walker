using UnityEngine;

public class HazardSpawner : MonoBehaviour
{
    [Header("Runner（NPC）")]
    public Transform runner;

    [Header("Prefabs")]
    public GameObject obstaclePrefab;
    public GameObject pitPrefab;

    [Header("Spawn")]
    public float safeStartDistance = 8f;     // 开局缓冲
    public float spawnEveryDistance = 7f;    // 每跑多远刷一次
    public float spawnAhead = 12f;           // 刷在屏幕右侧多远
    [Range(0f, 1f)] public float pitChance = 0.25f;

    [Header("Y Positions")]
    public float obstacleY = 0.5f;           // 白区
    public float pitY = -0.5f;               // 黑区

    Camera _cam;
    float _nextSpawnAt;

    void Start()
    {
        _cam = Camera.main;
        _nextSpawnAt = safeStartDistance;
    }

    void Update()
    {
        var gm = GameManager.I;
        if (gm == null || gm.IsGameOver) return;

        if (gm.Distance < _nextSpawnAt) return;
        _nextSpawnAt += spawnEveryDistance;

        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        float z = Mathf.Abs(_cam.transform.position.z);
        float camRight = _cam.ViewportToWorldPoint(new Vector3(1f, 0.5f, z)).x;
        float x = camRight + spawnAhead;

        bool spawnPit = (pitPrefab != null) && (Random.value < pitChance);

        if (spawnPit)
        {
            var go = Instantiate(pitPrefab, new Vector3(x, pitY, 0f), Quaternion.identity);
            var pit = go.GetComponent<PitController>();
            if (pit) pit.runner = runner;
        }
        else
        {
            Instantiate(obstaclePrefab, new Vector3(x, obstacleY, 0f), Quaternion.identity);
        }
    }
}