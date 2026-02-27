using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class CameraGradientBackground2D : MonoBehaviour
{
    [Header("Colors")]
    [SerializeField] private Color topColor = Color.white;
    [SerializeField] private Color bottomColor = Color.black;

    [Header("Render")]
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = -1000;

    [Header("Boundary")]
    [SerializeField] private bool followBoundaryGround = true;
    [SerializeField] private BoundaryGround2D boundaryGround;
    [SerializeField] private float seamOverlap = 0f; // 建议先 0，确认绝对对齐后再微调

    private Camera cam;
    private GameObject topObj;
    private GameObject bottomObj;
    private SpriteRenderer topRenderer;
    private SpriteRenderer bottomRenderer;

    private Texture2D whiteTexture;
    private Sprite whiteSprite;

    private void OnEnable()
    {
        EnsureSetup();
        FitToCamera();
    }

    private void OnValidate()
    {
        EnsureSetup();
        FitToCamera();
    }

    private void LateUpdate()
    {
        FitToCamera();
    }

    private void OnDisable()
    {
        CleanupGeneratedAssets();
    }

    private void EnsureSetup()
    {
        if (cam == null) cam = GetComponent<Camera>();

        EnsureWhiteSprite();

        if (topObj == null)
        {
            Transform t = transform.Find("BackgroundTop");
            if (t != null) topObj = t.gameObject;
        }

        if (bottomObj == null)
        {
            Transform t = transform.Find("BackgroundBottom");
            if (t != null) bottomObj = t.gameObject;
        }

        if (topObj == null)
        {
            topObj = new GameObject("BackgroundTop");
            topObj.transform.SetParent(transform, false);
        }

        if (bottomObj == null)
        {
            bottomObj = new GameObject("BackgroundBottom");
            bottomObj.transform.SetParent(transform, false);
        }

        if (topRenderer == null)
        {
            topRenderer = topObj.GetComponent<SpriteRenderer>();
            if (topRenderer == null) topRenderer = topObj.AddComponent<SpriteRenderer>();
        }

        if (bottomRenderer == null)
        {
            bottomRenderer = bottomObj.GetComponent<SpriteRenderer>();
            if (bottomRenderer == null) bottomRenderer = bottomObj.AddComponent<SpriteRenderer>();
        }

        topRenderer.sprite = whiteSprite;
        bottomRenderer.sprite = whiteSprite;

        topRenderer.color = topColor;
        bottomRenderer.color = bottomColor;

        topRenderer.sortingLayerName = sortingLayerName;
        bottomRenderer.sortingLayerName = sortingLayerName;

        // 固定绘制顺序：上白层在上
        bottomRenderer.sortingOrder = sortingOrder;
        topRenderer.sortingOrder = sortingOrder + 1;
    }

    private void EnsureWhiteSprite()
    {
        if (whiteSprite != null) return;

        if (whiteTexture == null)
        {
            whiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            whiteTexture.wrapMode = TextureWrapMode.Clamp;
            whiteTexture.filterMode = FilterMode.Point;
            whiteTexture.SetPixel(0, 0, Color.white);
            whiteTexture.Apply();
        }

        whiteSprite = Sprite.Create(
            whiteTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f
        );
    }

    private void FitToCamera()
    {
        if (cam == null || topRenderer == null || bottomRenderer == null) return;

        topRenderer.color = topColor;
        bottomRenderer.color = bottomColor;

        float z = -cam.transform.position.z;
        Vector3 vmin = cam.ViewportToWorldPoint(new Vector3(0f, 0f, z));
        Vector3 vmax = cam.ViewportToWorldPoint(new Vector3(1f, 1f, z));

        float minX = vmin.x;
        float maxX = vmax.x;
        float minY = vmin.y;
        float maxY = vmax.y;

        float splitY = cam.transform.position.y;
        if (followBoundaryGround)
        {
            if (boundaryGround == null) boundaryGround = cam.GetComponent<BoundaryGround2D>();
            if (boundaryGround != null) splitY = boundaryGround.GetGroundTopY();
        }

        splitY = Mathf.Clamp(splitY, minY, maxY);

        float width = Mathf.Max(0.01f, maxX - minX);
        float topHeight = Mathf.Max(0.01f, (maxY - splitY) + seamOverlap);
        float bottomHeight = Mathf.Max(0.01f, (splitY - minY) + seamOverlap);

        float topCenterY = splitY + (maxY - splitY) * 0.5f;
        float bottomCenterY = minY + (splitY - minY) * 0.5f;

        topObj.transform.position = new Vector3((minX + maxX) * 0.5f, topCenterY, 0f);
        bottomObj.transform.position = new Vector3((minX + maxX) * 0.5f, bottomCenterY, 0f);

        topObj.transform.localScale = new Vector3(width, topHeight, 1f);
        bottomObj.transform.localScale = new Vector3(width, bottomHeight, 1f);
    }

    private void CleanupGeneratedAssets()
    {
        if (whiteSprite != null)
        {
            if (Application.isPlaying) Destroy(whiteSprite);
            else DestroyImmediate(whiteSprite);
            whiteSprite = null;
        }

        if (whiteTexture != null)
        {
            if (Application.isPlaying) Destroy(whiteTexture);
            else DestroyImmediate(whiteTexture);
            whiteTexture = null;
        }
    }
}