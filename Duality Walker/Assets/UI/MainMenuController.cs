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

    private bool isStarting;
    private Coroutine startCoroutine;

    private void Awake()
    {
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
        if (isStarting)
        {
            return;
        }

        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogWarning("[MainMenuController] gameSceneName Îª¿Õ¡£", this);
            return;
        }

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
        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogWarning("[MainMenuController] gameSceneName Îª¿Õ¡£", this);
            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }

    public void OnClickBack()
    {
        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogWarning("[MainMenuController] mainMenuSceneName Îª¿Õ¡£", this);
            return;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void LoadGameScene()
    {
        SceneManager.LoadScene(gameSceneName);
    }
}