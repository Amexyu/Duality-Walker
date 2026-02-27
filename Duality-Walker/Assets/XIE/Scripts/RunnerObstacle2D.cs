using UnityEngine;

public class RunnerObstacle2D : MonoBehaviour
{
    public enum ObstacleKind
    {
        Block, // °×Çø×èµ²
        Pit    // ºÚÇø¿Ó
    }

    [SerializeField] private ObstacleKind kind;
    [SerializeField] private Vector2Int[] cells;

    public ObstacleKind Kind => kind;
    public Vector2Int[] Cells => cells;

    public void Init(ObstacleKind k, Vector2Int[] shapeCells)
    {
        kind = k;
        cells = shapeCells;
    }

    public string GetSignature()
    {
        return RunnerShapeUtil.MakeSignature(cells);
    }
}