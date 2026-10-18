using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 15f;
    public float maxDistance = 12f;
    public float radius = 0.15f;
    public int damage = 1;

    LayerMask groundLayer;
    Vector2 dir = Vector2.right;
    float travelled;

    public void Init(Vector2 direction, LayerMask ground)
    {
        groundLayer = ground;
        dir = direction.normalized;
        
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void Update()
    {
        // Notbremse: Steckt der Ball im Boden -> sofort löschen
        if (Physics2D.OverlapCircle(transform.position, radius * 0.8f, groundLayer))
        {
            Destroy(gameObject);
            return;
        }

        float step = speed * Time.deltaTime;

        // Pruefen, ob auf dem naechsten Stueck Weg etwas Festes liegt
        RaycastHit2D hit = Physics2D.CircleCast(transform.position, radius, dir, step, groundLayer);
        if (hit.collider != null)
        {
            Destructible d = hit.collider.GetComponent<Destructible>();
            if (d != null)
            {
                d.TakeHit(damage);
            }
            
            // Egal was getroffen wurde: Projektil sofort zerstören
            Destroy(gameObject);
            return;
        }

        transform.Translate(dir * step, Space.World);
        travelled += step;
        if (travelled >= maxDistance) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) return;

        Destructible d = other.GetComponent<Destructible>();
        if (d != null)
        {
            d.TakeHit(damage);
        }
        Destroy(gameObject);
    }
}