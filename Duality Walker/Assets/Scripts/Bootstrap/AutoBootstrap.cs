using UnityEngine;

namespace DualityWalker.Bootstrap
{
    public static class AutoBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrap()
        {
            if (Object.FindFirstObjectByType<RunnerBootstrap>() != null)
            {
                return;
            }

            var go = new GameObject("RunnerBootstrap");
            go.AddComponent<RunnerBootstrap>();
        }
    }
}
