using System.Collections.Generic;
using DualityWalker.Core;
using UnityEngine;

namespace DualityWalker.World
{
    public class ChunkGenerator : MonoBehaviour
    {
        [SerializeField] private RunnerMotor runnerMotor;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private float spawnAheadDistance = 45f;
        [SerializeField] private float despawnBehindDistance = 25f;
        [SerializeField] private int chunkWidth = 18;
        [SerializeField] private int randomSeed = 2026;

        private readonly Queue<GameObject> activeChunks = new();
        private float lastChunkEndX;
        private Transform chunkRoot;
        private System.Random random;
        private int spawnedChunkCount;

        public void Configure(RunnerMotor motor)
        {
            runnerMotor = motor;
        }

        private void Start()
        {
            if (runnerMotor == null)
            {
                runnerMotor = FindFirstObjectByType<RunnerMotor>();
            }

            random = new System.Random(randomSeed);
            chunkRoot = new GameObject("WorldChunks").transform;
            chunkRoot.SetParent(transform);

            // 从玩家左侧开始铺，避免开局在地形外导致下沉或位移感。
            var startX = runnerMotor != null ? runnerMotor.transform.position.x - 12f : -12f;
            var chunkWorldWidth = chunkWidth * cellSize;
            lastChunkEndX = Mathf.Floor(startX / chunkWorldWidth) * chunkWorldWidth;

            while (lastChunkEndX < (runnerMotor != null ? runnerMotor.transform.position.x + spawnAheadDistance : spawnAheadDistance))
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
            var chunk = new GameObject($"Chunk_{spawnedChunkCount}");
            chunk.transform.SetParent(chunkRoot);
            chunk.transform.position = new Vector3(lastChunkEndX, 0f, 0f);

            var firstTwoChunks = spawnedChunkCount < 2;
            for (var x = 0; x < chunkWidth; x++)
            {
                var safeLane = firstTwoChunks && x < 12;

                var pit = !safeLane && random.NextDouble() < 0.14;
                var obstacle = !safeLane && !pit && random.NextDouble() < 0.20;

                if (!pit)
                {
                    CreateGroundCell(chunk.transform, x, 0, Color.black);
                }
                else
                {
                    CreateZoneCell(chunk.transform, x, 0, ZoneType.Pit, new Color(0f, 0f, 0f, 0.20f));
                }

                if (obstacle)
                {
                    var obstacleHeight = random.Next(1, 3);
                    for (var h = 1; h <= obstacleHeight; h++)
                    {
                        CreateObstacleCell(chunk.transform, x, h, Color.white);
                        CreateZoneCell(chunk.transform, x, h, ZoneType.Obstacle, new Color(1f, 1f, 1f, 0.18f));
                    }
                }
            }

            activeChunks.Enqueue(chunk);
            lastChunkEndX += chunkWidth * cellSize;
            spawnedChunkCount++;
        }

        private void CreateGroundCell(Transform parent, int x, int y, Color color)
        {
            var cell = new GameObject($"Ground_{x}_{y}");
            cell.transform.SetParent(parent);
            cell.transform.localPosition = new Vector3(x * cellSize, y * cellSize - 1f, 0f);

            var renderer = cell.AddComponent<SpriteRenderer>();
            renderer.sprite = WorldVisualFactory.Pixel;
            renderer.color = color;
            renderer.sortingOrder = -10;
            cell.transform.localScale = Vector3.one * cellSize;

            var collider = cell.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
        }

        private void CreateObstacleCell(Transform parent, int x, int y, Color color)
        {
            var cell = new GameObject($"Obstacle_{x}_{y}");
            cell.transform.SetParent(parent);
            cell.transform.localPosition = new Vector3(x * cellSize, y * cellSize - 1f, 0f);
            cell.transform.localScale = Vector3.one * cellSize;

            var renderer = cell.AddComponent<SpriteRenderer>();
            renderer.sprite = WorldVisualFactory.Pixel;
            renderer.color = color;
            renderer.sortingOrder = -5;

            var collider = cell.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            collider.isTrigger = true; // 避免直接卡死在障碍上
        }

        private void CreateZoneCell(Transform parent, int x, int y, ZoneType zoneType, Color color)
        {
            var zone = new GameObject($"Zone_{zoneType}_{x}_{y}");
            zone.transform.SetParent(parent);
            zone.transform.localPosition = new Vector3(x * cellSize, y * cellSize - 1f, 0f);
            zone.transform.localScale = Vector3.one * cellSize;

            var renderer = zone.AddComponent<SpriteRenderer>();
            renderer.sprite = WorldVisualFactory.Pixel;
            renderer.color = color;
            renderer.sortingOrder = -3;

            var cell = zone.AddComponent<ZoneCell>();
            cell.Initialize(zoneType);
        }
    }
}
