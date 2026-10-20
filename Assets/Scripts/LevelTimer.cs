using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelTimer : MonoBehaviour
{
    public static LevelTimer Instance { get; private set; }

    [Header("Einstellungen")]
    public bool startOnFirstInput = true;
    public int maxEntries = 5;

    class Entry
    {
        public string name;
        public float time;
    }

    // Zeit
    float elapsed, finalTime, finishedAt;
    bool running, finished;

    // Muenzen
    int coins, totalCoins;

    // Bestenliste
    List<Entry> scores = new List<Entry>();
    Entry newEntry;
    string playerName = "Player";
    bool awaitingName;

    // GUI
    GUIStyle timerStyle, titleStyle, centerStyle, rowStyle, rowHighlightStyle, buttonStyle, fieldStyle, hintStyle;

    public bool IsFinished { get { return finished; } }

    // ---------- Unity ----------

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    void Start()
    {
        scores = LoadScores();
        totalCoins = FindObjectsByType<Coin>().Length;
    }

    void Update()
    {
        // Ergebnis-Tafel
        if (finished)
        {
            if (Time.unscaledTime - finishedAt < 0.5f) return;

            bool confirm = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)
                           || Input.GetKeyDown(KeyCode.JoystickButton1);
            bool retry = Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.JoystickButton2);

            if (awaitingName)
            {
                if (confirm) SaveEntry();
            }
            else
            {
                if (confirm) LoadNext();
                else if (retry) Restart();
            }
            return;
        }

        // Start
        if (!running)
        {
            if (!startOnFirstInput || Input.GetAxisRaw("Horizontal") != 0f
                || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.JoystickButton1))
                running = true;
            return;
        }

        elapsed += Time.deltaTime;
    }

    // ---------- Spielstand ----------

    public void AddCoins(int amount)
    {
        coins += amount;
    }

    public void Finish()
    {
        if (finished) return;

        finished = true;
        running = false;
        finalTime = elapsed;
        finishedAt = Time.unscaledTime;

        // Spieler sperren
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null) player.enabled = false;

        playerName = PlayerPrefs.GetString("playerName", "Player");
        awaitingName = scores.Count < maxEntries || finalTime < scores[scores.Count - 1].time;

        Time.timeScale = 0f;
    }

    // ---------- Highscores ----------

    string Key
    {
        get { return "scores_" + SceneManager.GetActiveScene().name; }
    }

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
        if (next >= SceneManager.sceneCountInBuildSettings) next = 0;
        SceneManager.LoadScene(next);
    }

    // ---------- GUI ----------

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

        hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
        hintStyle.normal.textColor = new Color(1f, 1f, 1f, 0.65f);

        fieldStyle = new GUIStyle(GUI.skin.textField) { fontSize = 26, alignment = TextAnchor.MiddleLeft };
        fieldStyle.normal.textColor = Color.white;
        fieldStyle.focused.textColor = Color.white;
    }

    void OnGUI()
    {
        if (timerStyle == null) BuildStyles();

        // Skalierung
        float scale = Screen.height / 720f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float w = Screen.width / scale;

        // Anzeige oben links
        float shown = finished ? finalTime : elapsed;
        GUI.Label(new Rect(20f, 12f, 420f, 50f), "Zeit " + FormatTime(shown), timerStyle);
        if (scores.Count > 0)
            GUI.Label(new Rect(20f, 56f, 420f, 36f), "Rekord " + FormatTime(scores[0].time), rowStyle);
        if (totalCoins > 0)
            GUI.Label(new Rect(20f, 92f, 420f, 36f), "Münzen " + coins + "/" + totalCoins, rowStyle);

        if (!finished) return;

        // Tafel
        Rect panel = new Rect(w / 2f - 260f, 90f, 520f, 520f);
        GUI.Box(panel, GUIContent.none);
        GUI.Box(panel, GUIContent.none);

        GUI.Label(new Rect(panel.x, panel.y + 20f, panel.width, 50f), "Level geschafft!", titleStyle);
        string result = "Deine Zeit: " + FormatTime(finalTime);
        if (totalCoins > 0) result += "   Münzen: " + coins + "/" + totalCoins;
        GUI.Label(new Rect(panel.x, panel.y + 80f, panel.width, 40f), result, centerStyle);

        // Namenseingabe
        if (awaitingName)
        {
            GUI.Label(new Rect(panel.x, panel.y + 150f, panel.width, 40f), "In den Top " + maxEntries + "! Dein Name:", centerStyle);
            playerName = GUI.TextField(new Rect(panel.x + 110f, panel.y + 200f, 300f, 46f), playerName, 12, fieldStyle);
            if (GUI.Button(new Rect(panel.x + 160f, panel.y + 280f, 200f, 55f), "Speichern", buttonStyle))
                SaveEntry();
            GUI.Label(new Rect(panel.x, panel.y + 345f, panel.width, 30f), "Enter oder Kreuz: speichern", hintStyle);
            return;
        }

        // Bestenliste
        GUI.Label(new Rect(panel.x, panel.y + 140f, panel.width, 40f), "Bestenliste", centerStyle);
        for (int i = 0; i < scores.Count; i++)
        {
            GUIStyle style = scores[i] == newEntry ? rowHighlightStyle : rowStyle;
            float y = panel.y + 185f + i * 40f;
            GUI.Label(new Rect(panel.x + 40f, y, 60f, 40f), (i + 1) + ".", style);
            GUI.Label(new Rect(panel.x + 100f, y, 250f, 40f), scores[i].name, style);
            GUI.Label(new Rect(panel.x + 350f, y, 140f, 40f), FormatTime(scores[i].time), style);
        }

        // Buttons
        float by = panel.y + panel.height - 80f;
        if (GUI.Button(new Rect(panel.x + 40f, by, 200f, 55f), "Nochmal", buttonStyle))
            Restart();
        if (GUI.Button(new Rect(panel.x + 280f, by, 200f, 55f), HasNextLevel() ? "Weiter" : "Zum Anfang", buttonStyle))
            LoadNext();
        GUI.Label(new Rect(panel.x, panel.y + panel.height - 22f, panel.width, 24f),
            "Enter / Kreuz: " + (HasNextLevel() ? "weiter" : "zum Anfang") + "     R / Kreis: nochmal", hintStyle);
    }
}
