using System.Collections.Generic;
using DualityWalker.Core;
using UnityEngine;

namespace DualityWalker.World
{
    public class ChunkGenerator : MonoBehaviour
    {
        [SerializeField] private RunnerMotor runnerMotor;
        [SerializeField] private List<ChunkPattern> patterns = new();
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private int groundHeight = 2;
        [SerializeField] private float spawnAheadDistance = 40f;
        [SerializeField] private float despawnBehindDistance = 20f;
        [SerializeField] private int chunkWidth = 16;
        [SerializeField] private int randomSeed = 2026;

        private readonly Queue<GameObject> activeChunks = new();
        private float lastChunkEndX;
        private Transform chunkRoot;
        private System.Random random;

        private void Start()
        {
            if (runnerMotor == null)
            {
                runnerMotor = FindFirstObjectByType<RunnerMotor>();
            }

            random = new System.Random(randomSeed);
            chunkRoot = new GameObject("WorldChunks").transform;
            chunkRoot.SetParent(transform);

            while (lastChunkEndX < spawnAheadDistance)
            {
                SpawnChunk();
            }
        }

        private void Update()
        {
            if (runnerMotor == null)
            {
                return;
            }

            var playerX = runnerMotor.transform.position.x;
            while (lastChunkEndX < playerX + spawnAheadDistance)
            {
                SpawnChunk();
            }

            while (activeChunks.Count > 0)
            {
                var chunk = activeChunks.Peek();
                if (chunk.transform.position.x + chunkWidth * cellSize < playerX - despawnBehindDistance)
                {
                    activeChunks.Dequeue();
                    Destroy(chunk);
                }
                else
                {
                    break;
                }
            }
        }

        private void SpawnChunk()
        {
            var chunk = new GameObject($"Chunk_{activeChunks.Count}");
            chunk.transform.SetParent(chunkRoot);
            chunk.transform.position = new Vector3(lastChunkEndX, 0f, 0f);

            var pattern = patterns.Count > 0 ? patterns[random.Next(patterns.Count)] : null;
            var width = pattern != null ? pattern.width : chunkWidth;

            for (var x = 0; x < width; x++)
            {
                var hasPit = pattern != null && pattern.GetPit(x, groundHeight);
                if (!hasPit)
                {
                    CreateGroundCell(chunk.transform, x, 0, Color.black);
                }

                if (pattern != null)
                {
                    for (var y = 0; y < pattern.height; y++)
                    {
                        if (pattern.GetPit(x, y))
                        {
                            CreateZoneCell(chunk.transform, x, y, ZoneType.Pit, new Color(0f, 0f, 0f, 0.25f));
                        }
                        if (pattern.GetObstacle(x, y))
                        {
                            CreateObstacleCell(chunk.transform, x, y, Color.white);
                            CreateZoneCell(chunk.transform, x, y, ZoneType.Obstacle, new Color(1f, 1f, 1f, 0.25f));
                        }
                    }
                }
                else if (random.NextDouble() < 0.12)
                {
                    CreateZoneCell(chunk.transform, x, 0, ZoneType.Pit, new Color(0f, 0f, 0f, 0.25f));
                }
                else if (random.NextDouble() < 0.15)
                {
                    CreateObstacleCell(chunk.transform, x, 1, Color.white);
                    CreateZoneCell(chunk.transform, x, 1, ZoneType.Obstacle, new Color(1f, 1f, 1f, 0.25f));
                }
            }

            activeChunks.Enqueue(chunk);
            lastChunkEndX += width * cellSize;
        }

        private void CreateGroundCell(Transform parent, int x, int y, Color color)
        {
            var cell = new GameObject($"Ground_{x}_{y}");
            cell.transform.SetParent(parent);
            cell.transform.localPosition = new Vector3(x * cellSize, y * cellSize - 1f, 0f);

            var renderer = cell.AddComponent<SpriteRenderer>();
            renderer.sprite = WorldVisualFactory.Pixel;
            renderer.color = color;
            cell.transform.localScale = Vector3.one * cellSize;

            var collider = cell.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
        }

        private void CreateObstacleCell(Transform parent, int x, int y, Color color)
        {
            var cell = new GameObject($"Obstacle_{x}_{y}");
            cell.transform.SetParent(parent);
            cell.transform.localPosition = new Vector3(x * cellSize, y * cellSize, 0f);
            cell.transform.localScale = Vector3.one * cellSize;

            var renderer = cell.AddComponent<SpriteRenderer>();
            renderer.sprite = WorldVisualFactory.Pixel;
            renderer.color = color;

            var collider = cell.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
        }

        private void CreateZoneCell(Transform parent, int x, int y, ZoneType zoneType, Color color)
        {
            var zone = new GameObject($"Zone_{zoneType}_{x}_{y}");
            zone.transform.SetParent(parent);
            zone.transform.localPosition = new Vector3(x * cellSize, y * cellSize, 0f);
            zone.transform.localScale = Vector3.one * cellSize;

            var renderer = zone.AddComponent<SpriteRenderer>();
            renderer.sprite = WorldVisualFactory.Pixel;
            renderer.color = color;
            renderer.sortingOrder = -1;

            var cell = zone.AddComponent<ZoneCell>();
            cell.Initialize(zoneType);
        }
    }
}
