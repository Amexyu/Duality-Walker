using UnityEngine;

public enum FeatureCellType
{
    None,
    Obstacle, // �����еĺ��ϰ�
    Pit       // �����еİ׿�
}

public class FeatureCell : MonoBehaviour
{
    public FeatureCellType cellType = FeatureCellType.None;
    public bool isBlackBlock = true;
}