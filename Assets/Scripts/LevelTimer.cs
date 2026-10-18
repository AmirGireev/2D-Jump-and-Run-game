using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

// Zeigt oben links die Zeit an und beim Erreichen des Ziels eine Ergebnis-Tafel mit Bestenliste (Top 5 pro Level).
// Einbau: leeres GameObject "LevelTimer" in jede Level-Szene legen und dieses Script draufziehen.
// Die Anzeige wird per Code gezeichnet (OnGUI), du brauchst also keinen Canvas.
public class LevelTimer : MonoBehaviour
{
    public static LevelTimer Instance { get; private set; }

    [Header("Einstellungen")]
    public bool startOnFirstInput = true;   // Zeit startet erst bei der ersten Bewegung
    public int maxEntries = 5;              // so viele Plaetze hat die Bestenliste

    class Entry
    {
        public string name;
        public float time;
    }

    float elapsed, finalTime;
    bool running, finished, awaitingName;
    string playerName = "Player";
    List<Entry> scores = new List<Entry>();
    Entry newEntry;

    GUIStyle timerStyle, titleStyle, centerStyle, rowStyle, rowHighlightStyle, buttonStyle, fieldStyle;

    string Key
    {
        get { return "scores_" + SceneManager.GetActiveScene().name; }
    }

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    void Start()
    {
        scores = LoadScores();
    }

    void Update()
    {
        if (finished) return;

        if (!running)
        {
            if (!startOnFirstInput || Input.GetAxisRaw("Horizontal") != 0f || Input.GetKeyDown(KeyCode.W))
                running = true;
            return;
        }

        elapsed += Time.deltaTime;
    }

    // Wird vom Ziel-Tor (Goal) aufgerufen
    public void Finish()
    {
        if (finished) return;

        finished = true;
        running = false;
        finalTime = elapsed;

        // Spieler sperren, sonst loesen Tastendruecke beim Namen-Tippen Spielaktionen aus
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null) player.enabled = false;

        playerName = PlayerPrefs.GetString("playerName", "Player");
        awaitingName = scores.Count < maxEntries || finalTime < scores[scores.Count - 1].time;

