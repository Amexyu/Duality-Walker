using UnityEngine;

namespace DualityWalker.World
{
    public class ScrollingBackdrop : MonoBehaviour
    {
        [SerializeField] private Transform followTarget;
        [SerializeField] private float splitY = 0f;
        [SerializeField] private float width = 120f;
        [SerializeField] private float topHeight = 20f;
        [SerializeField] private float bottomHeight = 30f;

        private Transform topBand;
        private Transform bottomBand;

        public void SetFollowTarget(Transform target)
        {
            followTarget = target;
        }

        private void Start()
        {
            topBand = CreateBand("TopWhite", Color.white, topHeight, splitY + topHeight * 0.5f, -20);
            bottomBand = CreateBand("BottomBlack", Color.black, bottomHeight, splitY - bottomHeight * 0.5f, -20);
            RefreshPosition();
        }

        private void LateUpdate()
        {
            RefreshPosition();
        }

        private void RefreshPosition()
        {
            var centerX = followTarget != null ? followTarget.position.x : transform.position.x;
            if (topBand != null)
            {
                topBand.position = new Vector3(centerX, topBand.position.y, topBand.position.z);
            }

            if (bottomBand != null)
            {
                bottomBand.position = new Vector3(centerX, bottomBand.position.y, bottomBand.position.z);
            }
        }

        private Transform CreateBand(string name, Color color, float height, float centerY, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            go.transform.localScale = new Vector3(width, height, 1f);
            go.transform.position = new Vector3(0f, centerY, 0f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = WorldVisualFactory.Pixel;
            renderer.color = color;
            renderer.sortingOrder = order;
            return go.transform;
        }
    }
}
