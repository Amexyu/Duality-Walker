using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button backButton;

    [Header("Scene")]
    [SerializeField] private string gameSceneName = "Game";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Start Intro")]
    [SerializeField] private STARTUINPC startUiNpc;
    [SerializeField] private float startSceneDelaySeconds = 2f;

    [Header("Button SFX")]
    [SerializeField] private AudioSource uiSfxSource;
    [SerializeField] private AudioClip defaultButtonClickSfx;
    [SerializeField] private AudioClip startClickSfx;
    [SerializeField] private AudioClip restartClickSfx;
    [SerializeField] private AudioClip backClickSfx;
    [SerializeField] private AudioClip quitClickSfx;
    [SerializeField] [Min(0f)] private float buttonClickVolume = 1.5f;
    [SerializeField] private float sceneLoadDelayAfterClick = 0.08f;

    [Header("StartUI BGM")]
    [SerializeField] private AudioSource startUiBgmSource;
    [SerializeField] private AudioClip startUiBgmClip;
    [SerializeField] [Range(0f, 1f)] private float startUiBgmVolume = 0.7f;
    [SerializeField] private bool loopStartUiBgm = true;

    [Header("Game BGM")]
    [SerializeField] private AudioClip gameBgmClip;
    [SerializeField] [Range(0f, 1f)] private float gameBgmVolume = 0.8f;

    private const string PersistentBgmRootNamePrefix = "PersistentGameBgm";

    private bool isStarting;
    private bool isSceneLoading;
    private Coroutine startCoroutine;

    private void Awake()
    {
        EnsureUiSfxSource();
        EnsureStartUiBgmSource();

        if (startButton != null)
        {
            startButton.onClick.AddListener(OnClickStart);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnClickQuit);
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnClickRestart);
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnClickBack);
        }
    }

    private void OnEnable()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        isStarting = false;
        isSceneLoading = false;

        StopAllPersistentGameBgm();
        PlayStartUiBgm();

        if (startButton != null)
        {
            startButton.gameObject.SetActive(true);
        }

        if (quitButton != null)
        {
            quitButton.gameObject.SetActive(true);
        }
    }

    private void OnDisable()
    {
        if (startCoroutine != null)
        {
            StopCoroutine(startCoroutine);
            startCoroutine = null;
        }

        StopStartUiBgm();
    }

    private void OnDestroy()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnClickStart);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(OnClickQuit);
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(OnClickRestart);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(OnClickBack);
        }
    }

    public void OnClickStart()
    {
        if (isStarting || isSceneLoading)
        {
            return;
        }

        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogWarning("[MainMenuController] gameSceneName 为空。", this);
            return;
        }

        PlayButtonClickSfx(startClickSfx);
        StopStartUiBgm();
        PlayGameBgmImmediately(true);

        isStarting = true;

        if (startButton != null)
        {
            startButton.gameObject.SetActive(false);
        }

        if (quitButton != null)
        {
            quitButton.gameObject.SetActive(false);
        }

        if (startUiNpc != null)
        {
            startUiNpc.PlayRunAndMoveRight(null);
        }

        startCoroutine = StartCoroutine(DelayLoadGameScene());
    }

    private IEnumerator DelayLoadGameScene()
    {
        float delay = Mathf.Max(0f, startSceneDelaySeconds);
        yield return new WaitForSecondsRealtime(delay);
        LoadGameScene();
    }

    public void OnClickRestart()
    {
        StartSceneLoadWithClick(gameSceneName, "gameSceneName", restartClickSfx);
    }

    public void OnClickBack()
    {
        StopStartUiBgm();
        StopAllPersistentGameBgm();
        StartSceneLoadWithClick(mainMenuSceneName, "mainMenuSceneName", backClickSfx);
    }

    public void OnClickQuit()
    {
        if (isSceneLoading)
        {
            return;
        }

        StopStartUiBgm();

        float sfxLen = PlayButtonClickSfx(quitClickSfx);
        float delay = Mathf.Max(sceneLoadDelayAfterClick, sfxLen > 0f ? Mathf.Min(0.25f, sfxLen) : 0f);
        StartCoroutine(QuitAfterDelay(delay));
    }

    private IEnumerator QuitAfterDelay(float delay)
    {
        isSceneLoading = true;
        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void StartSceneLoadWithClick(string sceneName, string fieldName, AudioClip clickClip)
    {
        if (isSceneLoading || isStarting)
        {
            return;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[MainMenuController] " + fieldName + " 为空。", this);
            return;
        }

        StopStartUiBgm();

        float sfxLen = PlayButtonClickSfx(clickClip);
        float delay = Mathf.Max(sceneLoadDelayAfterClick, sfxLen > 0f ? Mathf.Min(0.25f, sfxLen) : 0f);
        StartCoroutine(LoadSceneAfterDelay(sceneName, delay));
    }

    private IEnumerator LoadSceneAfterDelay(string sceneName, float delay)
    {
        isSceneLoading = true;

        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        SceneManager.LoadScene(sceneName);
    }

    private void LoadGameScene()
    {
        if (isSceneLoading)
        {
            return;
        }

        isSceneLoading = true;
        SceneManager.LoadScene(gameSceneName);
    }

    private void EnsureUiSfxSource()
    {
        if (uiSfxSource == null)
        {
            uiSfxSource = GetComponent<AudioSource>();
        }

        if (uiSfxSource == null)
        {
            uiSfxSource = gameObject.AddComponent<AudioSource>();
        }

        uiSfxSource.playOnAwake = false;
        uiSfxSource.loop = false;
        uiSfxSource.spatialBlend = 0f;
    }

    private void EnsureStartUiBgmSource()
    {
        if (startUiBgmSource == null)
        {
            startUiBgmSource = gameObject.AddComponent<AudioSource>();
        }

        startUiBgmSource.playOnAwake = false;
        startUiBgmSource.loop = loopStartUiBgm;
        startUiBgmSource.spatialBlend = 0f;
    }

    private void PlayStartUiBgm()
    {
        if (startUiBgmSource == null || startUiBgmClip == null)
        {
            return;
        }

        startUiBgmSource.loop = loopStartUiBgm;
        startUiBgmSource.volume = Mathf.Clamp01(startUiBgmVolume);

        if (startUiBgmSource.clip != startUiBgmClip)
        {
            startUiBgmSource.clip = startUiBgmClip;
        }

        if (!startUiBgmSource.isPlaying)
        {
            startUiBgmSource.Play();
        }
    }

    private void StopStartUiBgm()
    {
        if (startUiBgmSource == null)
        {
            return;
        }

        startUiBgmSource.Stop();
    }

    private float PlayButtonClickSfx(AudioClip clickClip)
    {
        if (uiSfxSource == null)
        {
            return 0f;
        }

        AudioClip clip = clickClip != null ? clickClip : defaultButtonClickSfx;
        if (clip == null)
        {
            return 0f;
        }

        float volume = Mathf.Max(0f, buttonClickVolume);
        uiSfxSource.PlayOneShot(clip, volume);
        return clip.length;
    }

    private void PlayGameBgmImmediately(bool forceRestart)
    {
        if (gameBgmClip == null)
        {
            Debug.LogWarning("[MainMenuController] gameBgmClip 未赋值，无法播放背景音乐。", this);
            return;
        }

        AudioSource source = FindAnyPersistentGameBgmSource();
        if (source == null)
        {
            GameObject bgmRoot = new GameObject(PersistentBgmRootNamePrefix);
            DontDestroyOnLoad(bgmRoot);

            source = bgmRoot.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = true;
        }

        source.mute = false;
        source.volume = Mathf.Clamp01(gameBgmVolume);

        bool clipChanged = source.clip != gameBgmClip;
        source.clip = gameBgmClip;

        if (forceRestart || clipChanged || !source.isPlaying)
        {
            source.Stop();
            source.Play();
        }
    }

    private AudioSource FindAnyPersistentGameBgmSource()
    {
        AudioSource[] sources = FindObjectsOfType<AudioSource>(true);
        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource source = sources[i];
            if (source == null || source.gameObject == null)
            {
                continue;
            }

            if (source.gameObject.name.StartsWith(PersistentBgmRootNamePrefix))
            {
                return source;
            }
        }

        return null;
    }

    private void StopAllPersistentGameBgm()
    {
        AudioSource[] sources = FindObjectsOfType<AudioSource>(true);
        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource source = sources[i];
            if (source == null || source.gameObject == null)
            {
                continue;
            }

            if (!source.gameObject.name.StartsWith(PersistentBgmRootNamePrefix))
            {
                continue;
            }

            source.Stop();
            Destroy(source.gameObject);
        }
    }
}