using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Coin : MonoBehaviour
{
    public int value = 1;

    [Header("Animation")]
    public float bobHeight = 0.15f;
    public float bobSpeed = 3f;
    public float spinSpeed = 3f;

    [Header("Beim Einsammeln")]
    public AudioClip pickupSound;
    [Range(0f, 1f)] public float volume = 0.8f;
    public GameObject pickupEffect;

    Vector3 startPos;
    Vector3 startScale;
    float phase;
    bool collected;

    // ---------- Unity ----------

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void Start()
    {
        startPos = transform.position;
        startScale = transform.localScale;
        phase = Random.value * 10f;
    }

    // ---------- Animation ----------

    void Update()
    {
        float t = Time.time + phase;

        // Wippen
        if (bobHeight > 0f)
            transform.position = startPos + Vector3.up * Mathf.Sin(t * bobSpeed) * bobHeight;

        // Drehen
        if (spinSpeed > 0f)
        {
            float s = Mathf.Max(0.1f, Mathf.Abs(Mathf.Cos(t * spinSpeed)));
            transform.localScale = new Vector3(startScale.x * s, startScale.y, startScale.z);
        }
    }

    // ---------- Einsammeln ----------

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || !other.CompareTag("Player")) return;
        collected = true;

        if (LevelTimer.Instance != null)
            LevelTimer.Instance.AddCoins(value);

        // Sound
        if (pickupSound != null)
        {
            Vector3 pos = Camera.main != null ? Camera.main.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(pickupSound, pos, volume);
        }

        // Effekt
        if (pickupEffect != null)
            Instantiate(pickupEffect, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
