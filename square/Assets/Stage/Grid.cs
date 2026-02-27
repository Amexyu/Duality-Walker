using UnityEngine;

public class Grid : MonoBehaviour
{
    public int gridSizeX = 20;
    public int gridSizeY = 15;
    public float cellSize = 1f;
    public Color gridColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
    public Material lineMaterial; 

    private void Start()
    {
        DrawGrid();
    }

    private void DrawGrid()
    {
        for (int x = 0; x <= gridSizeX; x++)
        {
            GameObject lineObj = new GameObject($"VerticalLine_{x}");
            lineObj.transform.parent = transform;
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();

            lr.material = lineMaterial;
            lr.startColor = gridColor;
            lr.endColor = gridColor;
            lr.startWidth = 0.05f;
            lr.endWidth = 0.05f;
            lr.positionCount = 2;

            Vector3 startPos = transform.position + new Vector3(x * cellSize, 0, 0);
            Vector3 endPos = transform.position + new Vector3(x * cellSize, gridSizeY * cellSize, 0);
            lr.SetPosition(0, startPos);
            lr.SetPosition(1, endPos);
        }

        for (int y = 0; y <= gridSizeY; y++)
        {
            GameObject lineObj = new GameObject($"HorizontalLine_{y}");
            lineObj.transform.parent = transform;
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();

            lr.material = lineMaterial;
            lr.startColor = gridColor;
            lr.endColor = gridColor;
            lr.startWidth = 0.05f;
            lr.endWidth = 0.05f;
            lr.positionCount = 2;

            Vector3 startPos = transform.position + new Vector3(0, y * cellSize, 0);
            Vector3 endPos = transform.position + new Vector3(gridSizeX * cellSize, y * cellSize, 0);
            lr.SetPosition(0, startPos);
            lr.SetPosition(1, endPos);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gridColor;
        for (int x = 0; x <= gridSizeX; x++)
        {
            Vector3 startPos = transform.position + new Vector3(x * cellSize, 0, 0);
            Vector3 endPos = transform.position + new Vector3(x * cellSize, gridSizeY * cellSize, 0);
            Gizmos.DrawLine(startPos, endPos);
        }
        for (int y = 0; y <= gridSizeY; y++)
        {
            Vector3 startPos = transform.position + new Vector3(0, y * cellSize, 0);
            Vector3 endPos = transform.position + new Vector3(gridSizeX * cellSize, y * cellSize, 0);
            Gizmos.DrawLine(startPos, endPos);
        }
    }
}