using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BLACKMOVE : MonoBehaviour
{
    [Header("移动")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float maxFallSpeed = 20f;

    [Header("相机")]
    [SerializeField] private float lookAheadX = 4f;
    [SerializeField] private float cameraSmoothTimeX = 0.12f;
    [SerializeField] private float cameraSmoothTimeY = 0.12f;
    [SerializeField] private bool lockCameraY = true;
    [SerializeField] private bool followDownWhenFalling = true;
    [SerializeField] private float followDownOffsetY = 1.2f; // 玩家在屏幕中稍偏下
    [SerializeField] private float followDownTriggerY = -0.2f;

    [Header("玩家显示层级")]
    [SerializeField] private int playerSortingOrder = 50;

    private Rigidbody2D rb;
    private Camera mainCamera;

    private float camVelX;
    private float camVelY;
    private float baseCamY;
    private float baseCamZ;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        PhysicsMaterial2D mat = new PhysicsMaterial2D("PlayerNoFriction");
        mat.friction = 0f;
        mat.bounciness = 0f;

        Collider2D[] cols = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < cols.Length; i++)
        {
            cols[i].sharedMaterial = mat;
        }

        ApplyPlayerSortingOrder();
    }

    private void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        baseCamY = mainCamera.transform.position.y;
        baseCamZ = mainCamera.transform.position.z;
    }

    private void FixedUpdate()
    {
        Vector2 v = rb.linearVelocity;
        v.x = moveSpeed;
        v.y = Mathf.Max(v.y, -maxFallSpeed);
        rb.linearVelocity = v;
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
        {
            return;
        }

        Vector3 camPos = mainCamera.transform.position;

        float targetX = transform.position.x + lookAheadX;
        camPos.x = Mathf.SmoothDamp(camPos.x, targetX, ref camVelX, cameraSmoothTimeX);

        float targetY = baseCamY;
        if (lockCameraY && followDownWhenFalling)
        {
            if (transform.position.y < baseCamY + followDownTriggerY)
            {
                targetY = Mathf.Min(baseCamY, transform.position.y + followDownOffsetY);
            }
        }
        else if (!lockCameraY)
        {
            targetY = transform.position.y;
        }

        camPos.y = Mathf.SmoothDamp(camPos.y, targetY, ref camVelY, cameraSmoothTimeY);
        camPos.z = baseCamZ;

        mainCamera.transform.position = camPos;
    }

    private void ApplyPlayerSortingOrder()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingOrder = playerSortingOrder;
        }
    }
}
