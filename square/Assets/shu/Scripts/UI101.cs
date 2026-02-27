using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UI101 : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button restartButton; // 新增：可选的 RESTART 按钮

    [Header("Scene")]
    [Tooltip("要加载的目标场景名（或路径）。为空则重载当前场景。")]
    [SerializeField] private string sceneToLoad = "";

    [Header("Click SFX")]
    [Tooltip("按钮点击音效")]
    [SerializeField] private AudioClip clickClip;
    [SerializeField, Range(0f, 1f)] private float clickVolume = 1.0f;
    [SerializeField, Range(0.5f, 2f)] private float clickPitch = 1.0f;
    [Tooltip("如设置，优先使用该 AudioSource 播放；否则自动创建一次性播放器")]
    [SerializeField] private AudioSource audioSource;

    private AsyncOperation loadOp;

    private void Awake()
    {
        if (startButton != null)
            startButton.onClick.AddListener(OnStartClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);

        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);
    }

    private void OnDestroy()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(OnStartClicked);

        if (quitButton != null)
            quitButton.onClick.RemoveListener(OnQuitClicked);

        if (restartButton != null)
            restartButton.onClick.RemoveListener(OnRestartClicked);
    }

    public void OnStartClicked()
    {
        // 防重入
        if (loadOp != null) return;

        // 播放点击音效
        PlayClickSfx();

        // 恢复时间缩放，确保游戏正常运行
        Time.timeScale = 1f;

        // 禁用按钮，避免重复点击
        if (startButton != null) startButton.interactable = false;
        if (quitButton != null) quitButton.interactable = false;
        if (restartButton != null) restartButton.interactable = false;

        // 加载指定场景
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            int buildIndex = FindSceneBuildIndex(sceneToLoad);
            if (buildIndex >= 0)
            {
                loadOp = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single);
                if (loadOp != null) { loadOp.allowSceneActivation = true; return; }
                SceneManager.LoadScene(buildIndex, LoadSceneMode.Single);
                return;
            }
            else
            {
                Debug.LogError($"Scene '{sceneToLoad}' 未在 Build Settings 中。请在 File > Build Settings 添加或检查名称/路径。");
                ReenableButtons();
                return;
            }
        }

        // 未配置目标场景时，重载当前场景
        var currentIndex = SceneManager.GetActiveScene().buildIndex;
        if (currentIndex >= 0 && currentIndex < SceneManager.sceneCountInBuildSettings)
        {
            loadOp = SceneManager.LoadSceneAsync(currentIndex, LoadSceneMode.Single);
            if (loadOp != null) loadOp.allowSceneActivation = true;
            else SceneManager.LoadScene(currentIndex, LoadSceneMode.Single);
        }
        else
        {
            Debug.LogWarning("当前场景不在 Build Settings 中，无法重载。");
            ReenableButtons();
        }
    }

    public void OnRestartClicked()
    {
        // 防重入
        if (loadOp != null) return;

        // 播放点击音效
        PlayClickSfx();

        // 恢复时间缩放，确保游戏正常运行
        Time.timeScale = 1f;

        // 禁用按钮，避免重复点击
        if (startButton != null) startButton.interactable = false;
        if (quitButton != null) quitButton.interactable = false;
        if (restartButton != null) restartButton.interactable = false;

        // 默认重载当前场景；如希望重启到特定场景，可在 Inspector 设置 sceneToLoad
        int buildIndex = -1;
        if (!string.IsNullOrEmpty(sceneToLoad))
            buildIndex = FindSceneBuildIndex(sceneToLoad);
        else
            buildIndex = SceneManager.GetActiveScene().buildIndex;

        if (buildIndex >= 0 && buildIndex < SceneManager.sceneCountInBuildSettings)
        {
            loadOp = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single);
            if (loadOp != null) loadOp.allowSceneActivation = true;
            else SceneManager.LoadScene(buildIndex, LoadSceneMode.Single);
        }
        else
        {
            Debug.LogError("Restart 目标场景不在 Build Settings 中。请检查 sceneToLoad 或当前场景是否已加入 Build Settings。");
            ReenableButtons();
        }
    }

    public void OnQuitClicked()
    {
        // 播放点击音效
        PlayClickSfx();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private int FindSceneBuildIndex(string nameOrPath)
    {
        // 支持场景名或路径（不区分大小写）
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (string.IsNullOrEmpty(path)) continue;

            int slash = path.LastIndexOf('/') + 1;
            int dot = path.LastIndexOf('.');
            string fileName = (slash >= 0 && dot > slash) ? path.Substring(slash, dot - slash) : path;

            if (string.Equals(fileName, nameOrPath, System.StringComparison.OrdinalIgnoreCase))
                return i;
            if (string.Equals(path, nameOrPath, System.StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    private void PlayClickSfx()
    {
        if (clickClip == null) return;

        // 优先使用指定 AudioSource（受混音/音量总控）
        if (audioSource != null)
        {
            float originalPitch = audioSource.pitch;
            audioSource.pitch = clickPitch;
            audioSource.PlayOneShot(clickClip, clickVolume);
            audioSource.pitch = originalPitch;
            return;
        }

        // 无 AudioSource 时，创建一次性播放器（跨场景播放后自动销毁）
        GameObject temp = new GameObject("UI_ClickSFX");
        DontDestroyOnLoad(temp);
        var src = temp.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f; // 2D
        src.rolloffMode = AudioRolloffMode.Linear;
        src.volume = clickVolume;
        src.pitch = clickPitch;
        src.clip = clickClip;
        src.loop = false;
        src.Play();

        Object.Destroy(temp, clickClip.length / Mathf.Max(0.01f, clickPitch));
    }

    private void ReenableButtons()
    {
        if (startButton != null) startButton.interactable = true;
        if (quitButton != null) quitButton.interactable = true;
        if (restartButton != null) restartButton.interactable = true;
    }
}
