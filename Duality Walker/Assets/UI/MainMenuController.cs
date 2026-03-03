using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;

    [Header("Scene")]
    [SerializeField] private string gameSceneName = "Game";

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
    }

    public void OnClickStart()
    {
        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogWarning("[MainMenuController] gameSceneName Îª¿Õ¡£", this);
            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }

    public void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}