using UnityEngine;

/// <summary>
/// HUD de la partida (Zombie Rush) con IMGUI (OnGUI) y el look neón de UiKit. Muestra
/// chips de Unidades / Nivel / Monedas con iconos, una barra de progreso del nivel con
/// marcador de jefe, un botón de PAUSA, y las pantallas pulidas de VICTORIA y DERROTA.
///
/// Incluye un tutorial sutil "Arrastra para mover" la primera vez que se juega
/// (PlayerPrefs "seen_tutorial"), que desaparece al primer arrastre o tras unos
/// segundos. Conserva FlashDamage() y el destello rojo de daño (los usa código dormante).
///
/// Provisional sobre IMGUI; en pulido se migrará a uGUI.
/// </summary>
public class Hud : MonoBehaviour
{
    const string SeenTutorialKey = "seen_tutorial";
    const float TutorialTimeout = 4f;

    static float damageFlash;

    // Estado de pantalla final (para disparar confeti una sola vez).
    bool confettiFired;

    // Estado del tutorial.
    bool tutorialActive;
    bool tutorialChecked;
    float tutorialTimer;

    /// <summary>Destello rojo de pantalla (lo conserva PlayerController dormante).</summary>
    public static void FlashDamage() => damageFlash = 0.25f;

    void Start()
    {
        // El tutorial solo aparece la primera vez (flag persistente).
        tutorialActive = PlayerPrefs.GetInt(SeenTutorialKey, 0) == 0;
        tutorialChecked = true;
        tutorialTimer = 0f;
    }

    void Update()
    {
        if (damageFlash > 0f) damageFlash -= Time.deltaTime;

        if (tutorialActive)
            UpdateTutorial();
    }

    /// <summary>Cierra el tutorial al detectar el primer arrastre/toque o por timeout.</summary>
    void UpdateTutorial()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;

        tutorialTimer += Time.deltaTime;

