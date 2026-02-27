using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class BoundaryGround2D : MonoBehaviour
{
    [Header("Ground Line")]
    [SerializeField] private float boundaryOffsetY = 0f;
    [SerializeField] private float thickness = 0.4f;
    [SerializeField] private float extraWidth = 2f;

    [Header("Align")]
    [SerializeField] private bool alignTopToBoundaryLine = true;
    [SerializeField] private bool compensateContactOffset = true;

    private Camera cam;
    private GameObject groundObject;
    private BoxCollider2D groundCollider;
    private Rigidbody2D groundRb;

    private void OnEnable()
    {
        EnsureSetup();
        FitGroundToCamera();
    }

    private void OnValidate()
    {
        EnsureSetup();
        FitGroundToCamera();
    }

    private void LateUpdate()
    {
        FitGroundToCamera();
    }

    private void EnsureSetup()
    {
        if (cam == null) cam = GetComponent<Camera>();

        if (groundObject == null)
        {
            Transform child = transform.Find("BoundaryGround");
            if (child != null) groundObject = child.gameObject;
        }

        if (groundObject == null)
        {
            groundObject = new GameObject("BoundaryGround");
            groundObject.transform.SetParent(transform, false);
        }

        if (groundCollider == null)
        {
            groundCollider = groundObject.GetComponent<BoxCollider2D>();
            if (groundCollider == null) groundCollider = groundObject.AddComponent<BoxCollider2D>();
        }

        if (groundRb == null)
        {
            groundRb = groundObject.GetComponent<Rigidbody2D>();
            if (groundRb == null) groundRb = groundObject.AddComponent<Rigidbody2D>();
        }

        groundCollider.isTrigger = false;
        groundRb.bodyType = RigidbodyType2D.Static;
        groundRb.simulated = true;
    }

    private void FitGroundToCamera()
    {
        if (cam == null || groundObject == null || groundCollider == null) return;

        float z = -cam.transform.position.z;
        Vector3 vmin = cam.ViewportToWorldPoint(new Vector3(0f, 0f, z));
        Vector3 vmax = cam.ViewportToWorldPoint(new Vector3(1f, 1f, z));

        float t = Mathf.Max(0.02f, thickness);
        float width = Mathf.Max(0.1f, (vmax.x - vmin.x) + extraWidth);
        float boundaryY = cam.transform.position.y + boundaryOffsetY;

        float centerY = boundaryY;
        if (alignTopToBoundaryLine)
        {
            float contact = compensateContactOffset ? Physics2D.defaultContactOffset : 0f;
            centerY = boundaryY - t * 0.5f + contact;
        }

        groundObject.transform.position = new Vector3(cam.transform.position.x, centerY, 0f);
        groundCollider.size = new Vector2(width, t);
        groundCollider.offset = Vector2.zero;
    }

    public float GetGroundTopY()
    {
        if (groundObject == null || groundCollider == null)
            return transform.position.y + boundaryOffsetY;

        return groundObject.transform.position.y + groundCollider.offset.y + groundCollider.size.y * 0.5f;
    }

    public BoxCollider2D GetGroundCollider()
    {
        return groundCollider;
    }
}