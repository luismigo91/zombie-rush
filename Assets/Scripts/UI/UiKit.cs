using UnityEngine;

/// <summary>
/// Biblioteca de "look" IMGUI por código para Zombie Rush (mood "noche apocalíptica
/// neón" de la biblia). Genera y CACHEA todas las Texture2D y GUIStyles una sola vez
/// (nunca dentro de OnGUI) y expone helpers de dibujo: panel neón, botón con estados,
/// barra de progreso, iconos, banner de título y labels con sombra.
///
/// Es la fuente de verdad visual de toda la UI (Hud, MenuUI, PauseMenu). El color de
/// la paleta se parsea una vez desde los hex de la biblia.
///
/// Provisional sobre IMGUI; en pulido se migrará a uGUI.
/// </summary>
public static class UiKit
{
    // ---- Paleta de la biblia (parseada una vez) ----
    public static readonly Color Sky1 = Hex("#14122A");
    public static readonly Color Sky2 = Hex("#241A3A");
    public static readonly Color PanelColor = Hex("#1A1830");
    public static readonly Color BorderNeon = Hex("#3DD6F5");
    public static readonly Color CyanNeon = Hex("#3DD6F5");
    public static readonly Color Magenta = Hex("#FF4D8D");
    public static readonly Color Bone = Hex("#F4F1E8");
    public static readonly Color GateGood = Hex("#3DE0C8");
    public static readonly Color GateBad = Hex("#FF5A3C");
    public static readonly Color Gold = Hex("#FFD23A");
    public static readonly Color Lime = Hex("#5BD66A");
    public static readonly Color GunGray = Hex("#2E2E36");
    public static readonly Color ZombieRed = Hex("#FF3B3B");

    // ---- Texturas cacheadas (generadas por código) ----
    static GUIStyle _sliced; // estilo "9-slice" cacheado (evita GC churn en OnGUI)
    static Texture2D panelTex;
    static Texture2D buttonTex;
    static Texture2D buttonPressedTex;
    static Texture2D buttonDisabledTex;
    static Texture2D barBackTex;
    static Texture2D barFillTex;
    static Texture2D softWhite;
    static bool texturesReady;

    // ---- GUIStyles cacheados (recreados solo si cambia el fontSize base) ----
    static GUIStyle title, header, body, label, labelMuted, button, buttonSmall;
    static int cachedFontBase = -1;

    // ---- Estilos de fuente reutilizables por tamaño (cache simple) ----

    /// <summary>Factor de escala global (misma base que el código original: alto/1280).</summary>
    public static float U => Screen.height / 1280f;

    /// <summary>Idempotente. Crea texturas y estilos si aún no existen. Lo puede llamar OnGUI.</summary>
    public static void Init()
    {
        EnsureTextures();
        EnsureStyles();
    }

    // ===================================================================
    //  GENERACIÓN DE TEXTURAS (una sola vez)
    // ===================================================================

    static void EnsureTextures()
    {
        if (texturesReady && panelTex != null) return;
        texturesReady = true;

        softWhite = SolidTex(1, 1, Color.white);

        panelTex = BuildPanelTex();
        buttonTex = BuildButtonTex(false, false);
        buttonPressedTex = BuildButtonTex(true, false);
        buttonDisabledTex = BuildButtonTex(false, true);
        barBackTex = BuildBarBackTex();
        barFillTex = BuildBarFillTex();
    }

    /// <summary>Textura 1x1 sólida (para tintes/overlays sin Texture2D.whiteTexture).</summary>
    static Texture2D SolidTex(int w, int h, Color c)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        var px = new Color[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = c;
        t.SetPixels(px);
        t.Apply();
        return t;
    }

