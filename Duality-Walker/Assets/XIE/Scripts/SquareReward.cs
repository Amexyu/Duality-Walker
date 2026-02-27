using UnityEngine;

public class SquareReward : MonoBehaviour
{
    [SerializeField] private Transform groupRoot; // ÍÏ GroupRoot
    [SerializeField] private float minScale = 0.12f;
    [SerializeField] private int score = 0;

    public void OnSquareCompleted(int addScore, int n)
    {
        score += addScore;
        Debug.Log($"[Square] n={n}, +{addScore}, score={score}");

        if (groupRoot == null) return;

        float factor = (n <= 1) ? 1f : (n - 1f) / n; // ËõÐ¡Ò»È¦
        float cur = groupRoot.localScale.x;
        float next = Mathf.Max(minScale, cur * factor);
        groupRoot.localScale = new Vector3(next, next, 1f);
    }
}
