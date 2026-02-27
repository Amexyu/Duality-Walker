using UnityEngine;

public sealed class DragBlockSpawner : MonoBehaviour
{
    [SerializeField] private Transform spawnRoot;
    [SerializeField] private int targetPoolSize = 8;
    [SerializeField] private Vector2 viewportCenter = new Vector2(0.12f, 0.15f);
    [SerializeField] private Vector2 viewportSpread = new Vector2(0.1f, 0.06f);

    private static Sprite solidSprite;
    private Camera cam;

    private void Awake()
    {
        cam = Camera.main;
        EnsureSpawnRoot();
    }

    private void Update()
    {
        EnsureSpawnRoot();

        while (spawnRoot.childCount < targetPoolSize)
        {
            SpawnOne();
        }
    }

    private void EnsureSpawnRoot()
    {
        if (spawnRoot != null) return;

        GameObject root = GameObject.Find("DragBlocksRoot");
        if (root == null)
        {
            root = new GameObject("DragBlocksRoot");
        }

        spawnRoot = root.transform;
    }

    private void SpawnOne()
    {
        if (cam == null) cam = Camera.main;

        Vector3 basePos = GetSpawnBasePos();
        GameObject block = new GameObject("DragBlock");
        block.transform.SetParent(spawnRoot, false);
        block.transform.position = new Vector3(
            basePos.x + Random.Range(-viewportSpread.x, viewportSpread.x) * 10f,
            basePos.y + Random.Range(-viewportSpread.y, viewportSpread.y) * 10f,
            0f);
        block.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

        SpriteRenderer sr = block.AddComponent<SpriteRenderer>();
        sr.sprite = GetSolidSprite();
        sr.color = new Color(0.35f, 0.95f, 1f, 1f);
        sr.sortingOrder = 30;

        BoxCollider2D col = block.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        block.AddComponent<DraggableBlock>();
    }

    private Vector3 GetSpawnBasePos()
    {
        if (cam == null) return new Vector3(-6f, -4f, 0f);

        float z = -cam.transform.position.z;
        Vector3 p = cam.ViewportToWorldPoint(new Vector3(viewportCenter.x, viewportCenter.y, z));
        p.z = 0f;
        return p;
    }

    private static Sprite GetSolidSprite()
    {
        if (solidSprite != null) return solidSprite;

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
