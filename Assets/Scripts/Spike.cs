using UnityEngine;

// Auf Spikes legen. Collider2D muss "Is Trigger" an haben.
public class Spike : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null) player.Die();
    }
}