        Time.timeScale = 0f;
    }

    // ---------- Speichern und Laden ----------

    List<Entry> LoadScores()
    {
        List<Entry> list = new List<Entry>();
        string raw = PlayerPrefs.GetString(Key, "");
        if (string.IsNullOrEmpty(raw)) return list;

        foreach (string part in raw.Split(';'))
        {
            string[] kv = part.Split(':');
            if (kv.Length != 2) continue;

            float t;
            if (!float.TryParse(kv[1], NumberStyles.Float, CultureInfo.InvariantCulture, out t)) continue;
            list.Add(new Entry { name = kv[0], time = t });
        }

        list.Sort((a, b) => a.time.CompareTo(b.time));
        return list;
    }

    void SaveScores()
    {
        List<string> parts = new List<string>();
        foreach (Entry e in scores)
            parts.Add(e.name + ":" + e.time.ToString("F3", CultureInfo.InvariantCulture));

        PlayerPrefs.SetString(Key, string.Join(";", parts.ToArray()));
        PlayerPrefs.Save();
    }

    void SaveEntry()
    {
        string clean = (playerName ?? "").Replace(":", "").Replace(";", "").Trim();
        if (clean.Length == 0) clean = "Player";
        PlayerPrefs.SetString("playerName", clean);

        newEntry = new Entry { name = clean, time = finalTime };
        scores.Add(newEntry);
        scores.Sort((a, b) => a.time.CompareTo(b.time));
        if (scores.Count > maxEntries)
            scores.RemoveRange(maxEntries, scores.Count - maxEntries);

        SaveScores();
        awaitingName = false;
    }

    // Im Editor: Zahnrad-Menue des Scripts > "Highscores dieses Levels loeschen"
    [ContextMenu("Highscores dieses Levels loeschen")]
    void ClearScores()
    {
        PlayerPrefs.DeleteKey(Key);
        scores.Clear();
    }

    // ---------- Szenenwechsel ----------

    void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    bool HasNextLevel()
    {
        return SceneManager.GetActiveScene().buildIndex + 1 < SceneManager.sceneCountInBuildSettings;
    }

    void LoadNext()
    {
        Time.timeScale = 1f;
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        if (next >= SceneManager.sceneCountInBuildSettings) next = 0; // nach dem letzten Level zurueck zum Anfang
        SceneManager.LoadScene(next);
    }

    // ---------- Anzeige ----------

    static string FormatTime(float t)
    {
        int minutes = (int)(t / 60f);
        float seconds = t - minutes * 60f;
        return string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00.00}", minutes, seconds);
    }

    void BuildStyles()
    {
        timerStyle = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold };
        timerStyle.normal.textColor = Color.white;

        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 38, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        titleStyle.normal.textColor = Color.white;

        centerStyle = new GUIStyle(GUI.skin.label) { fontSize = 26, alignment = TextAnchor.MiddleCenter };
        centerStyle.normal.textColor = Color.white;

        rowStyle = new GUIStyle(GUI.skin.label) { fontSize = 26, alignment = TextAnchor.MiddleLeft };
        rowStyle.normal.textColor = Color.white;

        rowHighlightStyle = new GUIStyle(rowStyle);
        rowHighlightStyle.normal.textColor = Color.yellow;

        buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 26 };

        fieldStyle = new GUIStyle(GUI.skin.textField) { fontSize = 26, alignment = TextAnchor.MiddleLeft };
        fieldStyle.normal.textColor = Color.white;
        fieldStyle.focused.textColor = Color.white;
    }

    void OnGUI()
    {
        if (timerStyle == null) BuildStyles();

        // Auf jede Bildschirmgroesse skalieren (Layout ist auf 720 Pixel Hoehe ausgelegt)
        float scale = Screen.height / 720f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float w = Screen.width / scale;

        // Zeitanzeige oben links
        float shown = finished ? finalTime : elapsed;
        GUI.Label(new Rect(20f, 12f, 420f, 50f), "Zeit " + FormatTime(shown), timerStyle);
        if (scores.Count > 0)
            GUI.Label(new Rect(20f, 56f, 420f, 36f), "Rekord " + FormatTime(scores[0].time), rowStyle);

        if (!finished) return;

        // Ergebnis-Tafel
        Rect panel = new Rect(w / 2f - 260f, 90f, 520f, 520f);
        GUI.Box(panel, GUIContent.none);
        GUI.Box(panel, GUIContent.none); // zweimal, damit der Hintergrund dunkler wird

        GUI.Label(new Rect(panel.x, panel.y + 20f, panel.width, 50f), "Level geschafft!", titleStyle);
        GUI.Label(new Rect(panel.x, panel.y + 80f, panel.width, 40f), "Deine Zeit: " + FormatTime(finalTime), centerStyle);

        if (awaitingName)
        {
            GUI.Label(new Rect(panel.x, panel.y + 150f, panel.width, 40f), "In den Top " + maxEntries + "! Dein Name:", centerStyle);
            playerName = GUI.TextField(new Rect(panel.x + 110f, panel.y + 200f, 300f, 46f), playerName, 12, fieldStyle);
            if (GUI.Button(new Rect(panel.x + 160f, panel.y + 280f, 200f, 55f), "Speichern", buttonStyle))
                SaveEntry();
            return;
        }

        GUI.Label(new Rect(panel.x, panel.y + 140f, panel.width, 40f), "Bestenliste", centerStyle);
        for (int i = 0; i < scores.Count; i++)
        {
            GUIStyle style = scores[i] == newEntry ? rowHighlightStyle : rowStyle;
            float y = panel.y + 185f + i * 40f;
            GUI.Label(new Rect(panel.x + 40f, y, 60f, 40f), (i + 1) + ".", style);
            GUI.Label(new Rect(panel.x + 100f, y, 250f, 40f), scores[i].name, style);
            GUI.Label(new Rect(panel.x + 350f, y, 140f, 40f), FormatTime(scores[i].time), style);
        }

        float by = panel.y + panel.height - 80f;
        if (GUI.Button(new Rect(panel.x + 40f, by, 200f, 55f), "Nochmal", buttonStyle))
            Restart();
        if (GUI.Button(new Rect(panel.x + 280f, by, 200f, 55f), HasNextLevel() ? "Weiter" : "Zum Anfang", buttonStyle))
            LoadNext();
    }
}
