using UnityEngine;

public sealed class DraggableBlock : MonoBehaviour
{
    private Camera cam;
    private Vector3 grabOffset;
    private Vector3 spawnPos;
    private bool dragging;

    private void Awake()
    {
        cam = Camera.main;
        spawnPos = transform.position;
    }

    private void OnMouseDown()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        dragging = true;
        Vector3 mouse = cam.ScreenToWorldPoint(Input.mousePosition);
        mouse.z = 0f;
        grabOffset = transform.position - mouse;
    }

    private void OnMouseDrag()
    {
        if (!dragging || cam == null) return;

        Vector3 mouse = cam.ScreenToWorldPoint(Input.mousePosition);
        mouse.z = 0f;
        transform.position = mouse + grabOffset;
    }

    private void OnMouseUp()
    {
        dragging = false;

        Collider2D[] hits = Physics2D.OverlapPointAll(transform.position);
        for (int i = 0; i < hits.Length; i++)
        {
            HazardPattern pattern = hits[i].GetComponentInParent<HazardPattern>();
            if (pattern == null) continue;

            pattern.TryFillOneCell();
            Destroy(gameObject);
            return;
        }

        transform.position = spawnPos;
    }
}
