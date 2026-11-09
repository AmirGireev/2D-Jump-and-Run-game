using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

// Hauptmenue mit Titelscreen
public class MainMenu : MonoBehaviour
{
    [Tooltip("Menue-Szene")]
    public string menuSceneName = "Menu";
    public string gameTitle = "2D Jump and Run";

    [Header("Titelscreen")]
    public bool startOnTitle = true;
    [Tooltip("Hintergrundbild fuer den Titelscreen (optional)")]
    public Texture2D titleBackground;
    public string clickText = "Click to play";

    [Header("Aussehen")]
    [Range(0.4f, 1.5f)] public float uiScale = 1f;

    enum View { Title, Main, Levels, Controls }
    View view = View.Main;
    readonly List<int> levelIndices = new List<int>();
    readonly List<string> levelNames = new List<string>();
    int selected;
    float lastStickY;
    Texture2D white;
    GUIStyle titleStyle, buttonStyle, buttonSelStyle, smallStyle, clickStyle;

    // ---------- Unity ----------

    // Vom Pause-Menue gesetzt: "Main Menu" ueberspringt den Titelscreen einmalig
    public static bool skipTitleOnce;

    void Awake()
    {
        Time.timeScale = 1f;   // Pause beenden
        view = (startOnTitle && !skipTitleOnce) ? View.Title : View.Main;
        skipTitleOnce = false;
    }

