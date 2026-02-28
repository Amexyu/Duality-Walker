using UnityEngine;

namespace DualityWalker.UI
{
    public class PlacementHintUI : MonoBehaviour
    {
        [SerializeField] private string normalHint = "左键抓取并拼接方块，右键落位";

        private void Start()
        {
            Debug.Log(normalHint);
        }
    }
}
