using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndMenuController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button backButton;

    [Header("Scene Names")]
    [SerializeField] private string gameSceneName = "Game";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool isLoading;

    private void Awake()
    {
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnClickRestart);
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnClickBack);
        }
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
    }

    public void OnClickRestart()
    {
        LoadSceneSafe(gameSceneName, "gameSceneName");
    }

    public void OnClickBack()
    {
        LoadSceneSafe(mainMenuSceneName, "mainMenuSceneName");
    }

    private void LoadSceneSafe(string sceneName, string fieldName)
    {
        if (isLoading)
        {
            return;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[EndMenuController] " + fieldName + " Îª¿Õ¡£", this);
            return;
        }

        isLoading = true;

        Time.timeScale = 1f;
        AudioListener.pause = false;

        SceneManager.LoadScene(sceneName);
    }
}