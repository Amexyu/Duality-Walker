using UnityEngine;

public enum FeatureCellType
{
    None,
    Obstacle, // 白区中的黑障碍
    Pit       // 黑区中的白坑
}

public class FeatureCell : MonoBehaviour
{
    public FeatureCellType cellType = FeatureCellType.None;
}