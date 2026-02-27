using TMPro;
using UnityEngine;

public class GameOverScoreDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private string prefix = "Score: ";
    [SerializeField] private bool thousandSeparator = true;

    private void Start()
    {
        if (scoreText == null) return;
        int s = ScoreStore.Get();
        scoreText.text = prefix + (thousandSeparator ? s.ToString("N0") : s.ToString());
    }
}