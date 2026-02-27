 using System.Collections;
using UnityEngine;

public sealed class BombSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField, Tooltip("生成する爆弾のPrefab（BOMBX付き）")]
    private GameObject _bombPrefab;

    [Header("Spawn 設定")]
    [SerializeField, Tooltip("ゲーム開始からスポーン開始までの遅延(秒)")]
    private float _startDelaySeconds = 20f;

    [SerializeField, Tooltip("開始時にまとめて生成する個数")]
    private int _initialSpawnCount = 0;

    [SerializeField, Tooltip("自動生成の固定間隔(秒)。ランダム未使用時に有効")]
    private float _spawnInterval = 0f;

    [SerializeField, Tooltip("自動生成のランダム間隔 最小値(秒)。0以下なら固定間隔を使用")]
    private float _spawnIntervalMin = 0f;

    [SerializeField, Tooltip("自動生成のランダム間隔 最大値(秒)。Min>0 かつ Max>=Min で有効")]
    private float _spawnIntervalMax = 0f;

    [SerializeField, Tooltip("同時に存在できる最大数（親子付けでカウント）")]
    private int _maxAlive = 10;

    [Header("ランダム範囲(ローカル)")]
    [SerializeField, Tooltip("中心オフセット（ワールド座標 = Spawner座標 + これ）")]
    private Vector2 _areaCenterOffset = Vector2.zero;

    [SerializeField, Tooltip("範囲サイズ（幅x高さ）")]
    private Vector2 _areaSize = new Vector2(10f, 6f);

    [Header("配置の重なり回避(2D)")]
    [SerializeField, Tooltip("重なり回避の半径（爆弾の見た目サイズに合わせて調整）")]
    private float _noOverlapRadius = 0.4f;

    [SerializeField, Tooltip("このレイヤーに対して重なりを回避する（Everything推奨、必要に応じて絞る）")]
    private LayerMask _checkLayers = ~0;

    [SerializeField, Tooltip("スポーン失敗時の最大試行回数")]
    private int _maxSpawnAttemptsPerBomb = 20;

    [Header("Despawn")]
    [SerializeField, Tooltip("未接触で自動消滅させるまでの秒数。0以下で無効")]
    private float _autoDespawnSeconds = 15f;

    private Coroutine _loop;

    private void Start()
    {
        StartCoroutine(StartAfterDelay());
    }

    private IEnumerator StartAfterDelay()
    {
        if (_startDelaySeconds > 0f)
            yield return new WaitForSeconds(_startDelaySeconds);

        // 初期生成
        for (int i = 0; i < _initialSpawnCount; i++)
            SpawnOne();

        // 自動生成開始
        float next = GetNextInterval();
        if (next > 0f)
            _loop = StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (enabled)
        {
            if (transform.childCount < _maxAlive)
                SpawnOne();

            float wait = GetNextInterval();
            if (wait <= 0f) yield break;
            yield return new WaitForSeconds(wait);
        }
    }

    private float GetNextInterval()
    {
        if (_spawnIntervalMin > 0f && _spawnIntervalMax >= _spawnIntervalMin)
            return Random.Range(_spawnIntervalMin, _spawnIntervalMax);

        return _spawnInterval > 0f ? _spawnInterval : 0f;
    }

    [ContextMenu("Spawn Now")]
    public void SpawnNow()
    {
        SpawnOne();
    }

    private void SpawnOne()
    {
        if (_bombPrefab == null) return;
        if (transform.childCount >= _maxAlive) return;

        if (!TryGetSpawnPosition(out Vector3 pos)) return;

        var go = Instantiate(_bombPrefab, pos, Quaternion.identity, transform);

        // 未接触なら一定時間後に自動消滅（BOMBX の演出を優先して呼ぶ）
        if (_autoDespawnSeconds > 0f)
            StartCoroutine(AutoDespawn(go, _autoDespawnSeconds));
    }

    private IEnumerator AutoDespawn(GameObject bomb, float seconds)
    {
        yield return new WaitForSeconds(seconds);

        if (bomb == null) yield break;
        if (!bomb.activeInHierarchy) yield break;

        // BOMBX の Disappear を呼んで爆発演出込みで消す（無ければ Destroy）
        bomb.SendMessage("Disappear", SendMessageOptions.DontRequireReceiver);

        // Disappear を持たない/無視された場合の保険
        if (bomb != null) Destroy(bomb);
    }

    private bool TryGetSpawnPosition(out Vector3 pos)
    {
        Vector3 center = transform.position + (Vector3)_areaCenterOffset;
        for (int i = 0; i < _maxSpawnAttemptsPerBomb; i++)
        {
            float rx = Random.Range(-_areaSize.x * 0.5f, _areaSize.x * 0.5f);
            float ry = Random.Range(-_areaSize.y * 0.5f, _areaSize.y * 0.5f);
            Vector3 candidate = center + new Vector3(rx, ry, 0f);

            if (_noOverlapRadius > 0f)
            {
                var hit = Physics2D.OverlapCircle(candidate, _noOverlapRadius, _checkLayers);
                if (hit != null) continue;
            }

            pos = candidate;
            return true;
        }

        pos = default;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.25f);
        Vector3 center = transform.position + (Vector3)_areaCenterOffset;
        Gizmos.DrawCube(center, new Vector3(_areaSize.x, _areaSize.y, 0.1f));
        Gizmos.color = new Color(1f, 0.6f, 0f, 1f);
        Gizmos.DrawWireCube(center, new Vector3(_areaSize.x, _areaSize.y, 0.1f));
    }
}