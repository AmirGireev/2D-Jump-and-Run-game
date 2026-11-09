using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using LeaderboardEntry = Unity.Services.Leaderboards.Models.LeaderboardEntry;

public class LevelTimer : MonoBehaviour
{
    public static LevelTimer Instance { get; private set; }

    [Header("Einstellungen")]
    public bool startOnFirstInput = true;
    public int maxEntries = 5;
    [Tooltip("Muenzen-Board-Endung")]
    public string coinBoardSuffix = "_Coins";

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

    // Online-Bestenliste
    enum OnlineState { Idle, Loading, Done, Failed }
    OnlineState onlineState = OnlineState.Idle;
    List<LeaderboardEntry> onlineEntries;   // null = offline
    string myPlayerId;
    bool allCoinsCollected;                 // Alle Muenzen
    bool showingCoinBoard;                  // Muenzen-Liste
    bool recordKnown;                       // Rekord geladen
    float recordTime = -1f;                 // -1 = keiner
    LeaderboardEntry myEntry;               // Eigener Platz

    // GUI
    GUIStyle timerStyle, titleStyle, centerStyle, rowStyle, rowHighlightStyle, buttonStyle, fieldStyle, hintStyle;

    public bool IsFinished { get { return finished; } }

    // ---------- Unity ----------

