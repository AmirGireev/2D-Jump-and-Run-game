using UnityEngine;
using UnityEngine.InputSystem;

// Globale Musik
[RequireComponent(typeof(AudioSource))]
public class MusicPlayer : MonoBehaviour
{
    public static MusicPlayer Instance { get; private set; }

    [Header("Musik")]
    public AudioClip music;
    [Range(0f, 1f)] public float volume = 0.35f;
    [Tooltip("Lautstaerke in Pause")]
    [Range(0f, 1f)] public float pausedVolumeFactor = 0.5f;
    public float fadeSpeed = 3f;

    [Header("Stummschalten")]
    public Key muteKey = Key.M;

    AudioSource src;
    bool muted;
    float current;

    const string MuteKeyPref = "musicMuted";

    // ---------- Unity ----------

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        src = GetComponent<AudioSource>();
        src.clip = music;
        src.loop = true;
        src.playOnAwake = false;
        src.spatialBlend = 0f;   // 2D-Sound
        src.ignoreListenerPause = true;

        muted = PlayerPrefs.GetInt(MuteKeyPref, 0) == 1;
        src.volume = 0f;

        if (music != null) src.Play();
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null && muteKey != Key.None && kb[muteKey].wasPressedThisFrame)
        {
            muted = !muted;
            PlayerPrefs.SetInt(MuteKeyPref, muted ? 1 : 0);
            PlayerPrefs.Save();
        }

        float target = muted ? 0f : volume;
        if (Time.timeScale == 0f) target *= pausedVolumeFactor;

        // Auch in Pause
        current = Mathf.MoveTowards(current, target, fadeSpeed * Time.unscaledDeltaTime);
        src.volume = current;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