    /// <summary>
    /// Panel neón "9-slice-like": relleno translúcido + glow interior suave + borde
    /// de 2px en cian. Se dibuja con GUIStyle.border para que el borde no se deforme.
    /// </summary>
    static Texture2D BuildPanelTex()
    {
        const int s = 32;
        const int b = 2; // grosor del borde
        var t = new Texture2D(s, s, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };

        var fill = new Color(PanelColor.r, PanelColor.g, PanelColor.b, 0.92f);
        var glow = new Color(BorderNeon.r, BorderNeon.g, BorderNeon.b, 0.10f);
        var border = new Color(BorderNeon.r, BorderNeon.g, BorderNeon.b, 1f);

        var px = new Color[s * s];
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                int dist = Mathf.Min(Mathf.Min(x, s - 1 - x), Mathf.Min(y, s - 1 - y));
                Color c;
                if (dist < b) c = border;                  // borde neón
                else if (dist < b + 3) c = glow;           // glow interior junto al borde
                else c = fill;                             // relleno
                px[y * s + x] = c;
            }
        }
        t.SetPixels(px);
        t.Apply();
        return t;
    }

    /// <summary>
    /// Botón: gradiente vertical cian->cian oscuro con borde. La versión pressed va
    /// más oscura/hundida; la disabled, desaturada.
    /// </summary>
    static Texture2D BuildButtonTex(bool pressed, bool disabled)
    {
        const int s = 32;
        const int b = 2;
        var t = new Texture2D(s, s, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };

        // Tonos base del gradiente (arriba claro, abajo oscuro).
        Color top = new Color(0.24f, 0.62f, 0.78f, 1f);
        Color bot = new Color(0.10f, 0.30f, 0.42f, 1f);
        Color border = BorderNeon;

        if (pressed)
        {
            // Hundido: invierte y oscurece el gradiente.
            top = new Color(0.08f, 0.24f, 0.34f, 1f);
            bot = new Color(0.16f, 0.42f, 0.54f, 1f);
            border = new Color(BorderNeon.r * 0.7f, BorderNeon.g * 0.7f, BorderNeon.b * 0.7f, 1f);
        }
        if (disabled)
        {
            // Desaturado/grisáceo y semitransparente.
            top = new Color(0.22f, 0.24f, 0.28f, 0.85f);
            bot = new Color(0.12f, 0.13f, 0.16f, 0.85f);
            border = new Color(0.40f, 0.42f, 0.48f, 0.85f);
        }

        var px = new Color[s * s];
        for (int y = 0; y < s; y++)
        {
            // y=0 es la fila inferior en SetPixels: interpolamos del fondo al techo.
            float ty = (float)y / (s - 1);
            Color grad = Color.Lerp(bot, top, ty);
            for (int x = 0; x < s; x++)
            {
                int dist = Mathf.Min(Mathf.Min(x, s - 1 - x), Mathf.Min(y, s - 1 - y));
                px[y * s + x] = dist < b ? border : grad;
            }
        }
        t.SetPixels(px);
        t.Apply();
        return t;
    }

    /// <summary>Surco de barra: oscuro con borde tenue.</summary>
    static Texture2D BuildBarBackTex()
    {
        const int s = 16;
        const int b = 1;
        var t = new Texture2D(s, s, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };

        var fill = new Color(0.04f, 0.05f, 0.10f, 0.9f);
        var border = new Color(BorderNeon.r, BorderNeon.g, BorderNeon.b, 0.5f);

        var px = new Color[s * s];
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                int dist = Mathf.Min(Mathf.Min(x, s - 1 - x), Mathf.Min(y, s - 1 - y));
                px[y * s + x] = dist < b ? border : fill;
            }
        t.SetPixels(px);
        t.Apply();
        return t;
    }

    /// <summary>Relleno de barra: gradiente horizontal cian->magenta con brillo arriba.</summary>
    static Texture2D BuildBarFillTex()
    {
        const int w = 32, h = 8;
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false)
        { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };

        var px = new Color[w * h];
        for (int x = 0; x < w; x++)
        {
            float tx = (float)x / (w - 1);
            Color baseC = Color.Lerp(CyanNeon, Magenta, tx);
            for (int y = 0; y < h; y++)
            {
                // Brillo en la fila superior (sensación de relieve).
                float ty = (float)y / (h - 1);
                Color c = Color.Lerp(baseC * 0.82f, Color.Lerp(baseC, Color.white, 0.35f), ty);
                c.a = 1f;
                px[y * w + x] = c;
            }
        }
        t.SetPixels(px);
        t.Apply();
        return t;
    }

    // ===================================================================
    //  ESTILOS
    // ===================================================================

    static void EnsureStyles()
    {
        int fontBase = Mathf.Max(1, Mathf.RoundToInt(32 * U));
        if (title != null && fontBase == cachedFontBase) return;
        cachedFontBase = fontBase;

        float u = U;

        title = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(70 * u),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = false
        };
        title.normal.textColor = Bone;

        header = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(40 * u),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        header.normal.textColor = CyanNeon;

        body = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(34 * u),
            alignment = TextAnchor.MiddleCenter
        };
        body.normal.textColor = Bone;

        label = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(32 * u),
            alignment = TextAnchor.MiddleLeft
        };
        label.normal.textColor = Bone;

        labelMuted = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(26 * u),
            alignment = TextAnchor.MiddleLeft
        };
        labelMuted.normal.textColor = new Color(Bone.r, Bone.g, Bone.b, 0.65f);

        button = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(40 * u),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        button.normal.textColor = Bone;

        buttonSmall = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(28 * u),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        buttonSmall.normal.textColor = Bone;
    }

    /// <summary>Estilo de título grande (Bone, bold, centrado).</summary>
    public static GUIStyle StyleTitle(float u) { EnsureStyles(); return title; }
    /// <summary>Estilo de cabecera de sección (cian, bold, centrado).</summary>
    public static GUIStyle StyleHeader(float u) { EnsureStyles(); return header; }
    /// <summary>Estilo de texto de cuerpo (Bone, centrado).</summary>
    public static GUIStyle StyleBody(float u) { EnsureStyles(); return body; }
    /// <summary>Estilo de etiqueta a la izquierda (Bone).</summary>
    public static GUIStyle StyleLabel(float u) { EnsureStyles(); return label; }
    /// <summary>Estilo de etiqueta atenuada (Bone translúcido).</summary>
    public static GUIStyle StyleLabelMuted(float u) { EnsureStyles(); return labelMuted; }
    /// <summary>Estilo de texto de botón grande (Bone, bold).</summary>
    public static GUIStyle StyleButton(float u) { EnsureStyles(); return button; }
    /// <summary>Estilo de texto de botón pequeño (Bone, bold).</summary>
    public static GUIStyle StyleButtonSmall(float u) { EnsureStyles(); return buttonSmall; }

    // ===================================================================
    //  HELPERS DE DIBUJO
    // ===================================================================

    /// <summary>Dibuja un panel neón (relleno translúcido + borde cian sin deformar).</summary>
    public static void Panel(Rect r)
    {
        Init();
        DrawSliced(r, panelTex);
    }

    /// <summary>
    /// Botón grande con estados pressed/disabled. Si enabled y se suelta dentro del
    /// rect, reproduce Sfx.Click + Haptics.Light y devuelve true.
    /// </summary>
    public static bool Button(Rect r, string label, bool enabled = true)
    {
        Init();

        Event e = Event.current;
        bool hover = r.Contains(e.mousePosition);
        bool pressedNow = enabled && hover && e.type == EventType.MouseDown;

        // Estado visual: hundido mientras se mantiene pulsado sobre el botón.
        bool held = enabled && hover &&
                    (e.type == EventType.MouseDrag || e.type == EventType.MouseDown);

        Texture2D tex = !enabled ? buttonDisabledTex : (held ? buttonPressedTex : buttonTex);
        DrawSliced(r, tex);

        // Texto (con leve hundimiento al pulsar).
        var style = button;
        Rect textRect = held ? new Rect(r.x, r.y + 2f, r.width, r.height) : r;
        var prev = style.normal.textColor;
        if (!enabled) style.normal.textColor = new Color(Bone.r, Bone.g, Bone.b, 0.5f);
        ShadowLabel(textRect, label, style);
        style.normal.textColor = prev;

        // Detección de click: soltar el ratón dentro del rect.
        bool clicked = false;
        if (enabled && e.type == EventType.MouseUp && hover)
        {
            clicked = true;
            Sfx.Click();
            Haptics.Light();
            e.Use();
        }
        if (pressedNow) e.Use();
        return clicked;
    }

    /// <summary>
    /// Barra de progreso del nivel: surco + relleno cian->magenta. Si showBossMarker,
    /// pinta un icono de calavera al final (nivel de jefe).
    /// </summary>
    public static void ProgressBar(Rect r, float t01, bool showBossMarker = false)
    {
        Init();
        t01 = Mathf.Clamp01(t01);

        DrawSliced(r, barBackTex);

        // Relleno con pequeño padding interior para que se vea el surco.
        float pad = Mathf.Max(2f, r.height * 0.18f);
        var inner = new Rect(r.x + pad, r.y + pad, (r.width - pad * 2f) * t01, r.height - pad * 2f);
        if (inner.width > 0.5f)
        {
            GUI.color = Color.white;
            GUI.DrawTexture(inner, barFillTex, ScaleMode.StretchToFill, true);
        }

        if (showBossMarker)
        {
            // Calavera simple al final de la barra.
            float sz = r.height * 1.6f;
            var mr = new Rect(r.xMax - sz * 0.5f, r.y + (r.height - sz) * 0.5f, sz, sz);
            Skull(mr);
        }
    }

    /// <summary>Dibuja un label con el estilo dado.</summary>
    public static void Label(Rect r, string text, GUIStyle style)
    {
        GUI.Label(r, text, style);
    }

    /// <summary>Dibuja un label con sombra (negro translúcido 2px abajo-derecha).</summary>
    public static void ShadowLabel(Rect r, string text, GUIStyle style)
    {
        if (string.IsNullOrEmpty(text)) return;
        var prev = style.normal.textColor;
        float off = Mathf.Max(1.5f, 2f * U);

        style.normal.textColor = new Color(0f, 0f, 0f, 0.55f);
        GUI.Label(new Rect(r.x + off, r.y + off, r.width, r.height), text, style);

        style.normal.textColor = prev;
        GUI.Label(r, text, style);
    }

    /// <summary>Icono simple: cuadro de color con borde neón (soldado=Lime, moneda=Gold, etc.).</summary>
    public static void Icon(Rect r, Color color)
    {
        Init();
        // Borde
        GUI.color = new Color(BorderNeon.r, BorderNeon.g, BorderNeon.b, 0.9f);
        GUI.DrawTexture(r, softWhite);
        // Relleno
        float b = Mathf.Max(1.5f, r.height * 0.12f);
        GUI.color = color;
        GUI.DrawTexture(new Rect(r.x + b, r.y + b, r.width - b * 2f, r.height - b * 2f), softWhite);
        GUI.color = Color.white;
    }

    /// <summary>
    /// Banner del título con glow/sombra y subrayado neón. Usa el estilo de título a
    /// gran tamaño; dibuja un halo cian detrás y una línea cian debajo.
    /// </summary>
    public static void TitleBanner(Rect r, string text)
    {
        Init();
        var style = title;

        // Halo cian detrás (varias copias desplazadas, muy translúcidas).
        var prev = style.normal.textColor;
        style.normal.textColor = new Color(CyanNeon.r, CyanNeon.g, CyanNeon.b, 0.18f);
        float g = Mathf.Max(2f, 4f * U);
        GUI.Label(new Rect(r.x - g, r.y, r.width, r.height), text, style);
        GUI.Label(new Rect(r.x + g, r.y, r.width, r.height), text, style);
        GUI.Label(new Rect(r.x, r.y - g, r.width, r.height), text, style);
        GUI.Label(new Rect(r.x, r.y + g, r.width, r.height), text, style);

        // Sombra + texto principal en hueso.
        style.normal.textColor = new Color(0f, 0f, 0f, 0.5f);
        GUI.Label(new Rect(r.x + g * 0.5f, r.y + g * 0.5f, r.width, r.height), text, style);
        style.normal.textColor = Bone;
        GUI.Label(r, text, style);
        style.normal.textColor = prev;

        // Subrayado neón.
        float lineW = r.width * 0.42f;
        float lineH = Mathf.Max(2f, 4f * U);
        var line = new Rect(r.center.x - lineW * 0.5f, r.yMax - lineH * 2f, lineW, lineH);
        GUI.color = CyanNeon;
        GUI.DrawTexture(line, softWhite);
        GUI.color = Color.white;
    }

    /// <summary>Dibuja una estrella sencilla (rombo + barra) de relleno 'color'.</summary>
    public static void Star(Rect r, Color color)
    {
        // Aproximación pixel: una cruz/rombo de cuadros para sugerir una estrella.
        GUI.color = color;
        float cx = r.center.x, cy = r.center.y;
        float arm = r.width * 0.5f;
        float th = r.height * 0.22f;
        // Brazo horizontal
        GUI.DrawTexture(new Rect(cx - arm, cy - th * 0.5f, arm * 2f, th), softWhite);
        // Brazo vertical
        GUI.DrawTexture(new Rect(cx - th * 0.5f, cy - arm, th, arm * 2f), softWhite);
        // Núcleo
        float core = r.width * 0.5f;
        GUI.DrawTexture(new Rect(cx - core * 0.5f, cy - core * 0.5f, core, core), softWhite);
        GUI.color = Color.white;
    }

    // ===================================================================
    //  INTERNOS
    // ===================================================================

    /// <summary>Dibuja una textura con borde "slice" (4px) para que escale sin deformarse.</summary>
    static void DrawSliced(Rect r, Texture2D tex)
    {
        if (tex == null) return;
        GUI.color = Color.white;
        // GUIStyle con border permite respetar las esquinas al estirar. Cacheado:
        // lo llaman Panel/Button en cada OnGUI; crearlo por frame generaba GC churn.
        if (_sliced == null)
            _sliced = new GUIStyle { border = new RectOffset(6, 6, 6, 6) };
        _sliced.normal.background = tex;
        _sliced.Draw(r, GUIContent.none, false, false, false, false);
    }

    /// <summary>Calavera mínima (icono de jefe) dibujada con cuadros.</summary>
    static void Skull(Rect r)
    {
        GUI.color = Bone;
        // Cráneo
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, r.height * 0.7f), softWhite);
        // Mandíbula
        GUI.DrawTexture(new Rect(r.x + r.width * 0.2f, r.y + r.height * 0.65f, r.width * 0.6f, r.height * 0.3f), softWhite);
        // Cuencas (rojas)
        GUI.color = ZombieRed;
        float ew = r.width * 0.22f, eh = r.height * 0.22f;
        GUI.DrawTexture(new Rect(r.x + r.width * 0.18f, r.y + r.height * 0.2f, ew, eh), softWhite);
        GUI.DrawTexture(new Rect(r.x + r.width * 0.6f, r.y + r.height * 0.2f, ew, eh), softWhite);
        GUI.color = Color.white;
    }

    /// <summary>Parsea un color hex "#RRGGBB" (con alfa opcional). Fallback magenta visible.</summary>
    public static Color Hex(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out var c)) return c;
        return Color.magenta;
    }
}