    void Start()
    {
        white = new Texture2D(1, 1);
        white.SetPixel(0, 0, Color.white);
        white.Apply();

        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string name = System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i));
            if (name == menuSceneName) continue;
            levelIndices.Add(i);
            levelNames.Add(name.Replace('_', ' '));
        }
    }

    void OnDestroy()
    {
        if (white != null) Destroy(white);
    }

    void Update()
    {
        bool up = KeyDown(Key.UpArrow) || PadDown(GamepadButton.DpadUp);
        bool down = KeyDown(Key.DownArrow) || PadDown(GamepadButton.DpadDown);

        Gamepad gp = Gamepad.current;
        float sy = gp != null ? gp.leftStick.y.ReadValue() : 0f;
        if (sy > 0.6f && lastStickY <= 0.6f) up = true;
        if (sy < -0.6f && lastStickY >= -0.6f) down = true;
        lastStickY = sy;

        int count = ItemCount;
        if (up) selected = (selected - 1 + count) % count;
        if (down) selected = (selected + 1) % count;

        if (KeyDown(Key.Enter) || KeyDown(Key.NumpadEnter) || KeyDown(Key.Space) || PadDown(GamepadButton.South))
            Activate(selected);
        else if ((view == View.Levels || view == View.Controls) && (KeyDown(Key.Escape) || PadDown(GamepadButton.East)))
            GoTo(View.Main);
    }

    // ---------- Logik ----------

    int ItemCount
    {
        get
        {
            if (view == View.Title) return 1;                       // nur "Click to play"
            if (view == View.Main) return 3;                        // Play, Controls, Quit
            if (view == View.Levels) return levelIndices.Count + 1;  // plus Back
            return 1;                                               // nur Back
        }
    }

    void GoTo(View v)
    {
        view = v;
        selected = 0;
    }

    void Activate(int index)
    {
        if (view == View.Title)
        {
            GoTo(View.Main);
            return;
        }

        if (view == View.Main)
        {
            if (index == 0) GoTo(View.Levels);
            else if (index == 1) GoTo(View.Controls);
            else GoTo(View.Title);   // Quit fuehrt zum Titelscreen
            return;
        }

        if (view == View.Controls)
        {
            GoTo(View.Main);
            return;
        }

        if (index < levelIndices.Count)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(levelIndices[index]);
        }
        else
        {
            GoTo(View.Main);
        }
    }

    public static void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------- Input ----------

    static bool KeyDown(Key k)
    {
        if (k == Key.None) return false;
        if (!System.Enum.IsDefined(typeof(Key), k)) return false;
        Keyboard kb = Keyboard.current;
        return kb != null && kb[k].wasPressedThisFrame;
    }

    static bool PadDown(GamepadButton b)
    {
        if (!System.Enum.IsDefined(typeof(GamepadButton), b)) return false;
        Gamepad gp = Gamepad.current;
        return gp != null && gp[b].wasPressedThisFrame;
    }

    // ---------- GUI ----------

    void BuildStyles()
    {
        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 60, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        titleStyle.normal.textColor = Color.white;

        buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 30 };

        buttonSelStyle = new GUIStyle(buttonStyle) { fontStyle = FontStyle.Bold };
        buttonSelStyle.normal.textColor = Color.yellow;
        buttonSelStyle.hover.textColor = Color.yellow;
        buttonSelStyle.active.textColor = Color.yellow;
        buttonSelStyle.focused.textColor = Color.yellow;

        smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
        smallStyle.normal.textColor = new Color(1f, 1f, 1f, 0.65f);

        clickStyle = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        clickStyle.normal.textColor = Color.white;
    }

    void OnGUI()
    {
        if (titleStyle == null) BuildStyles();

        Matrix4x4 oldMatrix = GUI.matrix;
        Color oldColor = GUI.color;

        float s = Screen.height / 720f * uiScale;
        GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
        float w = Screen.width / s;
        float h = Screen.height / s;

        if (view == View.Title && titleBackground != null)
        {
            GUI.DrawTexture(new Rect(0f, 0f, w, h), titleBackground, ScaleMode.ScaleAndCrop);
        }
        else
        {
            GUI.color = new Color(0.04f, 0.06f, 0.1f, 1f);
            GUI.DrawTexture(new Rect(0f, 0f, w, h), white);
            GUI.color = oldColor;
        }

        if (view == View.Title) DrawTitle(w, h);
        else if (view == View.Main) DrawMain(w, h);
        else if (view == View.Levels) DrawLevels(w, h);
        else DrawControls(w, h);

        GUI.matrix = oldMatrix;
    }

    void DrawTitle(float w, float h)
    {
        GUI.Label(new Rect(0f, h * 0.25f, w, 90f), gameTitle, titleStyle);

        // blinkender Text
        float a = Mathf.Lerp(0.35f, 1f, Mathf.PingPong(Time.unscaledTime * 1.2f, 1f));
        Color old = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, a);
        GUI.Label(new Rect(0f, h * 0.65f, w, 50f), clickText, clickStyle);
        GUI.color = old;

        if (GUI.Button(new Rect(0f, 0f, w, h), GUIContent.none, GUIStyle.none))
            GoTo(View.Main);
    }

    void DrawMain(float w, float h)
    {
        GUI.Label(new Rect(0f, h * 0.2f, w, 80f), gameTitle, titleStyle);

        string[] labels = { "Play", "Controls", "Quit" };
        for (int i = 0; i < labels.Length; i++)
        {
            Rect r = new Rect(w / 2f - 150f, h * 0.45f + i * 84f, 300f, 64f);
            if (GUI.Button(r, labels[i], i == selected ? buttonSelStyle : buttonStyle))
            {
                selected = i;
                Activate(i);
                return;
            }
        }

        GUI.Label(new Rect(0f, h - 40f, w, 30f), "Up / Down + Enter (A / Cross)", smallStyle);
    }

    // Feste Tasten
    static readonly string[,] controlRows =
    {
        { "W / Space", "Jump (also 2x)" },
        { "W at wall", "Wall jump" },
        { "S / Ctrl / Shift", "Slide" },
        { "Space", "Shoot straight" },
        { "Left click", "Shoot toward mouse" },
        { "Left click again", "Teleport to projectile" },
        { "E", "Teleport (extra key)" },
        { "Caps Lock / Q", "Dash" },
        { "Esc / Start", "Pause menu" },
    };

    void DrawControls(float w, float h)
    {
        GUI.Label(new Rect(0f, h * 0.1f, w, 70f), "Controls", titleStyle);

        float x = w / 2f - 380f;
        float y = h * 0.27f;
        GUIStyle keyS = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
        keyS.normal.textColor = new Color(1f, 0.85f, 0.35f);
        GUIStyle textS = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleLeft };
        textS.normal.textColor = Color.white;

        for (int i = 0; i < controlRows.GetLength(0); i++)
        {
            GUI.Label(new Rect(x, y + i * 38f, 300f, 34f), controlRows[i, 0], keyS);
            GUI.Label(new Rect(x + 300f, y + i * 38f, 400f, 34f), controlRows[i, 1], textS);
        }

        Rect back = new Rect(w / 2f - 130f, h - 130f, 260f, 60f);
        if (GUI.Button(back, "Back", buttonSelStyle))
            Activate(0);

        GUI.Label(new Rect(0f, h - 50f, w, 30f), "Enter / A / Cross or Esc / B: back", smallStyle);
    }

    void DrawLevels(float w, float h)
    {
        GUI.Label(new Rect(0f, h * 0.12f, w, 70f), "Select Level", titleStyle);

        if (levelIndices.Count == 0)
            GUI.Label(new Rect(0f, h * 0.4f, w, 40f), "No levels in Build Settings", smallStyle);

        for (int i = 0; i <= levelIndices.Count; i++)
        {
            bool isBack = i == levelIndices.Count;
            string text = isBack ? "Back" : (i + 1) + ".  " + levelNames[i];
            Rect r = new Rect(w / 2f - 200f, h * 0.3f + i * 74f, 400f, 60f);
            if (GUI.Button(r, text, i == selected ? buttonSelStyle : buttonStyle))
            {
                selected = i;
                Activate(i);
                return;
            }
        }

        GUI.Label(new Rect(0f, h - 40f, w, 30f), "Up / Down + Enter (A / Cross)   |   Esc / B: back", smallStyle);
    }
}
