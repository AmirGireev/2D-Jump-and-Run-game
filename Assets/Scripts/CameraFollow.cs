using UnityEngine;

// Auf die Main Camera legen. Keine Cinemachine noetig.
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [Header("Ziel")]
    public Transform target;                    // leer lassen = sucht automatisch den Spieler (Tag "Player")
    public Vector3 offset = new Vector3(0f, 1.5f, -10f);

    [Header("Gefuehl")]
    public float smoothTime = 0.15f;            // kleiner = straffer, groesser = schwammiger
    public float lookAheadDistance = 2f;        // Kamera schaut in Blickrichtung voraus
    public float lookAheadSmooth = 3f;

    [Header("Level-Grenzen (optional)")]
    public bool useBounds = false;
    public Vector2 minBounds = new Vector2(-10f, -5f);   // linke untere Ecke des Levels
    public Vector2 maxBounds = new Vector2(100f, 20f);   // rechte obere Ecke des Levels

    Camera cam;
    Vector3 velocity;
    float currentLookAhead;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        if (target == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) target = p.transform;
        }

        // Kamera startet direkt beim Spieler, ohne rueberzuruckeln
        if (target != null) transform.position = target.position + offset;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Blickrichtung des Spielers (PlayerController spiegelt localScale.x)
        float targetLook = Mathf.Sign(target.localScale.x) * lookAheadDistance;
        currentLookAhead = Mathf.Lerp(currentLookAhead, targetLook, lookAheadSmooth * Time.deltaTime);

        Vector3 desired = target.position + offset + new Vector3(currentLookAhead, 0f, 0f);

        if (useBounds)
        {
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            desired.x = Mathf.Clamp(desired.x, minBounds.x + halfW, maxBounds.x - halfW);
            desired.y = Mathf.Clamp(desired.y, minBounds.y + halfH, maxBounds.y - halfH);
        }

        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }

    void OnDrawGizmosSelected()
    {
        if (!useBounds) return;
        Vector3 center = (minBounds + maxBounds) / 2f;
        Vector3 size = maxBounds - minBounds;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, size);
    }
}
