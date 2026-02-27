using UnityEngine;

public static class ScoreStore
{
    private const string LastScoreKey = "LastScore";
    private static bool hasCache;
    private static int lastScore;

    public static void Set(int score)
    {
        lastScore = score;
        hasCache = true;
        PlayerPrefs.SetInt(LastScoreKey, score);
        PlayerPrefs.Save();
    }

    public static int Get()
    {
        if (hasCache) return lastScore;

        if (PlayerPrefs.HasKey(LastScoreKey))
        {
            lastScore = PlayerPrefs.GetInt(LastScoreKey);
        }
        else
        {
            lastScore = 0;
        }

        hasCache = true;
        return lastScore;
    }
}