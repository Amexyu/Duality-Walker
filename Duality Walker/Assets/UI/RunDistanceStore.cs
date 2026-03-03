using UnityEngine;

public static class RunDistanceStore
{
    private const string LastDistanceKey = "LastRunDistance";
    private static float cachedDistance;

    public static void Reset()
    {
        cachedDistance = 0f;
        PlayerPrefs.SetFloat(LastDistanceKey, 0f);
    }

    public static void SetLastDistance(float distance)
    {
        cachedDistance = Mathf.Max(0f, distance);
        PlayerPrefs.SetFloat(LastDistanceKey, cachedDistance);
    }

    public static float GetLastDistance()
    {
        if (cachedDistance <= 0f)
        {
            cachedDistance = PlayerPrefs.GetFloat(LastDistanceKey, 0f);
        }

        return Mathf.Max(0f, cachedDistance);
    }
}