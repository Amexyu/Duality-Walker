using UnityEngine;

public sealed class DragBlockSpawner : MonoBehaviour
{
    [SerializeField] private Transform spawnRoot;
    [SerializeField] private int targetPoolSize = 8;
    [SerializeField] private Vector2 spawnCenter = new Vector2(-6f, -4f);
    [SerializeField] private Vector2 spawnSpread = new Vector2(2.4f, 0.8f);

    private static Sprite solidSprite;

    private void Update()
    {
        if (spawnRoot == null) spawnRoot = transform;

        while (spawnRoot.childCount < targetPoolSize)
        {
            SpawnOne();
        }
    }

    private void SpawnOne()
    {
        GameObject block = new GameObject("DragBlock");
        block.transform.SetParent(spawnRoot, false);
        block.transform.position = new Vector3(
            spawnCenter.x + Random.Range(-spawnSpread.x, spawnSpread.x),
            spawnCenter.y + Random.Range(-spawnSpread.y, spawnSpread.y),
            0f);
        block.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

        SpriteRenderer sr = block.AddComponent<SpriteRenderer>();
        sr.sprite = GetSolidSprite();
        sr.color = new Color(0.35f, 0.95f, 1f, 1f);
        sr.sortingOrder = 20;

        BoxCollider2D col = block.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        block.AddComponent<DraggableBlock>();
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
