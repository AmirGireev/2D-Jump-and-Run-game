using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour
{
    [Header("Ziele")]
    public Transform punktA;
    public Transform punktB;

    [Header("Einstellungen")]
    public float geschwindigkeit = 3f;

    Rigidbody2D rb;
    Collider2D col;
    bool nachB = true;
    readonly HashSet<Rigidbody2D> passagiere = new HashSet<Rigidbody2D>();

    // ---------- Unity ----------

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    void OnDisable()
    {
        passagiere.Clear();
    }

    // ---------- Bewegung ----------

    void FixedUpdate()
    {
        if (punktA == null || punktB == null) return;

        Vector2 ziel = nachB ? (Vector2)punktB.position : (Vector2)punktA.position;
        Vector2 alt = rb.position;
        Vector2 neu = Vector2.MoveTowards(alt, ziel, geschwindigkeit * Time.fixedDeltaTime);
        Vector2 delta = neu - alt;

        rb.position = neu;

        // Passagiere
        foreach (Rigidbody2D p in passagiere)
        {
            if (p != null) p.position += delta;
        }

        // Richtungswechsel
        if (Vector2.Distance(neu, ziel) < 0.01f)
            nachB = !nachB;
    }

    // ---------- Passagiere ----------

    void OnCollisionStay2D(Collision2D c)
    {
        if (!c.gameObject.CompareTag("Player") || c.rigidbody == null) return;

        bool oben = c.collider.bounds.min.y >= col.bounds.max.y - 0.1f;
        if (oben) passagiere.Add(c.rigidbody);
        else passagiere.Remove(c.rigidbody);
    }

    void OnCollisionExit2D(Collision2D c)
    {
        if (c.rigidbody != null) passagiere.Remove(c.rigidbody);
    }
}
