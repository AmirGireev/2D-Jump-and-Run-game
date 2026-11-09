using UnityEngine;

// Parallax-Effekt
public class ParallaxLayer : MonoBehaviour
{
    [Tooltip("Folgestaerke")]
    public Vector2 parallax = new Vector2(0.3f, 0f);

    [Tooltip("Horizontal wiederholen")]
    public bool repeatX = true;

    Transform cam;
    Vector3 startPos;
    float tileWidth;

    void Start()
    {
        cam = Camera.main != null ? Camera.main.transform : null;
        startPos = transform.position;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
            tileWidth = sr.sprite.bounds.size.x * transform.lossyScale.x;
    }

    void LateUpdate()
    {
        if (cam == null) return;

        Vector3 c = cam.position;

        // Position setzen
        transform.position = new Vector3(
            startPos.x + c.x * parallax.x,
            startPos.y + c.y * parallax.y,
            transform.position.z);

        // Kachel verschieben
        if (repeatX && tileWidth > 0f)
        {
            float camOffset = c.x * (1f - parallax.x);
            if (camOffset > startPos.x + tileWidth) startPos.x += tileWidth;
            else if (camOffset < startPos.x - tileWidth) startPos.x -= tileWidth;
        }
    }
}
