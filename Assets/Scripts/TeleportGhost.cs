using UnityEngine;

// Teleport-Geist
[RequireComponent(typeof(SpriteRenderer))]
public class TeleportGhost : MonoBehaviour
{
    static readonly int ProgressId = Shader.PropertyToID("_Progress");

    SpriteRenderer sr;
    Material mat;
    float duration;
    float elapsed;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void Init(Sprite sprite, bool flipX, Vector3 scale, int sortingLayerID, int sortingOrder, float fadeDuration)
    {
        sr.sprite = sprite;
        sr.flipX = flipX;
        sr.sortingLayerID = sortingLayerID;
        sr.sortingOrder = sortingOrder;
        transform.localScale = scale;

        duration = Mathf.Max(0.01f, fadeDuration);

        // Eigene Materialkopie
        mat = sr.material;
        mat.SetFloat(ProgressId, 0f);
    }

    void Update()
    {
        // Auch in Pause
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / duration);

        if (mat != null) mat.SetFloat(ProgressId, t);

        if (t >= 1f) Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (mat != null) Destroy(mat);
    }
}
