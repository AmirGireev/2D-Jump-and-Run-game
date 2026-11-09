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

    [Header("Einschlag-Effekt")]
    [Tooltip("Partikel-Prefab (Particle System), das beim Treffer erscheint")]
    public GameObject impactVfx;
    [Tooltip("Nach wie vielen Sekunden der Effekt wieder geloescht wird")]
    public float impactVfxLifetime = 1f;

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
        Collider2D inside = Physics2D.OverlapCircle(transform.position, radius * 0.8f, groundLayer);
        if (inside != null)
        {
            if (!HitSwitch(inside)) Destroy(gameObject);
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
            if (HitSwitch(hit.collider)) return;

            Destructible d = hit.collider.GetComponent<Destructible>();
            if (d != null)
            {
                d.TakeHit(damage);
                OnHit(transform.position);
                Destroy(gameObject);
                return;
            }

            if (IsBlocker(hit.collider.gameObject))
            {
                OnHit(transform.position);
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

        if (HitSwitch(other)) return;

        Destructible d = other.GetComponent<Destructible>();
        if (d != null)
        {
            d.TakeHit(damage);
            OnHit(transform.position);
            Destroy(gameObject);
            return;
        }

        if (IsBlocker(other.gameObject))
        {
            OnHit(transform.position);
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
        velocity = Vector2.zero;
        OnHit(position);
        transform.position = new Vector3(position.x, position.y, transform.position.z);

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.position = position;
        }
    }

    // Schalter getroffen? Dann aktivieren und Ball entfernen
    bool HitSwitch(Collider2D c)
    {
        ShootSwitch sw = c != null ? c.GetComponentInParent<ShootSwitch>() : null;
        if (sw == null) return false;

        sw.Activate();
        OnHit(transform.position);
        Destroy(gameObject);
        return true;
    }

    bool IsBlocker(GameObject go)
    {
        int layer = LayerMask.NameToLayer(blockerLayerName);
        return layer >= 0 && go.layer == layer;
    }

    // ---------- Treffer-Effekt ----------

    void OnHit(Vector3 pos)
    {
        PlaySound(hitSound);

        if (impactVfx != null)
        {
            GameObject fx = Instantiate(impactVfx, pos, Quaternion.identity);
            Destroy(fx, impactVfxLifetime);
        }
    }

    // ---------- Sound ----------

    void PlaySound(AudioClip clip)
    {
        if (clip == null) return;
        Vector3 pos = Camera.main != null ? Camera.main.transform.position : transform.position;
        AudioSource.PlayClipAtPoint(clip, pos, volume);
    }
}
