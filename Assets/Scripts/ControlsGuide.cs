using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

// Pause-Menue
public class ControlsGuide : MonoBehaviour
{
    [Header("Pause")]
    public Key toggleKey = Key.Escape;
    public Key toggleKeyAlt = Key.P;
    public GamepadButton toggleButtonPad = GamepadButton.Start;   // Options / Start
    [Tooltip("Menue-Szene")]
    public string menuSceneName = "Menu";

    [Header("Start")]
    public bool showAtStart = true;

    [Header("Aussehen")]
    [Range(0.4f, 1.2f)] public float guideScale = 0.75f;
    [Range(0f, 0.8f)] public float dimAmount = 0.3f;

    static bool shownOnce;
    static readonly string[] pauseItems = { "Resume", "Controls", "Main Menu", "Quit" };

    enum View { Closed, Pause, Controls }
    View view = View.Closed;
    bool startView;      // Start-Ansicht
    bool pausedByMenu;
    int selected;
    float lastStickY;

    PlayerController player;
    Texture2D white;
    GUIStyle titleStyle, headStyle, keyStyle, textStyle, hintStyle, centerHintStyle, buttonStyle, buttonSelStyle;

    bool IsOpen { get { return view != View.Closed; } }

    // ---------- Unity ----------

    void Start()
    {
        player = FindAnyObjectByType<PlayerController>();

        white = new Texture2D(1, 1);
        white.SetPixel(0, 0, Color.white);
        white.Apply();

        if (showAtStart && !shownOnce)
        {
            shownOnce = true;
            startView = true;
            view = View.Controls;
            Pause();
        }
    }

    void OnDestroy()
    {
        if (pausedByMenu) Time.timeScale = 1f;
        if (white != null) Destroy(white);
    }

    void Update()
    {
        if (!IsOpen)
        {
            // Ziel gesperrt
            if (LevelTimer.Instance != null && LevelTimer.Instance.IsFinished) return;

            if (KeyDown(toggleKey) || KeyDown(toggleKeyAlt) || PadDown(toggleButtonPad))
                OpenPause();
            return;
        }

        if (view == View.Controls) UpdateControls();
        else UpdatePause();
    }

    void UpdateControls()
    {
        bool back = KeyDown(Key.Escape) || KeyDown(Key.Enter) || KeyDown(Key.NumpadEnter) || KeyDown(Key.Space)
                    || PadDown(GamepadButton.South) || PadDown(GamepadButton.East);
        if (back) GoBackFromControls();
    }

    void UpdatePause()
    {
        bool up = KeyDown(Key.UpArrow) || PadDown(GamepadButton.DpadUp);
        bool down = KeyDown(Key.DownArrow) || PadDown(GamepadButton.DpadDown);

        float sy = StickY();
        if (sy > 0.6f && lastStickY <= 0.6f) up = true;
        if (sy < -0.6f && lastStickY >= -0.6f) down = true;
        lastStickY = sy;

        if (up) selected = (selected - 1 + pauseItems.Length) % pauseItems.Length;
        if (down) selected = (selected + 1) % pauseItems.Length;

        if (KeyDown(Key.Enter) || KeyDown(Key.NumpadEnter) || KeyDown(Key.Space) || PadDown(GamepadButton.South))
            ActivatePause(selected);
        else if (KeyDown(toggleKey) || KeyDown(toggleKeyAlt) || PadDown(toggleButtonPad) || PadDown(GamepadButton.East))
            Resume();
    }

    // ---------- Zustand ----------

    void OpenPause()
    {
        view = View.Pause;
        selected = 0;
        lastStickY = 0f;
        Pause();
    }

    void ActivatePause(int index)
    {
        switch (index)
        {
            case 0: Resume(); break;
            case 1: view = View.Controls; startView = false; break;
            case 2: LoadMenuScene(true); break;    
            case 3: LoadMenuScene(false); break;   
        }
    }

    void GoBackFromControls()
    {
        if (startView) { Resume(); return; }
        view = View.Pause;
        selected = 1;
    }

    void Pause()
    {
        if (Time.timeScale > 0f)
        {
            pausedByMenu = true;
            Time.timeScale = 0f;
        }
    }

    void Resume()
    {
        view = View.Closed;
        startView = false;
        if (pausedByMenu)
        {
            pausedByMenu = false;
            Time.timeScale = 1f;
        }
    }

