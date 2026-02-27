using System.Reflection;
using TMPro;
using UnityEngine;

public class ScoreDisplay : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private PlayerController player;

    [Header("Format")]
    [SerializeField] private string prefix = "Score: ";
    [SerializeField] private bool thousandSeparator = true;

    private FieldInfo scoreField;
    private int lastScore = int.MinValue;

    private void Awake()
    {
        if (player == null)
            player = FindObjectOfType<PlayerController>();

        if (player != null)
        {
            scoreField = typeof(PlayerController).GetField("score", BindingFlags.Instance | BindingFlags.NonPublic);
            if (scoreField == null)
                Debug.LogError("[ScoreDisplay] 找不到 PlayerController 私有字段 'score'。请检查字段名是否变更。");
        }
    }

    private void Start()
    {
        Refresh(true);
    }

    private void Update()
    {
        Refresh(false);
    }

    private void Refresh(bool force)
    {
        if (scoreText == null || player == null || scoreField == null) return;

        object v = scoreField.GetValue(player);
        int s = v is int iv ? iv : 0;

        if (!force && s == lastScore) return;

        lastScore = s;
        ScoreStore.Set(s); // 同步保存“最近分数”
        scoreText.text = prefix + (thousandSeparator ? s.ToString("N0") : s.ToString());
    }
}