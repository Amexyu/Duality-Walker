using UnityEngine;

public class SplitBackground : MonoBehaviour
{
    [SerializeField] private Color topColor = Color.white;
    [SerializeField] private Color bottomColor = Color.black;
    [SerializeField] private int sortingOrder = -100;
    [SerializeField] private float depthFromCamera = 5f;

    private static Sprite solidSprite;

    private void Start()
    {
        BuildBackground();
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
