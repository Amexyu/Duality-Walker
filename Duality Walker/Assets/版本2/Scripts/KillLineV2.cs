using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class KillLineV2 : MonoBehaviour
{
    private void Awake()
    {
        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        Debug.Log("GAME OVER: Player out of view.");
        Time.timeScale = 0f;
    }
}