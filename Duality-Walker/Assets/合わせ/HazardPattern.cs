using UnityEngine;

public enum HazardZone
{
    Top,
    Bottom
}

public sealed class HazardPattern : MonoBehaviour
{
    [SerializeField] private HazardZone zone;
    [SerializeField] private int requiredShapeSize = 2;

    public HazardZone Zone => zone;
    public int RequiredShapeSize => requiredShapeSize;

    public void Setup(HazardZone hazardZone, int shapeSize)
    {
        zone = hazardZone;
        requiredShapeSize = Mathf.Max(1, shapeSize);
    }
}
