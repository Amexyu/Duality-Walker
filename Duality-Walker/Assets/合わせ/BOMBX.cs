using System.Collections;
using UnityEngine;

public sealed class BOMBX : MonoBehaviour
{
    [SerializeField] private string _playerTag = "Player";
    [SerializeField] private bool _useDestroy = true;
    [SerializeField] private bool _preventFall = true;
    [SerializeField] private RigidbodyType2D _bodyType = RigidbodyType2D.Static;

    [Header("Idle (懸婡傾僯儊)")]
    [SerializeField] private Sprite[] _idleFrames;
    [SerializeField] private float _idleFps = 8f;

    [Header("Explosion")]
    [SerializeField] private Sprite[] _explosionFrames;
    [SerializeField] private float _explosionFps = 12f;
    [SerializeField] private Vector2 _explosionOffset = Vector2.zero;
    [SerializeField] private bool _hideBombSpriteOnExplode = true;

    [Header("Carve")]
    [Tooltip("爆炸挖洞半径（单位：格）。例如 1 表示以1格半径的圆形范围删除玩家格子。")]
    [SerializeField] private float carveRadiusCells = 1f;

    [Header("Audio")]
    [Tooltip("爆炸时播放的音效")]
    [SerializeField] private AudioClip _explosionClip;
    [SerializeField] [Range(0f, 1f)] private float _explosionVolume = 0.9f;
    [SerializeField] [Range(0.5f, 2f)] private float _explosionPitch = 1.0f;
    [Tooltip("如设置，优先使用该 AudioSource 播放；否则临时创建播放一次")]
    [SerializeField] private AudioSource _audioSource;

    [Header("Lifetime")]
    [Tooltip("炸弹在该秒数后若未被触发则静默消失（不播放音效/动画/挖洞）。")]
    [SerializeField] private float _selfDestructSeconds = 15f;

    private Rigidbody2D _rb;
    private SpriteRenderer _bombRenderer;
    private bool _isDisappearing;
    private Coroutine _idleCo;

    private GameObject _explosionGo;
    private Coroutine _explosionCo;

    private PlayerGroup2D _playerGroup;
    private Coroutine _selfDestructCo;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _bombRenderer = GetComponent<SpriteRenderer>();

        if (_preventFall && _rb != null)
        {
            _rb.bodyType = _bodyType;

            if (_bodyType == RigidbodyType2D.Dynamic)
            {
                _rb.gravityScale = 0f;
                _rb.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
            }
            else if (_bodyType == RigidbodyType2D.Kinematic)
            {
                _rb.gravityScale = 0f;
                _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }
        }

