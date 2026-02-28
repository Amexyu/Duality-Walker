using System;
using UnityEngine;

namespace World
{
    [Flags]
    public enum ChunkCombinationType
    {
        None = 0,
        Flat = 1 << 0,
        Gap = 1 << 1,
        Stairs = 1 << 2,
        Mixed = 1 << 3
    }

    [CreateAssetMenu(fileName = "ChunkPattern", menuName = "World/Chunk Pattern")]
    public class ChunkPattern : ScriptableObject
    {
        [Header("Mask Textures")]
        [Tooltip("Black pixels define pit fill zones.")]
        [SerializeField] private Texture2D pitMask;

        [Tooltip("White pixels define obstacle cover zones.")]
        [SerializeField] private Texture2D obstacleMask;

        [Header("Chunk Layout")]
        [Min(1)]
        [SerializeField] private int cellsPerChunk = 8;
        [Min(0.1f)]
        [SerializeField] private float cellHeight = 1f;

        [Header("Optional Metadata")]
        [SerializeField] private int difficulty = 1;
        [Min(0)]
        [SerializeField] private int minSpacing = 0;
        [SerializeField] private ChunkCombinationType allowedCombinationTypes = ChunkCombinationType.Flat | ChunkCombinationType.Mixed;

        public int CellsPerChunk => cellsPerChunk;
        public float CellHeight => cellHeight;
        public int Difficulty => difficulty;
        public int MinSpacing => minSpacing;
        public ChunkCombinationType AllowedCombinationTypes => allowedCombinationTypes;

        public bool IsPitCell(int cellIndex)
        {
            return EvaluateMask(pitMask, cellIndex, isPitMask: true);
        }

        public bool IsObstacleCell(int cellIndex)
        {
            return EvaluateMask(obstacleMask, cellIndex, isPitMask: false);
        }

        private bool EvaluateMask(Texture2D mask, int cellIndex, bool isPitMask)
        {
            if (mask == null || cellsPerChunk <= 0)
            {
                return false;
            }

            var normalized = Mathf.Clamp01((cellIndex + 0.5f) / cellsPerChunk);
            var pixelX = Mathf.Clamp(Mathf.FloorToInt(normalized * mask.width), 0, Mathf.Max(0, mask.width - 1));
            var pixelY = mask.height > 0 ? mask.height - 1 : 0;
            var color = mask.GetPixel(pixelX, pixelY);

            if (color.a <= 0.05f)
            {
                return false;
            }

            var luminance = color.grayscale;
            return isPitMask ? luminance <= 0.1f : luminance >= 0.9f;
        }
    }
}
