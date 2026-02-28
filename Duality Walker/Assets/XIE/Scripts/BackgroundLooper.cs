using UnityEngine;

public class BackgroundLooper : MonoBehaviour
{
    public Transform[] segments;

    float[] _widths;
    Camera _cam;

    void Start()
    {
        _cam = Camera.main;
        _widths = new float[segments.Length];

        for (int i = 0; i < segments.Length; i++)
            _widths[i] = CalcWidth(segments[i]);
    }

    void LateUpdate()
    {
        var gm = GameManager.I;
        if (gm != null && gm.IsGameOver) return;
        if (_cam == null || segments.Length == 0) return;

        float z = Mathf.Abs(_cam.transform.position.z);
        float camLeft = _cam.ViewportToWorldPoint(new Vector3(0f, 0.5f, z)).x;

        float rightMost = float.NegativeInfinity;
        for (int i = 0; i < segments.Length; i++)
        {
            float end = segments[i].position.x + _widths[i] * 0.5f;
            if (end > rightMost) rightMost = end;
        }

        for (int i = 0; i < segments.Length; i++)
        {
            float segRight = segments[i].position.x + _widths[i] * 0.5f;
            if (segRight < camLeft)
            {
                float newX = rightMost + _widths[i] * 0.5f;
                segments[i].position = new Vector3(newX, segments[i].position.y, segments[i].position.z);
                rightMost = newX + _widths[i] * 0.5f;
            }
        }
    }

    float CalcWidth(Transform seg)
    {
        var rends = seg.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return 20f;

        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        return b.size.x;
    }
}