    void LoadMenuScene(bool skipTitle)
    {
        MainMenu.skipTitleOnce = skipTitle;
        pausedByMenu = false;
        view = View.Closed;
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
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

    static float StickY()
    {
        Gamepad gp = Gamepad.current;
        return gp != null ? gp.leftStick.y.ReadValue() : 0f;
    }

    // ---------- Tastennamen ----------

    static string K(Key k)
    {
        switch (k)
        {
            case Key.Space: return "Space";
            case Key.LeftCtrl: return "Ctrl";
            case Key.RightCtrl: return "Right Ctrl";
            case Key.LeftShift: return "Shift";
            case Key.RightShift: return "Right Shift";
            case Key.CapsLock: return "Caps Lock";
            case Key.Enter: return "Enter";
            case Key.Escape: return "Esc";
            default:
                string s = k.ToString();
                if (s.StartsWith("Digit")) s = s.Substring(5);
                return s;
        }
    }

    // Controller-Namen
    static string P(GamepadButton b)
    {
        switch (b)
        {
            case GamepadButton.South: return "Cross / A";
            case GamepadButton.East: return "Circle / B";
            case GamepadButton.West: return "Square / X";
            case GamepadButton.North: return "Triangle / Y";
            case GamepadButton.LeftShoulder: return "L1 / LB";
            case GamepadButton.RightShoulder: return "R1 / RB";
            case GamepadButton.LeftTrigger: return "L2 / LT";
            case GamepadButton.RightTrigger: return "R2 / RT";
            case GamepadButton.Start: return "Options / Start";
            case GamepadButton.Select: return "Share / Select";
            case GamepadButton.LeftStick: return "L3";
            case GamepadButton.RightStick: return "R3";
            case GamepadButton.DpadUp: return "D-Pad Up";
            case GamepadButton.DpadDown: return "D-Pad Down";
            case GamepadButton.DpadLeft: return "D-Pad Left";
            case GamepadButton.DpadRight: return "D-Pad Right";
            default: return b.ToString();
        }
    }

    static string Keys(params Key[] keys)
    {
        string result = "";
        foreach (Key k in keys)
        {
            if (k == Key.None) continue;
            if (result.Length > 0) result += " / ";
            result += K(k);
        }
        return result.Length > 0 ? result : "-";
    }

    static string Pads(params GamepadButton[] buttons)
    {
        string result = "";
        foreach (GamepadButton b in buttons)
        {
            if (result.Length > 0) result += " / ";
            result += P(b);
        }
        return result.Length > 0 ? result : "-";
    }

    static T Pick<T>(PlayerController p, System.Func<PlayerController, T> get, T fallback)
    {
        return p != null ? get(p) : fallback;
    }

    // ---------- GUI ----------

    void BuildStyles()
    {
        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 40, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        titleStyle.normal.textColor = Color.white;

        headStyle = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold };
        headStyle.normal.textColor = new Color(0.55f, 0.85f, 1f);

        keyStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
        keyStyle.normal.textColor = new Color(1f, 0.85f, 0.35f);
        keyStyle.wordWrap = false;

        textStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleLeft };
        textStyle.normal.textColor = Color.white;
        textStyle.wordWrap = false;

        hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleRight };
        hintStyle.normal.textColor = new Color(1f, 1f, 1f, 0.7f);

        centerHintStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
        centerHintStyle.normal.textColor = new Color(1f, 1f, 1f, 0.65f);

        buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 26 };
        buttonSelStyle = new GUIStyle(buttonStyle) { fontStyle = FontStyle.Bold };
        buttonSelStyle.normal.textColor = Color.yellow;
        buttonSelStyle.hover.textColor = Color.yellow;
        buttonSelStyle.active.textColor = Color.yellow;
        buttonSelStyle.focused.textColor = Color.yellow;
    }

    void Row(float x, ref float y, float keyWidth, float textWidth, string key, string text)
    {
        GUI.Label(new Rect(x, y, keyWidth, 34f), key, keyStyle);
        GUI.Label(new Rect(x + keyWidth, y, textWidth, 34f), text, textStyle);
        y += 38f;
    }

    void DrawPanel(Rect r)
    {
        Color old = GUI.color;
        GUI.color = new Color(0.08f, 0.1f, 0.16f, 0.97f);
        GUI.DrawTexture(r, white);
        GUI.color = old;
    }

    void OnGUI()
    {
        GUI.depth = -100;
        if (titleStyle == null) BuildStyles();

        float scale = Screen.height / 720f * guideScale;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float w = Screen.width / scale;
        float h = Screen.height / scale;

        if (!IsOpen)
        {
            string hint = K(toggleKey) + " / " + P(toggleButtonPad) + ": Pause";
            GUI.Label(new Rect(w - 520f, 10f, 500f, 30f), hint, hintStyle);
        }
        else
        {
            Color old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, dimAmount);
            GUI.DrawTexture(new Rect(0f, 0f, w, h), white);
            GUI.color = old;

            if (view == View.Pause) DrawPause(w, h);
            else DrawControls(w, h);
        }

        GUI.matrix = Matrix4x4.identity;
    }

    void DrawPause(float w, float h)
    {
        Rect panel = new Rect(w / 2f - 260f, (h - 470f) / 2f, 520f, 470f);
        DrawPanel(panel);

        GUI.Label(new Rect(panel.x, panel.y + 18f, panel.width, 56f), "Pause", titleStyle);

        for (int i = 0; i < pauseItems.Length; i++)
        {
            Rect r = new Rect(panel.x + 90f, panel.y + 110f + i * 70f, panel.width - 180f, 56f);
            if (GUI.Button(r, pauseItems[i], i == selected ? buttonSelStyle : buttonStyle))
            {
                selected = i;
                ActivatePause(i);
                return;
            }
        }

        GUI.Label(new Rect(panel.x, panel.y + panel.height - 40f, panel.width, 26f),
            "Up / Down + Enter (A / Cross)   |   Esc / Start: resume", centerHintStyle);
    }

    void DrawControls(float w, float h)
    {
        Rect panel = new Rect(w / 2f - 510f, (h - 540f) / 2f, 1020f, 540f);
        DrawPanel(panel);

        GUI.Label(new Rect(panel.x, panel.y + 14f, panel.width, 56f), "Controls", titleStyle);

        // Tasten lesen
        Key jump = Pick(player, p => p.jumpKey, Key.W);
        Key dash = Pick(player, p => p.dashKey, Key.CapsLock);
        Key dashAlt = Pick(player, p => p.dashKeyAlt, Key.Q);
        Key slide = Pick(player, p => p.slideKey, Key.S);
        Key slideAlt = Pick(player, p => p.slideKeyAlt, Key.LeftCtrl);
        Key slideAlt2 = Pick(player, p => p.slideKeyAlt2, Key.LeftShift);
        Key straight = Pick(player, p => p.shootStraightKey, Key.Space);
        Key tp = Pick(player, p => p.teleportKey, Key.E);

        GamepadButton padJump = Pick(player, p => p.padJumpButton, GamepadButton.South);
        GamepadButton padSlide = Pick(player, p => p.padSlideButton, GamepadButton.East);
        GamepadButton padSlideAlt = Pick(player, p => p.padSlideAltButton, GamepadButton.LeftTrigger);
        GamepadButton padDash = Pick(player, p => p.padDashButton, GamepadButton.West);
        GamepadButton padDashAlt = Pick(player, p => p.padDashAltButton, GamepadButton.LeftShoulder);
        GamepadButton padShoot = Pick(player, p => p.padShootButton, GamepadButton.North);
        GamepadButton padShootAlt = Pick(player, p => p.padShootAltButton, GamepadButton.RightTrigger);
        GamepadButton padTp = Pick(player, p => p.padTeleportButton, GamepadButton.RightShoulder);

        // Tastatur
        float x = panel.x + 30f;
        float y = panel.y + 80f;
        GUI.Label(new Rect(x, y, 440f, 36f), "Keyboard and Mouse", headStyle);
        y += 46f;
        Row(x, ref y, 200f, 240f, Keys(jump), "Jump (also 2x)");
        Row(x, ref y, 200f, 240f, Keys(jump) + " at wall", "Wall jump");
        Row(x, ref y, 200f, 240f, Keys(slide, slideAlt, slideAlt2), "Slide");
        Row(x, ref y, 200f, 240f, Keys(straight), "Shoot straight");
        Row(x, ref y, 200f, 240f, "Left click", "Shoot toward mouse");
        Row(x, ref y, 200f, 240f, "Shoot again", "Teleport to projectile");
        Row(x, ref y, 200f, 240f, Keys(tp), "Teleport (extra key)");
        Row(x, ref y, 200f, 240f, Keys(dash, dashAlt), "Dash");

        // Controller
        x = panel.x + 460f;
        y = panel.y + 80f;
        GUI.Label(new Rect(x, y, 420f, 36f), "Controller", headStyle);
        y += 46f;
        Row(x, ref y, 270f, 260f, Pads(padJump), "Jump (also 2x)");
        Row(x, ref y, 270f, 260f, Pads(padJump) + " at wall", "Wall jump");
        Row(x, ref y, 270f, 260f, Pads(padSlide, padSlideAlt), "Slide");
        Row(x, ref y, 270f, 260f, Pads(padShoot, padShootAlt), "Shoot (R-stick aims)");
        Row(x, ref y, 270f, 260f, "Shoot again", "Teleport to projectile");
        Row(x, ref y, 270f, 260f, Pads(padTp), "Teleport (extra)");
        Row(x, ref y, 270f, 260f, Pads(padDash, padDashAlt), "Dash");

        // Zurueck-Button
        string backText = startView ? "Let's go!" : "Back";
        if (GUI.Button(new Rect(w / 2f - 130f, panel.y + 464f, 260f, 54f), backText, buttonStyle))
            GoBackFromControls();
    }
}
