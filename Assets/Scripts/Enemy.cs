using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    // Bewegung
    public float speed = 2f;
    public float wallCheckDistance = 0.5f;
    public float edgeCheckOffset = 0.5f;
    public float edgeCheckDepth = 1f;
    public LayerMask groundLayer;

    [Header("Mario-Stomp")]
    public bool canBeStomped = true;
    public float stompBounce = 10f;

    [Header("Schaden")]
    public bool killsPlayerOnContact = true;

    Rigidbody2D rb;
    Collider2D col;
    Vector3 startPos;
    float dir = 1f;

    // ---------- Unity ----------

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    void Start()
    {
        startPos = transform.position;
        PlayerController.OnPlayerDied += ResetEnemy;
    }

    void OnDestroy()
    {
        PlayerController.OnPlayerDied -= ResetEnemy;
    }

    // ---------- Bewegung ----------

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

    // ---------- Reset ----------

    void ResetEnemy()
    {
        transform.position = startPos;
        gameObject.SetActive(true);
        dir = 1f;
    }

    // ---------- Schaden ----------

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag("Player")) return;

        PlayerController player = collision.collider.GetComponent<PlayerController>();
        Rigidbody2D playerRb = collision.collider.attachedRigidbody;
        if (player == null) return;

        // Stomp check
        bool fromAbove = collision.collider.bounds.min.y > col.bounds.center.y;
        bool falling = playerRb != null && playerRb.linearVelocity.y <= 0.1f;

        if (canBeStomped && fromAbove && falling)
        {
            playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, stompBounce);
            gameObject.SetActive(false);
        }
        else if (killsPlayerOnContact)
        {
            player.Die();
        }
    }

    // ---------- Gizmos ----------

    void OnDrawGizmosSelected()
    {
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * dir * wallCheckDistance);
        Vector3 e = transform.position + Vector3.right * dir * edgeCheckOffset;
        Gizmos.DrawLine(e, e + Vector3.down * edgeCheckDepth);
    }
}
