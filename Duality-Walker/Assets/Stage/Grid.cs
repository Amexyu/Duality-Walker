using UnityEngine;

public class Grid : MonoBehaviour
{
    public int gridSizeX = 20;
    public int gridSizeY = 15;
    public float cellSize = 1f;
    public Color gridColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);
    public Material lineMaterial;
    public Color topBackgroundColor = Color.white;
    public Color bottomBackgroundColor = Color.black;
    public float backgroundZ = 1f;

    private static Sprite solidSprite;

    private void Start()
    {
        DrawBackground();
        DrawGrid();
    }

    private void DrawBackground()
    {
        float width = gridSizeX * cellSize;
        float height = gridSizeY * cellSize;
        float halfHeight = height * 0.5f;

        CreateBackgroundHalf(
            name: "BackgroundTop",
            color: topBackgroundColor,
            centerOffset: new Vector2(width * 0.5f, halfHeight + halfHeight * 0.5f),
            size: new Vector2(width, halfHeight));

        CreateBackgroundHalf(
            name: "BackgroundBottom",
            color: bottomBackgroundColor,
            centerOffset: new Vector2(width * 0.5f, halfHeight * 0.5f),
            size: new Vector2(width, halfHeight));
    }

    private void CreateBackgroundHalf(string name, Color color, Vector2 centerOffset, Vector2 size)
    {
        GameObject bgObj = new GameObject(name);
        bgObj.transform.SetParent(transform);
        bgObj.transform.position = transform.position + new Vector3(centerOffset.x, centerOffset.y, backgroundZ);
        bgObj.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer sr = bgObj.AddComponent<SpriteRenderer>();
        sr.sprite = GetSolidSprite();
        sr.color = color;
        sr.sortingOrder = -10;
    }

    private static Sprite GetSolidSprite()
    {
        if (solidSprite != null)
        {
            return solidSprite;
        }

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        solidSprite = Sprite.Create(
            texture,
            new Rect(0, 0, 1, 1),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit: 1f);

        return solidSprite;
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
