using UnityEngine;

namespace DualityWalker.Blocks
{
    public enum BlockKind
    {
        Black,
        White
    }

    [RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
    public class BlockUnit : MonoBehaviour
    {
        [SerializeField] private BlockKind kind;

        public BlockKind Kind => kind;

        private void Awake()
        {
            RefreshColor();
        }

        public void Initialize(BlockKind blockKind)
        {
            kind = blockKind;
            RefreshColor();
        }

        private void RefreshColor()
        {
            var renderer = GetComponent<SpriteRenderer>();
            renderer.color = kind == BlockKind.Black ? Color.black : Color.white;
        }
    }
}