        bool touched = Input.GetMouseButton(0) || Input.touchCount > 0;
        if (touched || tutorialTimer >= TutorialTimeout)
            DismissTutorial();
    }

    void DismissTutorial()
    {
        tutorialActive = false;
        PlayerPrefs.SetInt(SeenTutorialKey, 1);
        PlayerPrefs.Save();
    }

    void OnGUI()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        UiKit.Init();
        float h = Screen.height, w = Screen.width, u = UiKit.U;

        // --- Destello de daño (conservado) ---
        if (damageFlash > 0f)
        {
            GUI.color = new Color(1f, 0f, 0f, 0.4f * Mathf.Clamp01(damageFlash / 0.25f));
            GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // En pantallas finales el HUD de juego se oculta tras el overlay.
        if (gm.State == GameState.GameOver)
        {
            DrawDefeatScreen(gm, w, h, u);
            return;
        }
        if (gm.State == GameState.Won)
        {
            DrawVictoryScreen(gm, w, h, u);
            return;
        }

        DrawTopBar(gm, w, u);
        DrawProgress(gm, w, u);
        DrawPauseButton(w, u);

        if (tutorialActive)
            DrawTutorial(w, h, u);
    }

    // ===================================================================
    //  HUD EN JUEGO
    // ===================================================================

    /// <summary>Chips superiores: unidades, nivel y monedas de la run, con iconos.</summary>
    void DrawTopBar(GameManager gm, float w, float u)
    {
        int count = gm.Squad != null ? gm.Squad.Count : 0;
        float y = 16 * u;
        float chipH = 64 * u;

        // Chip Unidades (izquierda).
        Chip(new Rect(16 * u, y, 250 * u, chipH), UiKit.Lime, $"{count}", u);
        // Chip Nivel (centro-izquierda).
        Chip(new Rect(16 * u + 262 * u, y, 230 * u, chipH), UiKit.CyanNeon, $"Nv {gm.Level}", u);

        // Chip Monedas de la run (derecha). gm.Coins es el contador de la run.
        int coins = gm.Coins;
        if (coins >= 0)
        {
            float cw = 220 * u;
            Chip(new Rect(w - cw - 16 * u, y, cw, chipH), UiKit.Gold, $"{coins}", u);
        }
    }

    /// <summary>Dibuja un chip: panel neón + icono de color + texto.</summary>
    void Chip(Rect r, Color iconColor, string text, float u)
    {
        UiKit.Panel(r);
        float pad = r.height * 0.22f;
        float iconSz = r.height - pad * 2f;
        var iconRect = new Rect(r.x + pad, r.y + pad, iconSz, iconSz);
        UiKit.Icon(iconRect, iconColor);

        var textRect = new Rect(iconRect.xMax + 10 * u, r.y, r.width - iconSz - pad * 2f - 12 * u, r.height);
        var style = UiKit.StyleLabel(u);
        var prev = style.alignment;
        style.alignment = TextAnchor.MiddleLeft;
        UiKit.ShadowLabel(textRect, text, style);
        style.alignment = prev;
    }

    /// <summary>Barra de progreso del nivel con marcador de jefe en niveles múltiplo de 10.</summary>
    void DrawProgress(GameManager gm, float w, float u)
    {
        float barW = w * 0.62f, barH = 22 * u;
        float bx = (w - barW) * 0.5f, by = 96 * u;
        bool boss = gm.Level % 10 == 0;
        UiKit.ProgressBar(new Rect(bx, by, barW, barH), gm.LevelProgress, boss);
    }

    /// <summary>Botón de PAUSA en la esquina superior derecha (icono "||").</summary>
    void DrawPauseButton(float w, float u)
    {
        float sz = 78 * u;
        var r = new Rect(w - sz - 16 * u, 96 * u, sz, sz);
        if (UiKit.Button(r, "II"))
            PauseMenu.Show();
    }

    // ===================================================================
    //  TUTORIAL
    // ===================================================================

    /// <summary>Overlay sutil "Arrastra para mover" con panel translúcido pulsante.</summary>
    void DrawTutorial(float w, float h, float u)
    {
        float pw = w * 0.6f, ph = 130 * u;
        var r = new Rect((w - pw) * 0.5f, h * 0.62f, pw, ph);

        // Pulso de opacidad (Time.unscaledTime para que lata aunque pausen).
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f);
        GUI.color = new Color(1f, 1f, 1f, 0.7f + 0.3f * pulse);
        UiKit.Panel(r);
        GUI.color = Color.white;

        // Icono de "mano/flecha" (flecha doble horizontal) a la izquierda del texto.
        float handSz = ph * 0.4f;
        var hand = new Rect(r.x + 26 * u, r.center.y - handSz * 0.5f, handSz, handSz);
        DrawDragArrow(hand);

        var textRect = new Rect(r.x + handSz + 40 * u, r.y, r.width - handSz - 50 * u, r.height);
        var style = UiKit.StyleBody(u);
        var prevA = style.alignment;
        style.alignment = TextAnchor.MiddleLeft;
        UiKit.ShadowLabel(textRect, "Arrastra para mover", style);
        style.alignment = prevA;
    }

    /// <summary>Flecha doble horizontal (sugiere arrastre lateral) dibujada con cuadros.</summary>
    void DrawDragArrow(Rect r)
    {
        GUI.color = UiKit.CyanNeon;
        // Barra central.
        float barH = r.height * 0.18f;
        GUI.DrawTexture(new Rect(r.x, r.center.y - barH * 0.5f, r.width, barH), Texture2D.whiteTexture);
        // Puntas (cuadros en los extremos).
        float tip = r.height * 0.5f;
        GUI.DrawTexture(new Rect(r.x - tip * 0.2f, r.center.y - tip * 0.5f, tip, tip), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(r.xMax - tip * 0.8f, r.center.y - tip * 0.5f, tip, tip), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    // ===================================================================
    //  PANTALLAS FINALES
    // ===================================================================

    /// <summary>VICTORIA: "NIVEL SUPERADO", estrellas, recompensa, confeti y botones.</summary>
    void DrawVictoryScreen(GameManager gm, float w, float h, float u)
    {
        Overlay(w, h);

        if (!confettiFired)
        {
            confettiFired = true;
            Vfx.Confetti(new Vector3(0f, 0f, 0f));
        }

        float pw = w * 0.82f, ph = h * 0.5f;
        var panel = new Rect((w - pw) * 0.5f, (h - ph) * 0.5f, pw, ph);
        UiKit.Panel(panel);

        var title = UiKit.StyleHeader(u);
        var prev = title.normal.textColor;
        title.normal.textColor = UiKit.Lime;
        UiKit.ShadowLabel(new Rect(panel.x, panel.y + 30 * u, panel.width, 80 * u), "NIVEL SUPERADO", title);
        title.normal.textColor = prev;

        // Tres estrellas.
        float starSz = 64 * u;
        float totalW = starSz * 3f + 20 * u * 2f;
        float sx = panel.center.x - totalW * 0.5f;
        float sy = panel.y + 120 * u;
        for (int i = 0; i < 3; i++)
            UiKit.Star(new Rect(sx + i * (starSz + 20 * u), sy, starSz, starSz), UiKit.Gold);

        // Recompensa de monedas de la run.
        var reward = UiKit.StyleBody(u);
        var prevR = reward.normal.textColor;
        reward.normal.textColor = UiKit.Gold;
        UiKit.ShadowLabel(new Rect(panel.x, sy + starSz + 18 * u, panel.width, 50 * u), $"+{gm.Coins} monedas", reward);
        reward.normal.textColor = prevR;

        DrawEndButtons(gm, panel, u, "SIGUIENTE NIVEL");
    }

    /// <summary>DERROTA: "DERROTA", monedas conseguidas y botones reintentar / menú.</summary>
    void DrawDefeatScreen(GameManager gm, float w, float h, float u)
    {
        Overlay(w, h);

        float pw = w * 0.82f, ph = h * 0.46f;
        var panel = new Rect((w - pw) * 0.5f, (h - ph) * 0.5f, pw, ph);
        UiKit.Panel(panel);

        var title = UiKit.StyleHeader(u);
        var prev = title.normal.textColor;
        title.normal.textColor = UiKit.GateBad;
        UiKit.ShadowLabel(new Rect(panel.x, panel.y + 34 * u, panel.width, 80 * u), "DERROTA", title);
        title.normal.textColor = prev;

        UiKit.ShadowLabel(new Rect(panel.x, panel.y + 130 * u, panel.width, 50 * u),
            $"Monedas conseguidas: {gm.Coins}", UiKit.StyleBody(u));

        DrawEndButtons(gm, panel, u, "REINTENTAR");
    }

    /// <summary>Par de botones inferiores de las pantallas finales (continuar / menú).</summary>
    void DrawEndButtons(GameManager gm, Rect panel, float u, string continueLabel)
    {
        float bw = panel.width * 0.74f, bh = 96 * u, gap = 18 * u;
        float bx = panel.center.x - bw * 0.5f;
        float by = panel.yMax - (bh * 2f + gap + 28 * u);

        if (UiKit.Button(new Rect(bx, by, bw, bh), continueLabel))
            gm.Restart();
        if (UiKit.Button(new Rect(bx, by + bh + gap, bw, bh), "MENÚ"))
            gm.GoToMenu();
    }

    /// <summary>Oscurece toda la pantalla (fondo de las pantallas finales).</summary>
    void Overlay(float w, float h)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.62f);
        GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}
