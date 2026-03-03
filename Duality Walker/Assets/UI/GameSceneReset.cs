using UnityEngine;

public class GameSceneReset : MonoBehaviour
{
    private void Awake()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }
}