        _playerGroup = FindObjectOfType<PlayerGroup2D>();
    }

    private void OnEnable()
    {
        if (!_isDisappearing && _idleFrames != null && _idleFrames.Length > 0 && _bombRenderer != null)
        {
            _idleCo = StartCoroutine(PlayIdleLoop());
        }

        if (_selfDestructSeconds > 0f)
            _selfDestructCo = StartCoroutine(SelfDestructAfter(_selfDestructSeconds));
    }

    private void OnDisable()
    {
        if (_idleCo != null)
        {
            StopCoroutine(_idleCo);
            _idleCo = null;
        }

        if (_explosionCo != null)
        {
            StopCoroutine(_explosionCo);
            _explosionCo = null;
        }
        if (_explosionGo != null)
        {
            Destroy(_explosionGo);
            _explosionGo = null;
        }

        if (_selfDestructCo != null)
        {
            StopCoroutine(_selfDestructCo);
            _selfDestructCo = null;
        }
    }

    private void Update()
    {
        // 兜底：AABB 与玩家整体交叠时也触发爆炸（玩家触发 -> 有音效/挖洞/动画）
        if (!_isDisappearing && _playerGroup != null && _bombRenderer != null)
        {
            _playerGroup.GetWorldAABB(out Vector2 pMin, out Vector2 pMax);

            Bounds b = _bombRenderer.bounds;
            Vector2 aMin = b.min;
            Vector2 aMax = b.max;

            if (AABBOverlap(aMin, aMax, pMin, pMax))
            {
                // 用炸弹中心在玩家AABB上的最近点作为爆炸圆心
                Vector2 center = transform.position;
                Vector2 closest = new Vector2(Mathf.Clamp(center.x, pMin.x, pMax.x), Mathf.Clamp(center.y, pMin.y, pMax.y));
                DisappearAt(closest); // 玩家触发
            }
        }
    }

    private static bool AABBOverlap(Vector2 aMin, Vector2 aMax, Vector2 bMin, Vector2 bMax)
    {
        return !(aMax.x < bMin.x || aMin.x > bMax.x || aMax.y < bMin.y || aMin.y > bMax.y);
    }

    private IEnumerator PlayIdleLoop()
    {
        float frameTime = 1f / Mathf.Max(1f, _idleFps);
        int i = 0;
        while (!_isDisappearing)
        {
            _bombRenderer.sprite = _idleFrames[i];
            yield return new WaitForSeconds(frameTime);
            i = (i + 1) % _idleFrames.Length;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isDisappearing) return;

        if (other != null && (other.CompareTag(_playerTag) || other.GetComponentInParent<PlayerGroup2D>() != null))
        {
            // 触发器：用对方碰撞体上“最接近炸弹中心”的点
            Vector2 hit = other.ClosestPoint(transform.position);
            DisappearAt(hit);
        }
    }
            
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_isDisappearing) return;

        var col = collision.collider;
        if (col != null && (col.CompareTag(_playerTag) || col.GetComponentInParent<PlayerGroup2D>() != null))
        {
            // 刚体碰撞：用第一个接触点
            Vector2 hit = collision.contactCount > 0 ? collision.GetContact(0).point : (Vector2)transform.position;
            DisappearAt(hit);
        }
    }

    // 玩家触发的爆炸：有音效/挖洞/动画（使用命中的世界点作为圆心）
    private void DisappearAt(Vector2 hitWorld)
    {
        if (_isDisappearing) return;
        _isDisappearing = true;

        // 倒计时 -15 秒（仅玩家触发时）
        //Timer.AddTime(-15f);

        // 播放爆炸音效（仅玩家触发）
        PlayExplosionSfx();

        // 先“挖洞”：圆心 = 命中点 + 偏移，半径 = carveRadiusCells（格）
        if (_playerGroup == null)
        {
            _playerGroup = FindObjectOfType<PlayerGroup2D>();
        }
        if (_playerGroup != null && carveRadiusCells > 0f)
        {
            Vector2 explosionCenter = hitWorld + _explosionOffset;
            _playerGroup.CarveHoleAtWorld(explosionCenter, carveRadiusCells);
        }

        if (_idleCo != null)
        {
            StopCoroutine(_idleCo);
            _idleCo = null;
        }

        if (_explosionFrames != null && _explosionFrames.Length > 0)
        {
            _explosionCo = StartCoroutine(PlayExplosionAndFinish());
        }
        else
        {
            FinishDisappear();
        }
    }

    // 兼容旧调用：圆心取炸弹中心
    private void Disappear()
    {
        DisappearAt(transform.position);
    }

    // 静默自毁：无音效、无挖洞、无爆炸动画
    private void DespawnSilently()
    {
        if (_isDisappearing) return;
        _isDisappearing = true;

        if (_idleCo != null)
        {
            StopCoroutine(_idleCo);
            _idleCo = null;
        }

        FinishDisappear();
    }

    private IEnumerator SelfDestructAfter(float seconds)
    {
        // 使用实时时间，避免暂停/减速影响自毁
        yield return new WaitForSecondsRealtime(seconds);
        if (!_isDisappearing)
        {
            DespawnSilently();
        }
    }

    private IEnumerator PlayExplosionAndFinish()
    {
        _explosionGo = new GameObject("Explosion");
        _explosionGo.transform.position = transform.position + (Vector3)_explosionOffset;
        _explosionGo.transform.localScale = transform.localScale;

        var sr = _explosionGo.AddComponent<SpriteRenderer>();
        if (_bombRenderer != null)
        {
            sr.sortingLayerID = _bombRenderer.sortingLayerID;
            sr.sortingOrder = _bombRenderer.sortingOrder + 1;
        }

        if (_hideBombSpriteOnExplode && _bombRenderer != null)
        {
            _bombRenderer.enabled = false;
        }

        float frameTime = 1f / Mathf.Max(1f, _explosionFps);
        for (int i = 0; i < _explosionFrames.Length; i++)
        {
            if (sr == null) break;
            sr.sprite = _explosionFrames[i];
            yield return new WaitForSecondsRealtime(frameTime);
        }

        if (_explosionGo != null)
        {
            Destroy(_explosionGo);
            _explosionGo = null;
        }

        _explosionCo = null;
        FinishDisappear();
    }

    private void FinishDisappear()
    {
        if (_useDestroy)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void PlayExplosionSfx()
    {
        if (_explosionClip == null) return;

        if (_audioSource != null)
        {
            float originalPitch = _audioSource.pitch;
            _audioSource.pitch = _explosionPitch;
            _audioSource.PlayOneShot(_explosionClip, _explosionVolume);
            _audioSource.pitch = originalPitch;
            return;
        }

        GameObject temp = new GameObject("ExplosionSFX");
        temp.transform.position = transform.position;
        var src = temp.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f; // 2D；若需3D可改为1并配置距离
        src.pitch = _explosionPitch;
        src.volume = _explosionVolume;
        src.clip = _explosionClip;
        src.loop = false;
        src.rolloffMode = AudioRolloffMode.Linear;

        src.Play();
        Destroy(temp, _explosionClip.length / Mathf.Max(0.01f, _explosionPitch));
    }
}
