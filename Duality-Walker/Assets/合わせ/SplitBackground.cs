using System.Collections.Generic;
using UnityEngine;

public class SplitBackground : MonoBehaviour
{
    [SerializeField] private Color topColor = Color.white;
    [SerializeField] private Color bottomColor = Color.black;
    [SerializeField] private int sortingOrder = -100;
    [SerializeField] private float depthFromCamera = 5f;

    [Header("Scroll Marks")]
    [SerializeField] private int marksPerZone = 12;
    [SerializeField] private float markWidth = 0.12f;
    [SerializeField] private float markSpeed = 1.6f;

    private static Sprite solidSprite;
    private readonly List<Transform> marks = new List<Transform>();
    private float halfWidth;

    private void Start()
    {
        BuildBackground();
    }

    private void Update()
    {
        if (marks.Count == 0) return;

        float loopLeft = -halfWidth - 1f;
        float loopRight = halfWidth + 1f;

        for (int i = 0; i < marks.Count; i++)
        {
            Transform t = marks[i];
            if (t == null) continue;

            Vector3 p = t.localPosition;
            p.x -= markSpeed * Time.deltaTime;
            if (p.x < loopLeft)
            {
                p.x = loopRight;
            }

            t.localPosition = p;
        }
    }

    private void BuildBackground()
    {
        Camera cam = GetComponent<Camera>();
        if (cam == null || !cam.orthographic)
        {
            return;
        }

        float fullHeight = cam.orthographicSize * 2f;
        float fullWidth = fullHeight * cam.aspect;
        float halfHeight = fullHeight * 0.5f;
        halfWidth = fullWidth * 0.5f;

        CreateHalf("TopBackground", topColor, new Vector3(0f, halfHeight * 0.5f, depthFromCamera), new Vector3(fullWidth, halfHeight, 1f));
        CreateHalf("BottomBackground", bottomColor, new Vector3(0f, -halfHeight * 0.5f, depthFromCamera), new Vector3(fullWidth, halfHeight, 1f));

        CreateMarks("TopMarks", new Color(0f, 0f, 0f, 0.12f), halfHeight * 0.5f, halfHeight, fullWidth);
        CreateMarks("BottomMarks", new Color(1f, 1f, 1f, 0.12f), -halfHeight * 0.5f, halfHeight, fullWidth);
    }

    private void CreateMarks(string rootName, Color color, float centerY, float zoneHeight, float zoneWidth)
    {
        Transform old = transform.Find(rootName);
        if (old != null)
        {
            Destroy(old.gameObject);
        }

        GameObject root = new GameObject(rootName);
        root.transform.SetParent(transform, false);

        for (int i = 0; i < Mathf.Max(1, marksPerZone); i++)
        {
            float x = Mathf.Lerp(-zoneWidth * 0.5f, zoneWidth * 0.5f, i / (float)Mathf.Max(1, marksPerZone - 1));
            GameObject mark = new GameObject($"Mark_{i}");
            mark.transform.SetParent(root.transform, false);
            mark.transform.localPosition = new Vector3(x, centerY, depthFromCamera - 0.1f);
            mark.transform.localScale = new Vector3(markWidth, zoneHeight, 1f);

            SpriteRenderer sr = mark.AddComponent<SpriteRenderer>();
            sr.sprite = GetSolidSprite();
            sr.color = color;
            sr.sortingOrder = sortingOrder + 1;

            marks.Add(mark.transform);
        }
    }

    private void CreateHalf(string name, Color color, Vector3 localPosition, Vector3 localScale)
    {
        Transform existing = transform.Find(name);
        if (existing != null)
        {
            Destroy(existing.gameObject);
        }

        GameObject background = new GameObject(name);
        background.transform.SetParent(transform, false);
        background.transform.localPosition = localPosition;
        background.transform.localScale = localScale;

        SpriteRenderer sr = background.AddComponent<SpriteRenderer>();
        sr.sprite = GetSolidSprite();
        sr.color = color;
        sr.sortingOrder = sortingOrder;
    }

    private static Sprite GetSolidSprite()
    {
        if (solidSprite != null)
        {
            return solidSprite;
        }

        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        tex.SetPixel(0, 0, Color.white);
        tex.Apply();

        solidSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return solidSprite;
    }
}
