using UnityEngine;

public class Projectile : MonoBehaviour
{
    // Flug
    public float speed = 15f;
    public float maxDistance = 12f;
    public float radius = 0.15f;
    [Tooltip("Wie stark die Schwerkraft den Ball nach unten zieht (Flugbogen). 0 = gerade Bahn. -1 = Wert vom Rigidbody2D im Prefab uebernehmen")]
    public float gravityScale = -1f;

    // Treffer
    public int damage = 1;
    [Tooltip("So lange bleibt der Ball an einer Wand/am Boden haengen (Sekunden). 0 = bis zum Teleport")]
    public float stuckLifetime = 5f;

    [Header("Blocker")]
    [Tooltip("Name des Layers, an dem der Ball NICHT haengen bleibt, sondern verschwindet")]
    public string blockerLayerName = "ProjectileBlocker";

    [Header("Sounds (optional)")]
    public AudioClip shootSound;
    public AudioClip hitSound;
    [Range(0f, 1f)] public float volume = 0.8f;

    Rigidbody2D rb;
    LayerMask groundLayer;

    // Flug
    Vector2 dir = Vector2.right;
    Vector2 velocity;
    float gravityFactor;
    float travelled;

    // Stuck
    bool stuck;
    float stuckTimer;

    // ---------- Setup ----------

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        gravityFactor = gravityScale >= 0f ? gravityScale : (rb != null ? rb.gravityScale : 0f);

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    public void Init(Vector2 direction, LayerMask ground)
    {
        groundLayer = ground;
        dir = direction.normalized;
        velocity = dir * speed;
        AlignToVelocity();
        PlaySound(shootSound);
    }

    // ---------- Flug ----------

    void Update()
    {
        // Stuck
        if (stuck)
        {
            if (stuckLifetime > 0f)
            {
                stuckTimer += Time.deltaTime;
                if (stuckTimer >= stuckLifetime) Destroy(gameObject);
            }
            return;
        }

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        // Im Boden
        if (Physics2D.OverlapCircle(transform.position, radius * 0.8f, groundLayer))
        {
            Destroy(gameObject);
            return;
        }

        // Schwerkraft
        velocity.y += Physics2D.gravity.y * gravityFactor * dt;

        Vector2 move = velocity * dt;
        float step = move.magnitude;
        if (step < 0.0001f) return;
        Vector2 moveDir = move / step;

        // Treffer check
        RaycastHit2D hit = Physics2D.CircleCast(transform.position, radius, moveDir, step, groundLayer);
        if (hit.collider != null)
        {
            Destructible d = hit.collider.GetComponent<Destructible>();
            if (d != null)
            {
                d.TakeHit(damage);
                PlaySound(hitSound);
                Destroy(gameObject);
                return;
            }

            if (IsBlocker(hit.collider.gameObject))
            {
                PlaySound(hitSound);
                Destroy(gameObject);
                return;
            }

            // Haengenbleiben
            Vector2 safePos = hit.point + hit.normal * 0.5f;
            StickAt(safePos);
            return;
        }

        transform.position += (Vector3)move;
        AlignToVelocity();

        travelled += step;
        if (travelled >= maxDistance) Destroy(gameObject);
    }

    void AlignToVelocity()
    {
        if (velocity.sqrMagnitude < 0.0001f) return;
        float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    // ---------- Treffer ----------

    void OnTriggerEnter2D(Collider2D other)
    {
        if (stuck) return;
        if (other.CompareTag("Player")) return;

        Destructible d = other.GetComponent<Destructible>();
        if (d != null)
        {
            d.TakeHit(damage);
            PlaySound(hitSound);
            Destroy(gameObject);
            return;
        }

        if (IsBlocker(other.gameObject))
        {
            PlaySound(hitSound);
            Destroy(gameObject);
            return;
        }

        // Boden Wand
        if (!other.isTrigger && ((1 << other.gameObject.layer) & groundLayer.value) != 0)
        {
            StickAt(transform.position);
            return;
        }

        Destroy(gameObject);
    }

    void StickAt(Vector2 position)
    {
        stuck = true;
        stuckTimer = 0f;
        PlaySound(hitSound);
        velocity = Vector2.zero;
        transform.position = new Vector3(position.x, position.y, transform.position.z);

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.position = position;
        }
    }

    bool IsBlocker(GameObject go)
    {
        int layer = LayerMask.NameToLayer(blockerLayerName);
        return layer >= 0 && go.layer == layer;
    }

    // ---------- Sound ----------

    void PlaySound(AudioClip clip)
    {
        if (clip == null) return;
        Vector3 pos = Camera.main != null ? Camera.main.transform.position : transform.position;
        AudioSource.PlayClipAtPoint(clip, pos, volume);
    }
}
