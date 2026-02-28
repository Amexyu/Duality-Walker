using DualityWalker.Blocks;
using DualityWalker.Core;
using DualityWalker.GameLoop;
using DualityWalker.Placement;
using DualityWalker.UI;
using DualityWalker.World;
using UnityEngine;

namespace DualityWalker.Bootstrap
{
    public class RunnerBootstrap : MonoBehaviour
    {
        [SerializeField] private bool autoCreateRuntimeObjects = true;

        private void Awake()
        {
            if (!autoCreateRuntimeObjects)
            {
                return;
            }

            var runState = new GameObject("RunStateMachine").AddComponent<RunStateMachine>();
            var _ = new GameObject("GameController").AddComponent<RunnerGameController>();
            _ = new GameObject("DifficultyDirector").AddComponent<DifficultyDirector>();
            var score = new GameObject("ScoreSystem").AddComponent<ScoreSystem>();
            var zoneResolver = new GameObject("ZoneResolver").AddComponent<ZoneResolver>();
            _ = new GameObject("ChunkGenerator").AddComponent<ChunkGenerator>();
            _ = new GameObject("HintUI").AddComponent<PlacementHintUI>();

            var player = CreatePlayer();
            var camera = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera");
            if (camera.GetComponent<Camera>() == null)
            {
                camera.AddComponent<Camera>();
            }

            var follow = camera.GetComponent<CameraFollow2D>() ?? camera.AddComponent<CameraFollow2D>();
            follow.SetTarget(player.transform);

            _ = new GameObject("BlockComposer").AddComponent<BlockComposer>();
            _ = CreateAssembly();
            _ = new GameObject("PlacementController").AddComponent<PlacementController>();

            var scoreListener = new GameObject("ScoreListener").AddComponent<ZoneScoreListener>();
            scoreListener.Initialize(zoneResolver, score);
            runState.SetState(RunState.Running);
        }

        private GameObject CreatePlayer()
        {
            var go = new GameObject("NPC");
            go.transform.position = new Vector3(0f, 2f, 0f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = WorldVisualFactory.Pixel;
            renderer.color = new Color(0.3f, 0.7f, 1f);
            go.transform.localScale = new Vector3(1f, 2f, 1f);

            go.AddComponent<BoxCollider2D>();
            go.AddComponent<Rigidbody2D>();
            go.AddComponent<RunnerMotor>();
            go.AddComponent<HazardDetector>();
            return go;
        }

        private BlockAssembly CreateAssembly()
        {
            var go = new GameObject("BlockAssembly");
            go.transform.position = new Vector3(2f, 4f, 0f);
            var assembly = go.AddComponent<BlockAssembly>();

            var b1 = CreateBlock("Block_Black_1", BlockKind.Black, new Vector3(0, 4, 0));
            var b2 = CreateBlock("Block_Black_2", BlockKind.Black, new Vector3(1, 4, 0));
            var w1 = CreateBlock("Block_White_1", BlockKind.White, new Vector3(2, 4, 0));

            assembly.TryAddBlock(b1, Vector2Int.zero);
            assembly.TryAddBlock(b2, Vector2Int.right);
            assembly.TryAddBlock(w1, new Vector2Int(2, 0));
            return assembly;
        }

        private BlockUnit CreateBlock(string name, BlockKind kind, Vector3 worldPos)
        {
            var go = new GameObject(name);
            go.transform.position = worldPos;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = WorldVisualFactory.Pixel;
            go.transform.localScale = Vector3.one;
            go.AddComponent<BoxCollider2D>();
            var block = go.AddComponent<BlockUnit>();
            block.Initialize(kind);
            return block;
        }
    }
}
