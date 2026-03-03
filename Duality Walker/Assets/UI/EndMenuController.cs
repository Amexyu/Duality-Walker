using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndMenuController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button quitButton;

    [Header("Scene Names")]
    [SerializeField] private string gameSceneName = "Game";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Button SFX")]
    [SerializeField] private AudioSource uiSfxSource;
    [SerializeField] private AudioClip defaultButtonClickSfx;
    [SerializeField] private AudioClip restartClickSfx;
    [SerializeField] private AudioClip backClickSfx;
    [SerializeField] private AudioClip quitClickSfx;
    [SerializeField] [Range(0f, 1f)] private float buttonClickVolume = 1f;
    [SerializeField] private float sceneLoadDelayAfterClick = 0.08f;

    [Header("GameOverUI BGM")]
    [SerializeField] private AudioSource gameOverUiBgmSource;
    [SerializeField] private AudioClip gameOverUiBgmClip;
    [SerializeField] [Range(0f, 1f)] private float gameOverUiBgmVolume = 0.7f;
    [SerializeField] private bool loopGameOverUiBgm = true;

    [Header("Game BGM")]
    [SerializeField] private AudioClip gameBgmClip;
    [SerializeField] [Range(0f, 1f)] private float gameBgmVolume = 0.8f;

    [Header("Run Distance")]
    [SerializeField] private TMP_Text runDistanceText;
    [SerializeField] private string runDistanceFormat = "本次前进距离：{0:0.0}";

    private const string PersistentBgmRootNamePrefix = "PersistentGameBgm";

    private bool isLoading;

    private void Awake()
    {
        EnsureUiSfxSource();
        EnsureGameOverUiBgmSource();

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnClickRestart);
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnClickBack);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnClickQuit);
        }
    }

    private void OnEnable()
    {
        StopAllPersistentGameBgm();
        PlayGameOverUiBgm();
        RefreshRunDistanceText();
    }

    private void OnDisable()
    {
        StopGameOverUiBgm();
    }

    private void OnDestroy()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(OnClickRestart);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(OnClickBack);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(OnClickQuit);
        }
    }

    public void OnClickRestart()
    {
        StopGameOverUiBgm();
        PlayGameBgmImmediately(true);
        LoadSceneSafe(gameSceneName, "gameSceneName", restartClickSfx);
    }

    public void OnClickBack()
    {
        StopGameOverUiBgm();
        StopAllPersistentGameBgm();
        LoadSceneSafe(mainMenuSceneName, "mainMenuSceneName", backClickSfx);
    }

    public void OnClickQuit()
    {
        if (isLoading)
        {
            return;
        }

        isLoading = true;
        StopGameOverUiBgm();

        Time.timeScale = 1f;
        AudioListener.pause = false;

        float sfxLen = PlayButtonClickSfx(quitClickSfx);
        float delay = Mathf.Max(sceneLoadDelayAfterClick, sfxLen > 0f ? Mathf.Min(0.25f, sfxLen) : 0f);
        StartCoroutine(QuitAfterDelay(delay));
    }

    private IEnumerator QuitAfterDelay(float delay)
    {
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

    private void LoadSceneSafe(string sceneName, string fieldName, AudioClip clickClip)
    {
        if (isLoading)
        {
            return;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[EndMenuController] " + fieldName + " 为空。", this);
            return;
        }

        isLoading = true;

        Time.timeScale = 1f;
        AudioListener.pause = false;

        float sfxLen = PlayButtonClickSfx(clickClip);
        float delay = Mathf.Max(sceneLoadDelayAfterClick, sfxLen > 0f ? Mathf.Min(0.25f, sfxLen) : 0f);
        StartCoroutine(LoadSceneAfterDelay(sceneName, delay));
    }

    private IEnumerator LoadSceneAfterDelay(string sceneName, float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        SceneManager.LoadScene(sceneName);
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

    private void EnsureGameOverUiBgmSource()
    {
        if (gameOverUiBgmSource == null)
        {
            gameOverUiBgmSource = gameObject.AddComponent<AudioSource>();
        }

        gameOverUiBgmSource.playOnAwake = false;
        gameOverUiBgmSource.loop = loopGameOverUiBgm;
        gameOverUiBgmSource.spatialBlend = 0f;
    }

    private void PlayGameOverUiBgm()
    {
        if (gameOverUiBgmSource == null || gameOverUiBgmClip == null)
        {
            return;
        }

        gameOverUiBgmSource.loop = loopGameOverUiBgm;
        gameOverUiBgmSource.volume = Mathf.Clamp01(gameOverUiBgmVolume);

        if (gameOverUiBgmSource.clip != gameOverUiBgmClip)
        {
            gameOverUiBgmSource.clip = gameOverUiBgmClip;
        }

        if (!gameOverUiBgmSource.isPlaying)
        {
            gameOverUiBgmSource.Play();
        }
    }

    private void StopGameOverUiBgm()
    {
        if (gameOverUiBgmSource == null)
        {
            return;
        }

        gameOverUiBgmSource.Stop();
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

        uiSfxSource.PlayOneShot(clip, Mathf.Clamp01(buttonClickVolume));
        return clip.length;
    }

    private void PlayGameBgmImmediately(bool forceRestart)
    {
        if (gameBgmClip == null)
        {
            Debug.LogWarning("[EndMenuController] gameBgmClip 未赋值，无法播放背景音乐。", this);
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

    private void RefreshRunDistanceText()
    {
        if (runDistanceText == null)
        {
            return;
        }

        float distance = RunDistanceStore.GetLastDistance();
        runDistanceText.text = string.Format(runDistanceFormat, distance);
    }
}