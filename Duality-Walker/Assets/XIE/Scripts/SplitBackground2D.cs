using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class SplitBackground2D : MonoBehaviour
{
    [Header("Colors")]
    [SerializeField] private Color topColor = Color.white;
    [SerializeField] private Color bottomColor = Color.black;

    [Header("Depth")]
    [SerializeField] private float zOffsetFromCamera = 10f;

    private Camera cam;
    private Transform topQuad;
    private Transform bottomQuad;
    private Material colorMat;

    private void OnEnable()
    {
        cam = GetComponent<Camera>();
        EnsureSetup();
        UpdateLayout();
        UpdateColors();
    }

    private void OnDisable()
    {
        Cleanup();
    }

    private void LateUpdate()
    {
        if (cam == null) cam = GetComponent<Camera>();
        EnsureSetup();
        UpdateLayout();
        UpdateColors();
    }

    private void EnsureSetup()
    {
        if (colorMat == null)
        {
            Shader shader = Shader.Find("Unlit/Color");
            colorMat = new Material(shader);
            colorMat.hideFlags = HideFlags.HideAndDontSave;
        }

        if (topQuad == null) topQuad = CreateQuad("TopBackground");
        if (bottomQuad == null) bottomQuad = CreateQuad("BottomBackground");

        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
    }

    private Transform CreateQuad(string name)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = name;
        go.transform.SetParent(transform, false);

        var collider = go.GetComponent<Collider>();
        if (collider != null)
        {
            if (Application.isPlaying) Destroy(collider);
            else DestroyImmediate(collider);
        }

        var mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = new Material(colorMat) { hideFlags = HideFlags.HideAndDontSave };
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

        return go.transform;
    }

    private void UpdateLayout()
    {
        if (cam == null || !cam.orthographic || topQuad == null || bottomQuad == null) return;

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        float z = zOffsetFromCamera;

        // 让两个 quad 各占半屏
        float quadWidth = halfWidth * 2f;
        float quadHeight = halfHeight;

        topQuad.localPosition = new Vector3(0f, halfHeight * 0.5f, z);
        topQuad.localScale = new Vector3(quadWidth, quadHeight, 1f);

        bottomQuad.localPosition = new Vector3(0f, -halfHeight * 0.5f, z);
        bottomQuad.localScale = new Vector3(quadWidth, quadHeight, 1f);
    }

    private void UpdateColors()
    {
        if (topQuad != null)
        {
            var mr = topQuad.GetComponent<MeshRenderer>();
            if (mr != null && mr.sharedMaterial != null) mr.sharedMaterial.color = topColor;
        }

        if (bottomQuad != null)
        {
            var mr = bottomQuad.GetComponent<MeshRenderer>();
            if (mr != null && mr.sharedMaterial != null) mr.sharedMaterial.color = bottomColor;
        }
    }

    private void Cleanup()
    {
        if (topQuad != null)
        {
            if (Application.isPlaying) Destroy(topQuad.gameObject);
            else DestroyImmediate(topQuad.gameObject);
        }

        if (bottomQuad != null)
        {
            if (Application.isPlaying) Destroy(bottomQuad.gameObject);
            else DestroyImmediate(bottomQuad.gameObject);
        }

        if (colorMat != null)
        {
            if (Application.isPlaying) Destroy(colorMat);
            else DestroyImmediate(colorMat);
        }
    }
}