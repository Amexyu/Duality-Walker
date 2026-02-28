using UnityEngine;

namespace DualityWalker.World
{
    [CreateAssetMenu(menuName = "DualityWalker/Chunk Pattern", fileName = "ChunkPattern")]
    public class ChunkPattern : ScriptableObject
    {
        [Min(6)] public int width = 16;
        [Min(4)] public int height = 8;
        [Tooltip("黑区：生成坑位，true 表示挖空")]
        public bool[] pitMask;
        [Tooltip("白区：生成障碍，true 表示生成障碍")]
        public bool[] obstacleMask;

        public bool GetPit(int x, int y)
        {
            if (!IsValid(x, y, pitMask)) return false;
            return pitMask[y * width + x];
        }

        public bool GetObstacle(int x, int y)
        {
            if (!IsValid(x, y, obstacleMask)) return false;
            return obstacleMask[y * width + x];
        }

        private bool IsValid(int x, int y, bool[] data)
        {
            return data != null && data.Length == width * height && x >= 0 && x < width && y >= 0 && y < height;
        }
    }
}
