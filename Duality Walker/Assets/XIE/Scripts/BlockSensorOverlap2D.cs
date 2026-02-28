using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class BlockSensorOverlap2D : MonoBehaviour
{
    [Header("只检测障碍层（中文界面：Obstacle Layers）")]
    public LayerMask obstacleLayers;

    [Header("防抖：检测不到后，仍保持阻挡多久（秒）")]
    public float graceTime = 0.08f;

    public bool IsBlocked { get; private set; }

    BoxCollider2D _box;
    ContactFilter2D _filter;
    Collider2D[] _results = new Collider2D[16];
    float _lastHitTime = -999f;

    void Awake()
    {
        _box = GetComponent<BoxCollider2D>();

        _filter = new ContactFilter2D();
        _filter.useLayerMask = true;
        _filter.useTriggers = true; // 障碍如果是Trigger也能检测到
    }

    void FixedUpdate()
    {
        // 每帧同步 layerMask（因为 Inspector 里可能会改）
        _filter.layerMask = obstacleLayers;

        int count = _box.Overlap(_filter, _results);

        bool hit = false;
        for (int i = 0; i < count; i++)
        {
            var c = _results[i];
            if (c == null) continue;

            // 忽略自己（同一个 NPC 根节点下的任何Collider）
            if (c.transform.root == transform.root) continue;

            hit = true;
            break;
        }

        if (hit) _lastHitTime = Time.time;

        IsBlocked = hit || (Time.time - _lastHitTime) < graceTime;
    }
}