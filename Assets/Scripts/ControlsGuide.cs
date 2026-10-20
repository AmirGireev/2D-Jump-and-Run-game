using UnityEngine;

public class ControlsGuide : MonoBehaviour
{
    public bool showAtStart = true;
    public KeyCode toggleKey = KeyCode.H;
    public KeyCode toggleKeyAlt = KeyCode.F1;
    public KeyCode toggleButtonPad = KeyCode.JoystickButton9;

    [Header("Aussehen")]
    [Range(0.4f, 1.2f)] public float guideScale = 0.75f;
    [Range(0f, 0.8f)] public float dimAmount = 0.3f;

    static bool shownOnce;

    bool open, pausedByGuide;
    PlayerController player;
    Texture2D white;
    GUIStyle titleStyle, headStyle, keyStyle, textStyle, hintStyle, buttonStyle;

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
            SetOpen(true);
        }
    }

    void OnDestroy()
    {
        if (pausedByGuide) Time.timeScale = 1f;
        if (white != null) Destroy(white);
    }

    void Update()
    {
        // Schliessen
        if (open)
        {
            bool onlyMouse = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);
            if (Input.anyKeyDown && !onlyMouse) SetOpen(false);
            return;
        }

        // Level fertig
        if (LevelTimer.Instance != null && LevelTimer.Instance.IsFinished) return;

        // Oeffnen
        if (Input.GetKeyDown(toggleKey) || Input.GetKeyDown(toggleKeyAlt) || Input.GetKeyDown(toggleButtonPad))
            SetOpen(true);
    }

    // ---------- Pause ----------

    void SetOpen(bool value)
    {
        open = value;
        if (open)
        {
            if (Time.timeScale > 0f) { pausedByGuide = true; Time.timeScale = 0f; }
        }
        else if (pausedByGuide)
        {
            pausedByGuide = false;
            Time.timeScale = 1f;
        }
    }

    // ---------- Tastennamen ----------

    static string K(KeyCode k)
    {
        switch (k)
        {
            case KeyCode.Space: return "Leertaste";
            case KeyCode.LeftControl: return "Strg";
            case KeyCode.RightControl: return "Strg rechts";
            case KeyCode.LeftShift: return "Shift";
            case KeyCode.RightShift: return "Shift rechts";
            case KeyCode.CapsLock: return "Feststelltaste";
            case KeyCode.Return: return "Enter";
            case KeyCode.Mouse0: return "Linksklick";
            case KeyCode.Mouse1: return "Rechtsklick";
            case KeyCode.JoystickButton0: return "Quadrat";
            case KeyCode.JoystickButton1: return "Kreuz";
            case KeyCode.JoystickButton2: return "Kreis";
            case KeyCode.JoystickButton3: return "Dreieck";
            case KeyCode.JoystickButton4: return "L1";
            case KeyCode.JoystickButton5: return "R1";
            case KeyCode.JoystickButton6: return "L2";
            case KeyCode.JoystickButton7: return "R2";
            case KeyCode.JoystickButton8: return "Share";
            case KeyCode.JoystickButton9: return "Options";
            case KeyCode.JoystickButton10: return "L3";
            case KeyCode.JoystickButton11: return "R3";
            default: return k.ToString();
        }
    }

    static string Keys(params KeyCode[] keys)
    {
        string result = "";
        foreach (KeyCode k in keys)
        {
            if (k == KeyCode.None) continue;
            if (result.Length > 0) result += " / ";
            result += K(k);
        }
        return result.Length > 0 ? result : "-";
    }

    static KeyCode Pick(PlayerController p, System.Func<PlayerController, KeyCode> get, KeyCode fallback)
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

        buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 26 };
    }

    void Row(float x, ref float y, float keyWidth, float textWidth, string key, string text)
    {
        GUI.Label(new Rect(x, y, keyWidth, 34f), key, keyStyle);
        GUI.Label(new Rect(x + keyWidth, y, textWidth, 34f), text, textStyle);
        y += 38f;
    }

    void OnGUI()
    {
        GUI.depth = -100;
        if (titleStyle == null) BuildStyles();

        float scale = Screen.height / 720f * guideScale;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float w = Screen.width / scale;
        float h = Screen.height / scale;

        // Hinweis
        if (!open)
        {
            string hint = Keys(toggleKey, toggleButtonPad) + ": Steuerung";
            GUI.Label(new Rect(w - 420f, 10f, 400f, 30f), hint, hintStyle);
            return;
        }

        // Hintergrund
        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, dimAmount);
        GUI.DrawTexture(new Rect(0f, 0f, w, h), white);
        GUI.color = new Color(0.08f, 0.1f, 0.16f, 0.97f);
        Rect panel = new Rect(w / 2f - 510f, (h - 540f) / 2f, 1020f, 540f);
        GUI.DrawTexture(panel, white);
        GUI.color = old;

        GUI.Label(new Rect(panel.x, panel.y + 14f, panel.width, 56f), "Steuerung", titleStyle);

        // Tasten lesen
        KeyCode jump = Pick(player, p => p.jumpKey, KeyCode.W);
        KeyCode dash = Pick(player, p => p.dashKey, KeyCode.CapsLock);
        KeyCode dashAlt = Pick(player, p => p.dashKeyAlt, KeyCode.Q);
        KeyCode slide = Pick(player, p => p.slideKey, KeyCode.S);
        KeyCode slideAlt = Pick(player, p => p.slideKeyAlt, KeyCode.LeftControl);
        KeyCode slideAlt2 = Pick(player, p => p.slideKeyAlt2, KeyCode.LeftShift);
        KeyCode straight = Pick(player, p => p.shootStraightKey, KeyCode.Space);
        KeyCode padJump = Pick(player, p => p.padJumpButton, KeyCode.JoystickButton1);
        KeyCode padDash = Pick(player, p => p.padDashButton, KeyCode.JoystickButton2);
        KeyCode padDashAlt = Pick(player, p => p.padDashAltButton, KeyCode.JoystickButton4);
        KeyCode padSlide = Pick(player, p => p.padSlideButton, KeyCode.JoystickButton3);
        KeyCode padShoot = Pick(player, p => p.padShootButton, KeyCode.JoystickButton0);
        KeyCode padShootAlt = Pick(player, p => p.padShootAltButton, KeyCode.JoystickButton5);
        KeyCode tp = Pick(player, p => p.teleportKey, KeyCode.E);
        KeyCode padTp = Pick(player, p => p.padTeleportButton, KeyCode.JoystickButton7);

        // Tastatur
        float x = panel.x + 30f;
        float y = panel.y + 80f;
        GUI.Label(new Rect(x, y, 440f, 36f), "Tastatur und Maus", headStyle);
        y += 46f;
        Row(x, ref y, 220f, 240f, Keys(jump), "Springen (auch 2x)");
        Row(x, ref y, 220f, 240f, Keys(jump) + " an der Wand", "Wandsprung");
        Row(x, ref y, 220f, 240f, Keys(slide, slideAlt, slideAlt2), "Rutschen (Slide)");
        Row(x, ref y, 220f, 240f, Keys(straight), "Schuss geradeaus");
        Row(x, ref y, 220f, 240f, "Linksklick", "Schuss zur Maus");
        Row(x, ref y, 220f, 240f, "Schuss erneut", "Teleport zum Pfeil");
        Row(x, ref y, 220f, 240f, Keys(tp), "Teleport (extra Taste)");
        Row(x, ref y, 220f, 240f, Keys(dash, dashAlt), "Dash");

        // Controller
        x = panel.x + 480f;
        y = panel.y + 80f;
        GUI.Label(new Rect(x, y, 420f, 36f), "Controller (PS4)", headStyle);
        y += 46f;
        Row(x, ref y, 175f, 350f, Keys(padJump), "Springen (auch 2x)");
        Row(x, ref y, 175f, 350f, Keys(padJump) + " an Wand", "Wandsprung");
        Row(x, ref y, 175f, 350f, Keys(padSlide), "Rutschen (Slide)");
        Row(x, ref y, 175f, 350f, Keys(padShoot, padShootAlt), "Schuss Richtung rechter Stick");
        Row(x, ref y, 175f, 350f, "Schuss erneut", "Teleport zum Pfeil");
        Row(x, ref y, 175f, 350f, Keys(padTp), "Teleport (extra Taste)");
        Row(x, ref y, 175f, 350f, Keys(padDash, padDashAlt), "Dash");

        // Button
        if (GUI.Button(new Rect(w / 2f - 130f, panel.y + 464f, 260f, 54f), "Los geht's", buttonStyle))
            SetOpen(false);
    }
}
