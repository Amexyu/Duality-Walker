using UnityEngine;

namespace DualityWalker.World
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class ZoneCell : MonoBehaviour
    {
        [SerializeField] private ZoneType zoneType;
        [SerializeField] private bool resolved;

        public ZoneType ZoneType => zoneType;
        public bool Resolved => resolved;

        public void Initialize(ZoneType type)
        {
            zoneType = type;
            resolved = false;

            var col = GetComponent<BoxCollider2D>();
            col.isTrigger = true;
        }

        public void Resolve()
        {
            resolved = true;
            gameObject.SetActive(false);
        }
    }
}
