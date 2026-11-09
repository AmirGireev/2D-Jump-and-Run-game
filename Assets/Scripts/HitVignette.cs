using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Tod-Effekt
public class HitVignette : MonoBehaviour
{
    public Volume volume;

    [Tooltip("Randfarbe")]
    public Color deathColor = new Color(0.9f, 0.1f, 0.1f);

    [Tooltip("Haltezeit")]
    public float holdTime = 0.1f;

    [Tooltip("Ausblendzeit")]
    public float fadeTime = 0.6f;

    Vignette vignette;
    float timer;
    float total;

    void Awake()
    {
        if (volume.profile == null || !volume.profile.TryGet(out vignette))
            Debug.LogWarning("HitVignette: Im Profile fehlt ein Vignette Override.");
        else
        {
            vignette.color.overrideState = true;
            vignette.intensity.overrideState = true;
        }

        volume.weight = 0f;
    }

    void OnEnable()  { PlayerController.OnPlayerDied += Trigger; }
    void OnDisable() { PlayerController.OnPlayerDied -= Trigger; }

    public void Trigger()
    {
        if (vignette != null) vignette.color.value = deathColor;

        total = holdTime + fadeTime;
        timer = total;
        volume.weight = 1f;
    }

    void Update()
    {
        if (timer <= 0f) return;

        // Auch in Pause
        timer -= Time.unscaledDeltaTime;

        float elapsed = total - timer;
        volume.weight = elapsed <= holdTime
            ? 1f
            : Mathf.Clamp01(timer / fadeTime);

        if (timer <= 0f) volume.weight = 0f;
    }
}
