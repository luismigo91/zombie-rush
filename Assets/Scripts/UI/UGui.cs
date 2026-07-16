using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Librería de builders uGUI+TMP para Zombie Rush (mood "noche apocalíptica neón").
/// Reemplaza a UiKit (IMGUI) en la migración a uGUI. Todo se construye por código:
/// Canvas con CanvasScaler para escalado responsive portrait, paneles/botones con
/// Image tintada, texto con TextMeshProUGUI (nítido y escalable) e iconos cuadrados.
///
/// La paleta se reutiliza de UiKit (fuente de verdad visual). Los builders devuelven
/// los componentes para que el llamante los actualice dinámicamente (p. ej. el HUD).
///
/// Escalado: CanvasScaler con referencia 720×1280 y match 0.5 (portrait equilibrado).
/// </summary>
public static class UGui
{
    // ---- Paleta (igual que UiKit) ----
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
    public static readonly Color Dark = new Color(0.08f, 0.08f, 0.11f, 1f);

    public const float RefW = 720f, RefH = 1280f;

    /// <summary>Sprite redondeado cacheado (9-slice) para paneles/botones.</summary>
    static Sprite _rounded;
    public static Sprite Rounded
    {
        get
        {
            if (_rounded == null) _rounded = MakeRounded(32, 8, PanelColor, BorderNeon, 2);
            return _rounded;
        }
    }

