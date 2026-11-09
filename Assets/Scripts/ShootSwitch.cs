using UnityEngine;

// Schiess-Schalter
public class ShootSwitch : MonoBehaviour
{
    [Tooltip("Zu oeffnende Tueren")]
    public Door[] doors;

    [Header("Aussehen")]
    [Tooltip("Sprite nach Treffer")]
    public Sprite activatedSprite;
    [Tooltip("Farbe ohne Sprite")]
    public Color activatedColor = new Color(0.4f, 1f, 0.4f);

    [Header("Sound (optional)")]
    public AudioClip activateSound;
    [Range(0f, 1f)] public float volume = 0.8f;

    bool activated;
    SpriteRenderer sr;

    public bool IsActivated { get { return activated; } }

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    // Vom Projectile
    public void Activate()
    {
        if (activated) return;
        activated = true;

        if (sr != null)
        {
            if (activatedSprite != null) sr.sprite = activatedSprite;
            else sr.color = activatedColor;
        }

        if (activateSound != null)
        {
            Vector3 pos = Camera.main != null ? Camera.main.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(activateSound, pos, volume);
        }

        foreach (Door d in doors)
            if (d != null) d.Open();
    }
}
