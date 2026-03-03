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
        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogWarning("[EndMenuController] gameSceneName Îª¿Õ¡£", this);
            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }

    public void OnClickBack()
    {
        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogWarning("[EndMenuController] mainMenuSceneName Îª¿Õ¡£", this);
            return;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }
}