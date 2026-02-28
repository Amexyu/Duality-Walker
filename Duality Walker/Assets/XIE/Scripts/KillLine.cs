using UnityEngine;

public class KillLine : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.I?.GameOver();
        }
    }
}