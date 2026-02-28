using System.Collections.Generic;
using UnityEngine;

namespace World
{
    public class ChunkGenerator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform npcTransform;
        [SerializeField] private Transform worldRoot;
        [SerializeField] private ChunkPattern defaultPattern;
        [SerializeField] private List<ChunkPattern> availablePatterns = new();

        [Header("Generation")]
        [Min(1f)]
        [SerializeField] private float chunkWidth = 8f;
        [SerializeField] private int seed = 12345;
        [Min(1f)]
        [SerializeField] private float spawnAheadDistance = 30f;
        [Min(1f)]
        [SerializeField] private float despawnBehindDistance = 20f;

        [Header("Layout")]
        [SerializeField] private float groundY = 0f;
        [SerializeField] private Vector2 obstacleHeightRange = new(1f, 3f);
        [SerializeField] private string pitZoneTag = "PitZone";
        [SerializeField] private string obstacleZoneTag = "ObstacleZone";
        [SerializeField] private string pitZoneLayer = "PitZone";
        [SerializeField] private string obstacleZoneLayer = "ObstacleZone";

        private readonly Dictionary<int, ChunkInstance> activeChunks = new();
        private readonly Queue<ChunkInstance> pooledChunks = new();
        private System.Random random;

        private void Awake()
        {
            random = new System.Random(seed);
            if (worldRoot == null)
            {
                worldRoot = transform;
            }
        }

        private void Start()
        {
            RefreshChunks(GetTrackedX());
        }

        private void Update()
        {
            RefreshChunks(GetTrackedX());
        }

        private float GetTrackedX()
        {
            if (npcTransform != null)
            {
                return npcTransform.position.x;
            }

            if (Camera.main != null)
            {
                return Camera.main.transform.position.x;
            }

            return transform.position.x;
        }

        private void RefreshChunks(float trackedX)
        {
            var minChunkIndex = Mathf.FloorToInt((trackedX - despawnBehindDistance) / chunkWidth);
            var maxChunkIndex = Mathf.FloorToInt((trackedX + spawnAheadDistance) / chunkWidth);

            for (var index = minChunkIndex; index <= maxChunkIndex; index++)
            {
                if (!activeChunks.ContainsKey(index))
                {
                    SpawnChunk(index);
                }
            }

            var recycle = ListPool<int>.Get();
            foreach (var pair in activeChunks)
            {
                var chunkStartX = pair.Key * chunkWidth;
                if (chunkStartX + chunkWidth < trackedX - despawnBehindDistance)
                {
                    recycle.Add(pair.Key);
                }
            }

            for (var i = 0; i < recycle.Count; i++)
            {
                DespawnChunk(recycle[i]);
            }

            ListPool<int>.Release(recycle);
        }

        private void SpawnChunk(int chunkIndex)
        {
            var chunk = GetChunkFromPool();
            chunk.Index = chunkIndex;
            chunk.Pattern = SelectPattern(chunkIndex);

            chunk.Root.name = $"Chunk_{chunkIndex}";
            chunk.Root.SetParent(worldRoot, false);
            chunk.Root.position = new Vector3(chunkIndex * chunkWidth, 0f, 0f);
            chunk.Root.gameObject.SetActive(true);

            BuildChunk(chunk);
            activeChunks.Add(chunkIndex, chunk);
        }

        private void DespawnChunk(int chunkIndex)
        {
            if (!activeChunks.TryGetValue(chunkIndex, out var chunk))
            {
                return;
            }

            chunk.Root.gameObject.SetActive(false);
            chunk.Clear();
            pooledChunks.Enqueue(chunk);
            activeChunks.Remove(chunkIndex);
        }

        private ChunkInstance GetChunkFromPool()
        {
            if (pooledChunks.Count > 0)
            {
                return pooledChunks.Dequeue();
            }

            var root = new GameObject("Chunk").transform;
            return new ChunkInstance(root);
        }

        private ChunkPattern SelectPattern(int chunkIndex)
        {
            if (availablePatterns.Count == 0)
            {
                return defaultPattern;
            }

            var idx = Mathf.Abs(seed + chunkIndex) % availablePatterns.Count;
            return availablePatterns[idx] != null ? availablePatterns[idx] : defaultPattern;
        }

        private void BuildChunk(ChunkInstance chunk)
        {
            var pattern = chunk.Pattern;
            var cellCount = pattern != null ? Mathf.Max(1, pattern.CellsPerChunk) : 8;
            var cellWidth = chunkWidth / cellCount;
            var pitLayer = LayerMask.NameToLayer(pitZoneLayer);
            var obstacleLayer = LayerMask.NameToLayer(obstacleZoneLayer);

            for (var cell = 0; cell < cellCount; cell++)
            {
                var xCenter = (cell + 0.5f) * cellWidth;
                CreateGroundTile(chunk.Root, cell, xCenter, cellWidth);

                if (pattern != null && pattern.IsPitCell(cell))
                {
                    CreateZoneMarker(chunk.Root, $"PitZone_{cell}", pitZoneTag, pitLayer, new Vector3(xCenter, groundY - pattern.CellHeight * 0.5f, 0f), new Vector2(cellWidth, pattern.CellHeight));
                }

                if (pattern != null && pattern.IsObstacleCell(cell))
                {
                    var y = Mathf.Lerp(obstacleHeightRange.x, obstacleHeightRange.y, (float)random.NextDouble());
                    CreateZoneMarker(chunk.Root, $"ObstacleZone_{cell}", obstacleZoneTag, obstacleLayer, new Vector3(xCenter, y, 0f), new Vector2(cellWidth * 0.75f, pattern.CellHeight));
                }
            }
        }

        private void CreateGroundTile(Transform parent, int cell, float centerX, float width)
        {
            var tile = new GameObject($"GroundTile_{cell}");
            tile.transform.SetParent(parent, false);
            tile.transform.localPosition = new Vector3(centerX, groundY, 0f);

            var collider = tile.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(width, 1f);
        }

        private void CreateZoneMarker(Transform parent, string markerName, string zoneTag, int layer, Vector3 localPosition, Vector2 size)
        {
            var marker = new GameObject(markerName);
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = localPosition;

            if (!string.IsNullOrWhiteSpace(zoneTag) && IsTagDefined(zoneTag))
            {
                marker.tag = zoneTag;
            }

            if (layer >= 0)
            {
                marker.layer = layer;
            }

            var collider = marker.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = size;
        }

        private static bool IsTagDefined(string tag)
        {
            try
            {
                _ = GameObject.FindWithTag(tag);
                return true;
            }
            catch (UnityException)
            {
                return false;
            }
        }

        private sealed class ChunkInstance
        {
            public ChunkInstance(Transform root)
            {
                Root = root;
            }

            public Transform Root { get; }
            public int Index { get; set; }
            public ChunkPattern Pattern { get; set; }

            public void Clear()
            {
                for (var i = Root.childCount - 1; i >= 0; i--)
                {
                    Destroy(Root.GetChild(i).gameObject);
                }
            }
        }

        private static class ListPool<T>
        {
            private static readonly Stack<List<T>> Pool = new();

            public static List<T> Get()
            {
                return Pool.Count > 0 ? Pool.Pop() : new List<T>();
            }

            public static void Release(List<T> list)
            {
                list.Clear();
                Pool.Push(list);
            }
        }
    }
}
