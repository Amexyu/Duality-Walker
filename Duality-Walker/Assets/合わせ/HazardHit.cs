using UnityEngine;

public sealed class HazardHit : MonoBehaviour
{
    private EndlessRunManager runManager;
    private float pushLeft;

    public void Setup(EndlessRunManager manager, float push)
    {
        runManager = manager;
        pushLeft = push;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() == null) return;
        runManager?.ApplyPenalty(pushLeft);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.GetComponentInParent<PlayerController>() == null) return;
        runManager?.ApplyPenalty(pushLeft);
    }
}
