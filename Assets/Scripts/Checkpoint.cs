using UnityEngine;

// Leeres Objekt oder Flagge mit Collider2D (Is Trigger) im Level platzieren.
public class Checkpoint : MonoBehaviour
{
    public Color activeColor = Color.green;

    bool activated;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (activated || !other.CompareTag("Player")) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) return;

        player.SetCheckpoint(transform.position);
        activated = true;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = activeColor;
    }
}
