using System.Collections.Generic;
using UnityEngine;

namespace DualityWalker.World
{
    public class ScrollingBackdrop : MonoBehaviour
    {
        [SerializeField] private Transform followTarget;
        [SerializeField] private float splitY = 0f;
        [SerializeField] private float segmentWidth = 30f;
        [SerializeField] private float topHeight = 20f;
        [SerializeField] private float bottomHeight = 30f;

        private readonly List<Transform> topSegments = new();
        private readonly List<Transform> bottomSegments = new();

        public void SetFollowTarget(Transform target)
        {
            followTarget = target;
        }

        private void Start()
        {
            // 三段循环，形成无限滚动视觉
            for (var i = -1; i <= 1; i++)
            {
                topSegments.Add(CreateBand($"TopWhite_{i}", Color.white, topHeight, splitY + topHeight * 0.5f, i * segmentWidth, -30));
                bottomSegments.Add(CreateBand($"BottomBlack_{i}", Color.black, bottomHeight, splitY - bottomHeight * 0.5f, i * segmentWidth, -30));
            }
        }

        private void LateUpdate()
        {
            var centerX = followTarget != null ? followTarget.position.x : transform.position.x;
            RecycleSegments(topSegments, centerX);
            RecycleSegments(bottomSegments, centerX);
        }

        private void RecycleSegments(List<Transform> segments, float centerX)
        {
            for (var i = 0; i < segments.Count; i++)
            {
                var seg = segments[i];
                var dx = seg.position.x - centerX;
                if (dx > segmentWidth)
                {
                    seg.position = new Vector3(seg.position.x - segmentWidth * 3f, seg.position.y, seg.position.z);
                }
                else if (dx < -segmentWidth)
                {
                    seg.position = new Vector3(seg.position.x + segmentWidth * 3f, seg.position.y, seg.position.z);
                }
            }
        }

        private Transform CreateBand(string name, Color color, float height, float centerY, float centerX, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            go.transform.localScale = new Vector3(segmentWidth, height, 1f);
            go.transform.position = new Vector3(centerX, centerY, 0f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = WorldVisualFactory.Pixel;
            renderer.color = color;
            renderer.sortingOrder = order;
            return go.transform;
        }
    }
}
