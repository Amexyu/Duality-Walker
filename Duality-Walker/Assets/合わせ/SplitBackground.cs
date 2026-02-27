using UnityEngine;

public class SplitBackground : MonoBehaviour
{
    [SerializeField] private Color topColor = Color.white;
    [SerializeField] private Color bottomColor = Color.black;
    [SerializeField] private int sortingOrder = -100;
    [SerializeField] private float depthFromCamera = 5f;

    [Header("Subtle Motion")]
    [SerializeField] private float seamPulseAmplitude = 0.06f;
    [SerializeField] private float seamPulseSpeed = 1.8f;

    private static Sprite solidSprite;
    private Transform seamLine;
    private float seamBaseX;

    private void Start()
    {
        BuildBackground();
    }

    private void Update()
    {
        if (seamLine == null) return;

        Vector3 p = seamLine.localPosition;
        p.x = seamBaseX + Mathf.Sin(Time.time * seamPulseSpeed) * seamPulseAmplitude;
        seamLine.localPosition = p;
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

        CreateHalf("TopBackground", topColor, new Vector3(0f, halfHeight * 0.5f, depthFromCamera), new Vector3(fullWidth, halfHeight, 1f));
        CreateHalf("BottomBackground", bottomColor, new Vector3(0f, -halfHeight * 0.5f, depthFromCamera), new Vector3(fullWidth, halfHeight, 1f));
        CreateSeam(fullWidth);
    }

    private void CreateSeam(float fullWidth)
    {
        Transform existing = transform.Find("ZoneSeam");
        if (existing != null)
        {
            Destroy(existing.gameObject);
        }

        GameObject seam = new GameObject("ZoneSeam");
        seam.transform.SetParent(transform, false);
        seam.transform.localPosition = new Vector3(0f, 0f, depthFromCamera - 0.1f);
        seam.transform.localScale = new Vector3(fullWidth, 0.06f, 1f);

        SpriteRenderer sr = seam.AddComponent<SpriteRenderer>();
        sr.sprite = GetSolidSprite();
        sr.color = new Color(0.8f, 0.25f, 0.25f, 0.3f);
        sr.sortingOrder = sortingOrder + 1;

        seamLine = seam.transform;
        seamBaseX = seamLine.localPosition.x;
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
