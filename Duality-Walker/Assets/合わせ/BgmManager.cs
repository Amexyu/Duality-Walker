using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class BgmManager : MonoBehaviour
{
    [Header("Music")]
    [SerializeField] private AudioClip music;
    [SerializeField] [Range(0f, 1f)] private float volume = 0.7f;
    [SerializeField] private bool loop = true;

    [Header("Fade")]
    [SerializeField] private float fadeInSeconds = 0.8f;
    [SerializeField] private float fadeOutSeconds = 0.8f;

    private static BgmManager s_instance;
    private AudioSource _src;
    private float _targetVol;

    private void Awake()
    {
        if (s_instance != null && s_instance != this)
        {
            Destroy(gameObject);
            return;
        }
        s_instance = this;
        DontDestroyOnLoad(gameObject);

        _src = gameObject.GetComponent<AudioSource>();
        if (_src == null) _src = gameObject.AddComponent<AudioSource>();
        _src.playOnAwake = false;
        _src.loop = loop;
        _src.spatialBlend = 0f; // 2D
        _src.rolloffMode = AudioRolloffMode.Linear;
        _src.clip = music;
        _src.volume = 0f;
        _targetVol = Mathf.Clamp01(volume);

        if (_src.clip != null)
        {
            _src.Play();
            if (fadeInSeconds > 0f) StartCoroutine(FadeTo(_targetVol, fadeInSeconds));
            else _src.volume = _targetVol;
        }

        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    private void OnDestroy()
    {
        if (s_instance == this) s_instance = null;
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }

    private void OnSceneChanged(Scene prev, Scene next)
    {
        // 如需在某些场景静音，可在这里判断 next.name 并淡出
        // 例如：if (next.name == "GameOver") { FadeOutAndStop(); }
    }

    public static void SetMusic(AudioClip clip, float vol = 0.7f, bool doFade = true, float fadeSec = 0.8f)
    {
        if (s_instance == null) return;
        var m = s_instance;
        if (m._src.clip == clip) return;

        m._targetVol = Mathf.Clamp01(vol);
        m.StopCurrent(doFade ? m.fadeOutSeconds : 0f);
        m._src.clip = clip;

        if (clip != null)
        {
            m._src.volume = 0f;
            m._src.Play();
            if (doFade && fadeSec > 0f) m.StartCoroutine(m.FadeTo(m._targetVol, fadeSec));
            else m._src.volume = m._targetVol;
        }
    }

    public void FadeOutAndStop()
    {
        StopCurrent(fadeOutSeconds);
    }

    private void StopCurrent(float fadeSec)
    {
        if (!_src.isPlaying) return;
        if (fadeSec > 0f) StartCoroutine(FadeTo(0f, fadeSec, stopAfterFade: true));
        else { _src.Stop(); _src.volume = 0f; }
    }

    private System.Collections.IEnumerator FadeTo(float target, float time, bool stopAfterFade = false)
    {
        float start = _src.volume;
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            float k = (time <= 0f) ? 1f : Mathf.Clamp01(t / time);
            _src.volume = Mathf.Lerp(start, target, k);
            yield return null;
        }
        _src.volume = target;
        if (stopAfterFade) _src.Stop();
    }
}