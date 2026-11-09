using System.Collections;
using UnityEngine;

// Tuer
public class Door : MonoBehaviour
{
    [Header("Oeffnen")]
    [Tooltip("Schluessel noetig")]
    public bool openWithKey = true;
    [Tooltip("Schluessel-ID")]
    public string keyId = "Key";

    [Header("Animation")]
    [Tooltip("Nach oben fahren")]
    public bool slideUp = true;
    [Tooltip("0 = Collider-Hoehe")]
    public float slideDistance = 0f;
    public float openTime = 0.6f;

    [Header("Hinweis")]
    [Tooltip("Leer = kein Text")]
    public string lockedMessage = "Locked - you need the key!";
    public float messageTime = 1.5f;

    [Header("Sounds (optional)")]
    public AudioClip openSound;
    public AudioClip lockedSound;
    [Range(0f, 1f)] public float volume = 0.8f;

    bool isOpen;
    float messageUntil;
    GUIStyle style, shadowStyle;

    public bool IsOpen { get { return isOpen; } }

    // ---------- Aufschliessen ----------

    void OnCollisionEnter2D(Collision2D c) { Touch(c.collider); }
    void OnCollisionStay2D(Collision2D c) { Touch(c.collider); }

    void Touch(Collider2D other)
    {
        if (isOpen || !openWithKey || !other.CompareTag("Player")) return;

        if (KeyPickup.Consume(keyId))
        {
            Open();
            return;
        }

        // Kein Schluessel
        if (Time.time >= messageUntil) PlaySound(lockedSound);
        messageUntil = Time.time + messageTime;
    }

    // ---------- Oeffnen ----------

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;
        messageUntil = 0f;
        PlaySound(openSound);

        if (slideUp) StartCoroutine(SlideRoutine());
        else gameObject.SetActive(false);
    }

    IEnumerator SlideRoutine()
    {
        float dist = slideDistance;
        if (dist <= 0f)
        {
            Collider2D col = GetComponent<Collider2D>();
            dist = col != null ? col.bounds.size.y : 2f;
        }

        Vector3 from = transform.position;
        Vector3 to = from + Vector3.up * dist;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, openTime);
            transform.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        gameObject.SetActive(false);
    }

    // ---------- Sound ----------

    void PlaySound(AudioClip clip)
    {
        if (clip == null) return;
        Vector3 pos = Camera.main != null ? Camera.main.transform.position : transform.position;
        AudioSource.PlayClipAtPoint(clip, pos, volume);
    }

    // ---------- Hinweis-Text ----------

    void OnGUI()
    {
        if (isOpen || string.IsNullOrEmpty(lockedMessage) || Time.time >= messageUntil) return;

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            style.normal.textColor = Color.white;
            shadowStyle = new GUIStyle(style);
            shadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
        }

        Matrix4x4 old = GUI.matrix;
        float scale = Screen.height / 720f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float w = Screen.width / scale;
        float h = Screen.height / scale;

        GUI.Label(new Rect(2f, h - 138f, w, 50f), lockedMessage, shadowStyle);
        GUI.Label(new Rect(0f, h - 140f, w, 50f), lockedMessage, style);

        GUI.matrix = old;
    }
}
