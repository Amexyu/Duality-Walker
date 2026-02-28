using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }

    [Header("Speed")]
    public float baseSpeed = 4f;
    public float accelPerSecond = 0.35f;
    public float maxSpeed = 14f;

    public float CurrentSpeed { get; private set; }
    public float Distance { get; private set; }
    public bool IsGameOver { get; private set; }

    float _t;

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
    }

    void Update()
    {
        if (IsGameOver) return;

        _t += Time.deltaTime;
        CurrentSpeed = Mathf.Min(maxSpeed, baseSpeed + _t * accelPerSecond);
        Distance += CurrentSpeed * Time.deltaTime;
    }

    public void GameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;

        Debug.Log($"GAME OVER  Distance={Distance:0}  Speed={CurrentSpeed:0.0}");
        Time.timeScale = 0f;
    }

    // 临时显示（不用做UI）
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 300, 30), $"Speed: {CurrentSpeed:0.0}");
        GUI.Label(new Rect(10, 30, 300, 30), $"Distance: {Distance:0}");
    }
}