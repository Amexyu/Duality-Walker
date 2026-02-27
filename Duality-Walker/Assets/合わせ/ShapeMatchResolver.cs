using UnityEngine;

public sealed class ShapeMatchResolver : MonoBehaviour
{
    [SerializeField] private EndlessRunManager runManager;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private int clearBonusDistance = 15;

    private void Awake()
    {
        if (runManager == null)
        {
            runManager = FindObjectOfType<EndlessRunManager>();
        }

        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
        }
    }

    private void OnEnable()
    {
        PlayerController.SquareCompletedEvent += HandleSquareCompleted;
    }

    private void OnDisable()
    {
        PlayerController.SquareCompletedEvent -= HandleSquareCompleted;
    }

    private void HandleSquareCompleted(int n)
    {
        HazardPattern[] hazards = FindObjectsOfType<HazardPattern>();
        if (hazards.Length == 0) return;

        HazardZone preferredZone = HazardZone.Top;
        if (playerController != null)
        {
            preferredZone = playerController.transform.position.y >= 0f ? HazardZone.Top : HazardZone.Bottom;
        }

        HazardPattern best = null;

        for (int i = 0; i < hazards.Length; i++)
        {
            HazardPattern candidate = hazards[i];
            if (candidate == null || candidate.RequiredShapeSize != n) continue;

            if (candidate.Zone == preferredZone)
            {
                best = candidate;
                break;
            }

            if (best == null)
            {
                best = candidate;
            }
        }

        if (best == null) return;

        Destroy(best.gameObject);
        runManager?.AddDistanceBonus(clearBonusDistance);
    }
}
