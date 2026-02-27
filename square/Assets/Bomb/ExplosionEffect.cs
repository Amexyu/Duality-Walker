using UnityEngine;

public class ExplosionEffect : MonoBehaviour
{
    [Header("Explosion Animation Settings")]
    public Sprite[] explosionSprites; // 爆破アニメーション用のPNG画像配列
    public float totalAnimationDuration = 1f; // アニメーション全体の再生時間（1秒）
    
    private SpriteRenderer spriteRenderer;
    private float animationTimer;
    private float frameInterval; // 各フレームの表示時間
    private int currentSpriteIndex = 0;
    private bool isAnimating = false;

    void Start()
    {
        InitializeEffect();
    }

    void Update()
    {
        if (isAnimating)
        {
            UpdateAnimation();
        }
    }
    
    private void InitializeEffect()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        
        if (explosionSprites != null && explosionSprites.Length > 0 && explosionSprites[0] != null)
        {
            frameInterval = totalAnimationDuration / explosionSprites.Length;
            
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = explosionSprites[0];
                spriteRenderer.sortingOrder = 10;
                
                isAnimating = true;
                animationTimer = 0f;
                currentSpriteIndex = 0;
            }
            else
            {
                DestroyEffect();
            }
        }
        else
        {
            DestroyEffect();
        }
    }
    
    private void UpdateAnimation()
    {
        animationTimer += Time.deltaTime;
        
        if (animationTimer >= frameInterval)
        {
            currentSpriteIndex++;
            
            if (currentSpriteIndex >= explosionSprites.Length)
            {
                DestroyEffect();
                return;
            }
            
            if (currentSpriteIndex < explosionSprites.Length && explosionSprites[currentSpriteIndex] != null)
            {
                spriteRenderer.sprite = explosionSprites[currentSpriteIndex];
            }
            
            animationTimer = 0f;
        }
    }
    
    private void DestroyEffect()
    {
        Destroy(gameObject);
    }
    
    public static void CreateExplosionAt(Vector3 position, Sprite[] explosionSprites, float animationDuration = 1f)
    {
        if (explosionSprites == null || explosionSprites.Length == 0)
        {
            return;
        }
        
        GameObject explosionObj = new GameObject("ExplosionEffect");
        explosionObj.transform.position = position;
        
        SpriteRenderer sr = explosionObj.AddComponent<SpriteRenderer>();
        
        ExplosionEffect explosion = explosionObj.AddComponent<ExplosionEffect>();
        explosion.explosionSprites = explosionSprites;
        explosion.totalAnimationDuration = animationDuration;
    }
    
    [ContextMenu("手動削除")]
    public void ManualDestroy()
    {
        DestroyEffect();
    }
}