    void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[LevelTimer] Zweiter LevelTimer gefunden, wird entfernt.");
            Destroy(this);
            return;
        }

        Instance = this;
        Time.timeScale = 1f;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        scores = LoadScores();
        totalCoins = FindObjectsByType<Coin>().Length;

        // Rekord laden
        _ = LoadRecordAsync();
    }

    void Update()
    {
        // Ergebnis-Tafel
        if (finished)
        {
            if (Time.unscaledTime - finishedAt < 0.5f) return;

            bool confirm = KeyDown(Key.Enter) || KeyDown(Key.NumpadEnter) || PadDown(GamepadButton.South);
            bool retry = KeyDown(Key.R) || PadDown(GamepadButton.East);

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
            if (!startOnFirstInput || AnyInputDown() || HorizontalInput() != 0f)
                running = true;
            return;
        }

        elapsed += Time.deltaTime;
    }

    // ---------- Input ----------

    static bool KeyDown(Key k)
    {
        Keyboard kb = Keyboard.current;
        return kb != null && kb[k].wasPressedThisFrame;
    }

    static bool PadDown(GamepadButton b)
    {
        Gamepad gp = Gamepad.current;
        return gp != null && gp[b].wasPressedThisFrame;
    }

    // Jede Eingabe
    static bool AnyInputDown()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.anyKey.wasPressedThisFrame) return true;

        Mouse m = Mouse.current;
        if (m != null && (m.leftButton.wasPressedThisFrame
                          || m.rightButton.wasPressedThisFrame
                          || m.middleButton.wasPressedThisFrame))
            return true;

        Gamepad gp = Gamepad.current;
        if (gp != null)
        {
            foreach (GamepadButton b in System.Enum.GetValues(typeof(GamepadButton)))
            {
                try
                {
                    if (gp[b].wasPressedThisFrame) return true;
                }
                catch
                {
                    // Ungueltige ignorieren
                }
            }
        }
        return false;
    }

    static float HorizontalInput()
    {
        float raw = 0f;

        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb[Key.A].isPressed || kb[Key.LeftArrow].isPressed) raw -= 1f;
            if (kb[Key.D].isPressed || kb[Key.RightArrow].isPressed) raw += 1f;
        }

        Gamepad gp = Gamepad.current;
        if (gp != null)
        {
            raw += gp.leftStick.x.ReadValue();
            raw += gp.dpad.x.ReadValue();
        }

        return Mathf.Abs(raw) < 0.2f ? 0f : raw;
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
        allCoinsCollected = totalCoins > 0 && coins >= totalCoins;
        awaitingName = scores.Count < maxEntries || finalTime < scores[scores.Count - 1].time;

        // Sofort hochladen
        if (!awaitingName) _ = UploadAndLoadAsync(playerName);

        Time.timeScale = 0f;
    }

    // ---------- Highscores ----------

    string ScoresKey
    {
        get { return "scores_" + SceneManager.GetActiveScene().name; }
    }

    List<Entry> LoadScores()
    {
        List<Entry> list = new List<Entry>();
        string raw = PlayerPrefs.GetString(ScoresKey, "");
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

        PlayerPrefs.SetString(ScoresKey, string.Join(";", parts.ToArray()));
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

        // Hochladen
        _ = UploadAndLoadAsync(clean);
    }

    // ---------- Online ----------

    // Online-Rekord
    async Task LoadRecordAsync()
    {
        try
        {
            string board = SceneManager.GetActiveScene().name;
            List<LeaderboardEntry> top = await WithTimeout(OnlineHighscores.TopAsync(board, 1), 8000, "Rekord laden");
            if (top != null)
            {
                recordKnown = true;
                recordTime = top.Count > 0 ? (float)top[0].Score : -1f;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Online] Rekord laden fehlgeschlagen: " + e.Message);
        }
    }

    // Hochladen, Liste laden
    async Task UploadAndLoadAsync(string name)
    {
        onlineState = OnlineState.Loading;
        string board = SceneManager.GetActiveScene().name;   // ID = Szenenname
        float time = finalTime;

        try
        {
            await WithTimeout(OnlineHighscores.InitAsync(), 8000, "Anmeldung");
            if (!OnlineHighscores.IsReady)
            {
                onlineState = OnlineState.Failed;
                return;
            }

            await WithTimeout(OnlineHighscores.SetNameAsync(OnlineName(name)), 8000, "Name setzen");
            await WithTimeout(OnlineHighscores.SubmitAsync(board, time), 8000, "Upload");

            // Muenzen-Board
            bool coinRun = allCoinsCollected;
            string coinBoard = board + coinBoardSuffix;
            if (coinRun)
                await WithTimeout(OnlineHighscores.SubmitAsync(coinBoard, time), 8000, "Upload (alle Muenzen)");

            // Angezeigtes Board
            string shownBoard = coinRun ? coinBoard : board;
            List<LeaderboardEntry> list = await WithTimeout(OnlineHighscores.TopAsync(shownBoard, maxEntries), 8000, "Bestenliste");
            LeaderboardEntry mine = list != null
                ? await WithTimeout(OnlineHighscores.MyEntryAsync(shownBoard), 8000, "Eigener Platz")
                : null;

            // Normaler Rekord
            List<LeaderboardEntry> recordList = list;
            if (coinRun)
                recordList = await WithTimeout(OnlineHighscores.TopAsync(board, 1), 8000, "Rekord");

            onlineEntries = list;
            myEntry = mine;
            myPlayerId = OnlineHighscores.PlayerId;
            showingCoinBoard = coinRun;

            if (recordList != null)
            {
                recordKnown = true;
                recordTime = recordList.Count > 0 ? (float)recordList[0].Score : -1f;
            }
            onlineState = list != null ? OnlineState.Done : OnlineState.Failed;
            Debug.Log("[Online] Fertig: " + onlineState);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Online] Fehler: " + e);
            onlineState = OnlineState.Failed;
        }
    }

    // Mit Zeitlimit
    static async Task WithTimeout(Task t, int ms, string what)
    {
        Task first = await Task.WhenAny(t, Task.Delay(ms));
        if (first != t)
        {
            Debug.LogWarning("[Online] Zeitueberschreitung bei: " + what);
            return;
        }
        await t;
    }

    static async Task<T> WithTimeout<T>(Task<T> t, int ms, string what) where T : class
    {
        Task first = await Task.WhenAny(t, Task.Delay(ms));
        if (first != t)
        {
            Debug.LogWarning("[Online] Zeitueberschreitung bei: " + what);
            return null;
        }
        return await t;
    }

    // Gueltiger Name
    static string OnlineName(string raw)
    {
        string n = (raw ?? "").Replace("#", "").Replace(" ", "_").Trim();
        if (n.Length == 0) n = "Player";
        while (n.Length < 3) n += "_";
        return n;
    }

    // Anzeige-Name
    static string ShowName(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "?";
        int i = raw.LastIndexOf('#');
        return i > 0 ? raw.Substring(0, i) : raw;
    }

    [ContextMenu("Highscores loeschen")]
    void ClearScores()
    {
        PlayerPrefs.DeleteKey(ScoresKey);
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
        GUI.Label(new Rect(20f, 12f, 420f, 50f), "Time " + FormatTime(shown), timerStyle);
        if (recordKnown)
        {
            // Online-Rekord
            if (recordTime >= 0f)
                GUI.Label(new Rect(20f, 56f, 420f, 36f), "Record " + FormatTime(recordTime), rowStyle);
        }
        else if (scores.Count > 0)
        {
            // Lokaler Rekord
            GUI.Label(new Rect(20f, 56f, 420f, 36f), "Record " + FormatTime(scores[0].time), rowStyle);
        }
        if (totalCoins > 0)
            GUI.Label(new Rect(20f, 92f, 420f, 36f), "Coins " + coins + "/" + totalCoins, rowStyle);

        if (!finished) return;

        // Tafel
        Rect panel = new Rect(w / 2f - 260f, 90f, 520f, 520f);
        GUI.Box(panel, GUIContent.none);
        GUI.Box(panel, GUIContent.none);

        GUI.Label(new Rect(panel.x, panel.y + 20f, panel.width, 50f), "Level Complete!", titleStyle);
        string result = "Your time: " + FormatTime(finalTime);
        if (totalCoins > 0) result += "   Coins: " + coins + "/" + totalCoins;
        GUI.Label(new Rect(panel.x, panel.y + 80f, panel.width, 40f), result, centerStyle);

        // Namenseingabe
        if (awaitingName)
        {
            GUI.Label(new Rect(panel.x, panel.y + 150f, panel.width, 40f), "Top " + maxEntries + "! Enter your name:", centerStyle);
            playerName = GUI.TextField(new Rect(panel.x + 110f, panel.y + 200f, 300f, 46f), playerName, 12, fieldStyle);
            if (GUI.Button(new Rect(panel.x + 160f, panel.y + 280f, 200f, 55f), "Save", buttonStyle))
                SaveEntry();
            GUI.Label(new Rect(panel.x, panel.y + 345f, panel.width, 30f), "Enter / A / Cross: save", hintStyle);
            return;
        }

        // Bestenliste
        string title = (showingCoinBoard && onlineEntries != null) ? "All Coins Leaderboard" : "Leaderboard";
        if (onlineEntries != null) title += " (Online)";
        else if (onlineState == OnlineState.Failed) title += " (Offline)";
        GUI.Label(new Rect(panel.x, panel.y + 140f, panel.width, 40f), title, centerStyle);

        if (onlineState == OnlineState.Loading)
        {
            GUI.Label(new Rect(panel.x, panel.y + 185f, panel.width, 40f), "Loading...", centerStyle);
        }
        else if (onlineEntries != null)
        {
            // Online-Liste
            for (int i = 0; i < onlineEntries.Count; i++)
            {
                LeaderboardEntry e = onlineEntries[i];
                GUIStyle style = e.PlayerId == myPlayerId ? rowHighlightStyle : rowStyle;
                float y = panel.y + 185f + i * 40f;
                GUI.Label(new Rect(panel.x + 40f, y, 60f, 40f), (e.Rank + 1) + ".", style);
                GUI.Label(new Rect(panel.x + 100f, y, 250f, 40f), ShowName(e.PlayerName), style);
                GUI.Label(new Rect(panel.x + 350f, y, 140f, 40f), FormatTime((float)e.Score), style);
            }

            // Eigener Platz
            bool inTop = false;
            foreach (LeaderboardEntry e in onlineEntries)
                if (e.PlayerId == myPlayerId) { inTop = true; break; }

            if (!inTop && myEntry != null)
            {
                float y = panel.y + 185f + onlineEntries.Count * 40f;
                GUI.Label(new Rect(panel.x + 40f, y, 60f, 40f), (myEntry.Rank + 1) + ".", rowHighlightStyle);
                GUI.Label(new Rect(panel.x + 100f, y, 250f, 40f), ShowName(myEntry.PlayerName), rowHighlightStyle);
                GUI.Label(new Rect(panel.x + 350f, y, 140f, 40f), FormatTime((float)myEntry.Score), rowHighlightStyle);
            }
        }
        else
        {
            // Lokale Liste
            for (int i = 0; i < scores.Count; i++)
            {
                GUIStyle style = scores[i] == newEntry ? rowHighlightStyle : rowStyle;
                float y = panel.y + 185f + i * 40f;
                GUI.Label(new Rect(panel.x + 40f, y, 60f, 40f), (i + 1) + ".", style);
                GUI.Label(new Rect(panel.x + 100f, y, 250f, 40f), scores[i].name, style);
                GUI.Label(new Rect(panel.x + 350f, y, 140f, 40f), FormatTime(scores[i].time), style);
            }
        }

        // Buttons
        float by = panel.y + panel.height - 80f;
        if (GUI.Button(new Rect(panel.x + 40f, by, 200f, 55f), "Retry", buttonStyle))
            Restart();
        if (GUI.Button(new Rect(panel.x + 280f, by, 200f, 55f), HasNextLevel() ? "Next Level!" : "Back to Start", buttonStyle))
            LoadNext();
        GUI.Label(new Rect(panel.x, panel.y + panel.height - 22f, panel.width, 24f),
            "Enter / A / Cross: " + (HasNextLevel() ? "next level" : "back to start") + "     R / B / Circle: retry", hintStyle);
    }
}
