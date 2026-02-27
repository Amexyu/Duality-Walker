using UnityEngine;

public sealed class HazardHit : MonoBehaviour
{
    private EndlessRunManager runManager;
    private float penaltyScale;

    public void Setup(EndlessRunManager manager, float scale)
    {
        runManager = manager;
        penaltyScale = scale;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<AutoRunnerAgent>() == null) return;
        runManager?.ApplyPenalty(penaltyScale);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.GetComponentInParent<AutoRunnerAgent>() == null) return;
        runManager?.ApplyPenalty(penaltyScale);
    }
}