    /// <summary>Sprite cuadrado blanco cacheado (iconos/overlays).</summary>
    static Sprite _white;
    public static Sprite White
    {
        get
        {
            if (_white == null)
            {
                var tex = Texture2D.whiteTexture;
                _white = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f), tex.width);
            }
            return _white;
        }
    }

    static TMP_FontAsset _font;
    /// <summary>Font TMP por defecto (de TMP_Settings). Cacheado.</summary>
    public static TMP_FontAsset Font
    {
        get
        {
            if (_font == null) _font = TMP_Settings.defaultFontAsset;
            return _font;
        }
    }

    /// <summary>Crea un Canvas raíz con CanvasScaler portrait y EventSystem si falta.</summary>
    public static Canvas MakeCanvas(string name, int sortOrder = 0)
    {
        var go = new GameObject(name);
        var c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = sortOrder;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(RefW, RefH);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        // Portrait: el ancho es la cota ajustada en móviles alargados (aspect < ref).
        // match=0 fija el canvas a 720 ref de ancho → banner/chips caben; el alto
        // sobrante queda como holgura vertical (anclajes relativos lo absorben).
        scaler.matchWidthOrHeight = 0f;

        go.AddComponent<GraphicRaycaster>();

        // EventSystem es necesario para los botones; lo crea si no existe ninguno.
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
        return c;
    }

    /// <summary>Crea un RectTransform hijo de parent, anclado y expandido según anchors.</summary>
    public static RectTransform Rect(Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = new GameObject("rect");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        return rt;
    }

    /// <summary>
    /// Contenedor a pantalla completa cuyo borde SUPERIOR respeta el safe area del
    /// dispositivo (notch/cámara perforada/barra de estado): todo lo anclado arriba
    /// debe colgar de aquí para no quedar tapado por el hardware. En pantallas sin
    /// recorte el inset es 0 y no cambia nada.
    /// </summary>
    public static RectTransform SafeTopRect(Transform root)
    {
        var rt = Rect(root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        rt.gameObject.name = "SafeTop";

        float insetPx = Screen.height - Screen.safeArea.yMax; // píxeles físicos tapados arriba
        if (insetPx > 0f)
        {
            // Conversión píxeles → unidades del canvas (CanvasScaler escala por ancho).
            var canvasRt = root.GetComponentInParent<Canvas>()?.transform as RectTransform;
            float scale = canvasRt != null && Screen.height > 0
                ? canvasRt.rect.height / Screen.height : 1f;
            rt.offsetMax = new Vector2(0f, -insetPx * scale);
        }
        return rt;
    }

    /// <summary>Añade una Image tintada (sprite redondeado por defecto) a un RectTransform.</summary>
    public static Image AddImage(RectTransform rt, Color color, Sprite sprite = null, bool sliced = true)
    {
        var img = rt.gameObject.AddComponent<UnityEngine.UI.Image>();
        img.sprite = sprite ?? Rounded;
        img.color = color;
        if (sliced && img.sprite != null) img.type = Image.Type.Sliced;
        img.raycastTarget = false;
        return img;
    }

    /// <summary>Añade un TextMeshProUGUI con estilo y outline opcional.</summary>
    public static TextMeshProUGUI Text(RectTransform rt, string content, float fontSize,
        Color color, TextAlignmentOptions align = TextAlignmentOptions.Center,
        bool bold = false, float outline = 0f, Color? outlineColor = null)
    {
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = Font;
        t.text = content;
        t.fontSize = fontSize;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        if (bold) t.fontStyle = FontStyles.Bold;
        if (outline > 0f)
        {
            t.outlineWidth = outline;
            t.outlineColor = outlineColor ?? Color.black;
        }
        // Margen de sombra suave (simula el ShadowLabel de IMGUI).
        return t;
    }

    /// <summary>
    /// Restringe un TMP a UNA sola línea: auto-size hacia abajo (hasta minSize)
    /// para que quepa a lo ancho y, si aun así no cabe, corta con puntos suspensivos.
    /// Evita los solapes verticales que produce el word-wrap en rects estrechos.
    /// </summary>
    public static TextMeshProUGUI FitOneLine(TextMeshProUGUI t, float minSize)
    {
        t.enableAutoSizing = true;
        t.fontSizeMax = t.fontSize;
        t.fontSizeMin = minSize;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Ellipsis;
        return t;
    }

    /// <summary>Añade un efecto de sombra (UnityEngine.UI.Shadow) al componente.</summary>
    public static T WithShadow<T>(T cmp, Color color, Vector2 distance) where T : Component
    {
        var sh = cmp.gameObject.AddComponent<Shadow>();
        sh.effectColor = color;
        sh.effectDistance = distance;
        return cmp;
    }

    /// <summary>Crea un botón uGUI: Image de fondo + texto hijo + componente Button.</summary>
    public static Button Button(RectTransform rt, string label, float fontSize,
        Color bg, Color fg)
    {
        var img = AddImage(rt, bg);
        img.raycastTarget = true;

        // Texto hijo centrado.
        var child = Rect(rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var t = Text(child, label, fontSize, fg, TextAlignmentOptions.Center, bold: true);
        WithShadow(t, new Color(0, 0, 0, 0.5f), new Vector2(2, -2));

        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        // ColorBlock neón: pressed más oscuro, disabled grisáceo.
        var cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        cb.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        cb.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);
        cb.fadeDuration = 0.08f;
        btn.colors = cb;
        return btn;
    }

    // ---- Iconos PNG (Resources/Art/ui, cargados vía ArtCache) ----

    /// <summary>Caché local de sprites de iconos UI (evita concat + lookup en ArtCache por llamada).</summary>
    static readonly Dictionary<string, Sprite> iconSprites = new Dictionary<string, Sprite>();

    /// <summary>
    /// Sprite de icono UI por nombre corto (p. ej. "icon_pause" → Resources/Art/ui/icon_pause).
    /// Devuelve null si el PNG no existe (ArtCache ya loguea el warning una vez).
    /// </summary>
    public static Sprite IconSprite(string name)
    {
        if (iconSprites.TryGetValue(name, out var s)) return s;
        s = ArtCache.Sprite("ui/" + name);
        iconSprites[name] = s;
        return s;
    }

    /// <summary>
    /// Icono PNG tintado ocupando el RectTransform dado (los PNG son monocolor hueso,
    /// tintan bien con Image.color). Si el sprite no existe, cae al icono procedural
    /// (cuadrado con borde neón) para no dejar el hueco vacío.
    /// </summary>
    public static Image Icon(RectTransform parent, string name, Color tint)
    {
        var s = IconSprite(name);
        if (s == null) return Icon(parent, tint); // fallback procedural existente
        var img = AddImage(parent, tint, s, sliced: false);
        img.preserveAspect = true;
        return img;
    }

    /// <summary>Overload sin tinte: icono PNG en blanco (color original hueso).</summary>
    public static Image Icon(RectTransform parent, string name) => Icon(parent, name, Color.white);

    /// <summary>
    /// Botón cuyo contenido es un icono PNG centrado. Si el sprite no existe,
    /// se queda con el texto fallback (mismo aspecto que Button()).
    /// </summary>
    public static Button IconButton(RectTransform rt, string iconName, string fallbackLabel,
        float fontSize, Color bg, Color fg, float iconSize = 44f)
    {
        bool hasIcon = IconSprite(iconName) != null;
        var btn = Button(rt, hasIcon ? "" : fallbackLabel, fontSize, bg, fg);
        if (hasIcon)
        {
            var ic = Rect(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            ic.sizeDelta = new Vector2(iconSize, iconSize);
            Icon(ic, iconName, fg);
        }
        return btn;
    }

    /// <summary>Icono cuadrado: Image blanca tintada con borde neón (Image hija).</summary>
    public static Image Icon(RectTransform parent, Color color, float inset = 3f)
    {
        // Borde neón (padre).
        var border = Rect(parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        border.sizeDelta = parent.rect.size;
        var bImg = AddImage(border, new Color(BorderNeon.r, BorderNeon.g, BorderNeon.b, 0.9f), White, false);

        // Relleno (hija, inset).
        var fill = Rect(border, Vector2.zero, Vector2.one, new Vector2(inset, inset), new Vector2(-inset, -inset));
        var fImg = AddImage(fill, color, White, false);
        return fImg;
    }

    /// <summary>Barra de progreso: fondo + relleno (Image) que se escala con fillAmount.</summary>
    public static Image ProgressBar(RectTransform parent, Color bgColor, Color fillColor)
    {
        var bg = Rect(parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        AddImage(bg, bgColor, Rounded);

        var fill = Rect(bg, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        var fImg = AddImage(fill, fillColor, White, false);
        fImg.fillMethod = Image.FillMethod.Horizontal;
        fImg.fillAmount = 0f;
        fImg.type = Image.Type.Filled;
        return fImg;
    }

    /// <summary>Genera un sprite redondeado 9-slice con borde y relleno tintados.</summary>
    static Sprite MakeRounded(int size, int radius, Color fill, Color border, int borderW)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var px = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Distancia al borde del cuadrado.
                int dx = Mathf.Min(x, size - 1 - x);
                int dy = Mathf.Min(y, size - 1 - y);
                int dist = Mathf.Min(dx, dy);

                // Esquinas redondeadas: fuera del radio → transparente.
                bool inCorner = dx < radius && dy < radius;
                float cd = Mathf.Sqrt((radius - dx) * (radius - dx) + (radius - dy) * (radius - dy));
                bool outsideCorner = inCorner && cd > radius;

                Color c;
                if (outsideCorner) c = new Color(0, 0, 0, 0);
                else if (dist < borderW) c = border;
                else c = fill;
                px[y * size + x] = c;
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        var spr = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size,
            extrude: 0, meshType: SpriteMeshType.FullRect,
            border: new Vector4(radius, radius, radius, radius));
        return spr;
    }

    /// <summary>Ancla un RectTransform a una posición relativa del padre (stretch o fijo).</summary>
    public static RectTransform Anchor(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
    {
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    /// <summary>Posiciona un RectTransform anclado a un punto con tamaño fijo (centro).</summary>
    public static RectTransform Size(RectTransform rt, Vector2 size)
    {
        rt.sizeDelta = size;
        return rt;
    }

    public static Color Hex(string hex) =>
        ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
}
