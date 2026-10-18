using UnityEngine;

// Laufender Gegner. Braucht: Rigidbody2D (Freeze Rotation Z), Collider2D (kein Trigger).
// Fuer "Schuss toetet Gegner" zusaetzlich das Script Destructible auf das gleiche Objekt legen.
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    public float speed = 2f;
    public float wallCheckDistance = 0.5f;
    public float edgeCheckOffset = 0.5f;   // wie weit vorne der Kanten-Check sitzt
    public float edgeCheckDepth = 1f;
    public LayerMask groundLayer;

    [Header("Mario-Stomp")]
    public bool canBeStomped = true;
    public float stompBounce = 10f;

    Rigidbody2D rb;
    Collider2D col;
    float dir = 1f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    void Update()
    {
        bool wallAhead = Physics2D.Raycast(transform.position, Vector2.right * dir, wallCheckDistance, groundLayer);

        Vector2 edgeOrigin = (Vector2)transform.position + Vector2.right * dir * edgeCheckOffset;
        bool groundAhead = Physics2D.Raycast(edgeOrigin, Vector2.down, edgeCheckDepth, groundLayer);

        if (wallAhead || !groundAhead) Flip();

        rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);
    }

    void Flip()
    {
        dir = -dir;
        Vector3 s = transform.localScale;
        transform.localScale = new Vector3(Mathf.Abs(s.x) * dir, s.y, s.z);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag("Player")) return;

        PlayerController player = collision.collider.GetComponent<PlayerController>();
        Rigidbody2D playerRb = collision.collider.attachedRigidbody;
        if (player == null) return;

        // Spieler landet von oben auf dem Gegner
        bool fromAbove = collision.collider.bounds.min.y > col.bounds.center.y;
        bool falling = playerRb != null && playerRb.linearVelocity.y <= 0.1f;

        if (canBeStomped && fromAbove && falling)
        {
            playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, stompBounce);
            Destroy(gameObject);
        }
        else
        {
            player.Die();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * dir * wallCheckDistance);
        Vector3 e = transform.position + Vector3.right * dir * edgeCheckOffset;
        Gizmos.DrawLine(e, e + Vector3.down * edgeCheckDepth);
    }
}
