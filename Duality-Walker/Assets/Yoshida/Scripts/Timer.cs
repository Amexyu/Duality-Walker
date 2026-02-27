using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class Timer : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI TimerText;
    [SerializeField] private float limitTime = 5f;

    [Header("Game Over")]
    [SerializeField] private string gameOverScene = "GameOver"; // 在 Inspector 设置“游戏结束”场景名（需在 Build Settings 中）
    [SerializeField] private bool useAsyncLoad = true;          // 可选：是否异步加载

    private bool triggered;

    // 单例引用（便于全局加减时间）
    private static Timer s_instance;

    private void OnEnable()
    {
        s_instance = this;
        // 刷一次 UI，确保场景开始显示正确
        if (TimerText != null) TimerText.text = Mathf.Max(0f, limitTime).ToString("F0");
    }

    private void OnDisable()
    {
        if (s_instance == this) s_instance = null;
    }

    // 供其他脚本调用：加/减时间（正加负减）
    public static void AddTime(float seconds)
    {
        if (s_instance == null)
        {
            s_instance = FindObjectOfType<Timer>();
            if (s_instance == null) return; // 场景无计时器则忽略
        }
        s_instance.AddTimeInstance(seconds);
    }

    private void AddTimeInstance(float seconds)
    {
        if (triggered) return;

        limitTime = Mathf.Max(0f, limitTime + seconds);

        if (TimerText != null)
            TimerText.text = limitTime.ToString("F0");

        if (limitTime <= 0f)
            TriggerGameOver();
    }

    // Update is called once per frame
    void Update()
    {
        if (triggered) return;

        limitTime -= Time.deltaTime;

        if (limitTime <= 0f)
        {
            limitTime = 0f;
            TriggerGameOver();
        }

        if (TimerText != null)
            TimerText.text = limitTime.ToString("F0");
    }

    private void TriggerGameOver()
    {
        if (triggered) return;
        triggered = true;

        if (string.IsNullOrEmpty(gameOverScene))
        {
            Debug.LogError("[Timer] 未设置 gameOverScene。请在 Inspector 设置并确保在 Build Settings 中。");
            return;
        }

        if (useAsyncLoad)
        {
            var op = SceneManager.LoadSceneAsync(gameOverScene);
            if (op != null) op.allowSceneActivation = true;
        }
        else
        {
            SceneManager.LoadScene(gameOverScene);
        }
    }
}
