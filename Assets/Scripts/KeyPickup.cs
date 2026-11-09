using System.Collections.Generic;
using UnityEngine;

// Schluessel
// Key-Namenskollision
public class KeyPickup : MonoBehaviour
{
    [Tooltip("Schluessel-ID")]
    public string keyId = "Key";

    [Header("Aussehen")]
    public float bobHeight = 0.15f;
    public float bobSpeed = 3f;
    [Tooltip("Offset zum Spieler")]
    public Vector2 followOffset = new Vector2(-0.7f, 0.9f);
    public float followSpeed = 8f;

    [Header("Sound (optional)")]
    public AudioClip pickupSound;
    [Range(0f, 1f)] public float volume = 0.8f;

    // Getragene Schluessel
    static readonly List<KeyPickup> carried = new List<KeyPickup>();

    bool collected;
    Transform carrier;
    Vector3 startPos;
    float bobPhase;

    // ---------- Zustand ----------

    // Statics zuruecksetzen
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        carried.Clear();
    }

    static void Clean()
    {
        // Zerstoerte entfernen
        carried.RemoveAll(k => k == null);
    }

    static KeyPickup Find(string id)
    {
        foreach (KeyPickup k in carried)
            if (k.keyId == id) return k;
        return null;
    }

    // Schluessel vorhanden?
    public static bool Has(string id)
    {
        Clean();
        return Find(id) != null;
    }

    // Verbrauchen
    public static bool Consume(string id)
    {
        Clean();
        KeyPickup k = Find(id);
        if (k == null) return false;

        carried.Remove(k);
        Destroy(k.gameObject);
        return true;
    }

    // ---------- Unity ----------

    void Awake()
    {
        startPos = transform.position;
        bobPhase = Random.value * Mathf.PI * 2f;
    }

    void Update()
    {
        // Schweben
        if (!collected)
        {
            float y = Mathf.Sin(Time.time * bobSpeed + bobPhase) * bobHeight;
            transform.position = startPos + new Vector3(0f, y, 0f);
            return;
        }

        // Spieler folgen
        if (carrier == null) return;

        int index = Mathf.Max(0, carried.IndexOf(this));
        float side = carrier.localScale.x >= 0f ? 1f : -1f;
        Vector3 target = carrier.position + new Vector3((followOffset.x - 0.5f * index) * side, followOffset.y, 0f);
        float t = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, target, t);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || !other.CompareTag("Player")) return;

        collected = true;
        carrier = other.transform;
        carried.Add(this);

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (pickupSound != null)
        {
            Vector3 pos = Camera.main != null ? Camera.main.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(pickupSound, pos, volume);
        }
    }
}
