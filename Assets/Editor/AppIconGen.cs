#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// Genera por código el ICONO de la app y configura el SPLASH y el versionado,
/// sin escribir assets binarios al disco (todo va directo a PlayerSettings).
///
/// El icono usa la paleta "noche apocalíptica neón": fondo degradado vertical
/// (#14122A → #241A3A) con viñeta oscura, una silueta de zombie (cabeza disco,
/// orejas, ojos rojos #FF3B3B) recortada por una mira de francotirador (anillo
/// cian neón #3DD6F5 + cruz + marcas). Se aplican iconos legacy y adaptivos de
/// Android. El splash queda con color de fondo de la paleta y, si la licencia lo
/// permite, sin el logo de Unity. AppIconGen.Apply() orquesta todo y es
/// idempotente (se puede llamar en cada build).
/// </summary>
public static class AppIconGen
{
    // --- Paleta (los mismos colores que el resto del juego) ---
    static readonly Color SkyTop = Hex("#14122A");
    static readonly Color SkyBottom = Hex("#241A3A");
    static readonly Color Neon = Hex("#3DD6F5");   // cian neón (mira)
    static readonly Color EyeRed = Hex("#FF3B3B");   // ojos zombie
    static readonly Color ZombieBody = Hex("#7FB04E");   // verde enfermizo
    static readonly Color ZombieShade = Hex("#4E7330");   // sombra del cuerpo
    static readonly Color Outline = Hex("#0C2A14");   // contorno oscuro

    /// <summary>
    /// Genera y asigna icono (legacy + adaptivo), configura el splash de la
    /// paleta y fija bundleVersion/bundleVersionCode. Robusto e idempotente.
    /// </summary>
    public static void Apply()
    {
        ApplyIcons();
        ApplySplash();
        ApplyVersion();
        Debug.Log("AppIconGen: icono, splash y versión aplicados.");
    }

    // ---------------------------------------------------------------------
    // ICONOS
    // ---------------------------------------------------------------------

    static void ApplyIcons()
    {
        // Textura base nítida a 512: el icono completo (fondo + silueta + mira).
        Texture2D iconFull = BuildIconTexture(512, withBackground: true);

        // Set legacy: tamaños estándar de Android, escalados desde la base.
        int[] legacySizes = { 192, 144, 96, 72, 48, 36 };
        var legacy = new Texture2D[legacySizes.Length];
        for (int i = 0; i < legacySizes.Length; i++)
            legacy[i] = ScaleTexture(iconFull, legacySizes[i]);

        try
        {
            PlayerSettings.SetIcons(NamedBuildTarget.Android, legacy, IconKind.Application);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("AppIconGen: no se pudieron fijar iconos legacy: " + e.Message);
        }

        // Iconos ADAPTIVOS: background = degradado plano, foreground = silueta+mira.
        TryApplyAdaptiveIcons();
    }

    /// <summary>
    /// Intenta fijar iconos adaptivos de Android (foreground + background). El tipo
    /// AndroidPlatformIconKind vive en el módulo de Android, que puede no estar
    /// referenciado en tiempo de compilación; por eso se usa REFLECTION: si la API
    /// no está disponible, no rompe el build y los iconos legacy ya cubren el caso.
    /// </summary>
    static void TryApplyAdaptiveIcons()
    {
        try
        {
            // AndroidPlatformIconKind.Adaptive (enum del módulo Android).
            Type kindType = FindType("UnityEditor.Android.AndroidPlatformIconKind")
                            ?? FindType("AndroidPlatformIconKind");
            if (kindType == null) return; // sin soporte Android cargado: solo legacy

            object adaptive;
            try { adaptive = Enum.Parse(kindType, "Adaptive"); }
            catch { return; }

            // Busca los overloads de Get/SetPlatformIcons que aceptan
            // NamedBuildTarget (evita la variante obsoleta con BuildTargetGroup).
            MethodInfo getIcons = FindPlayerSettingsIconMethod("GetPlatformIcons", 2);
            MethodInfo setIcons = FindPlayerSettingsIconMethod("SetPlatformIcons", 3);
            if (getIcons == null || setIcons == null) return;

            var iconsObj = getIcons.Invoke(null, new object[] { NamedBuildTarget.Android, adaptive }) as Array;
            if (iconsObj == null || iconsObj.Length == 0) return;

            // Capas reutilizables a 512 (se reescalan por slot/capa).
            Texture2D background = BuildBackgroundTexture(512);                 // degradado opaco
            Texture2D foreground = BuildIconTexture(512, withBackground: false); // silueta+mira con alpha

            Type iconType = iconsObj.GetValue(0).GetType();
            PropertyInfo maxLayersProp = iconType.GetProperty("maxLayerCount");
            MethodInfo getLayerWidth = iconType.GetMethod("GetLayerWidth", new[] { typeof(int) });
            MethodInfo setTexture = iconType.GetMethod("SetTexture", new[] { typeof(Texture2D), typeof(int) });
            if (setTexture == null) return;

            for (int i = 0; i < iconsObj.Length; i++)
            {
                object slot = iconsObj.GetValue(i);
                int maxLayers = maxLayersProp != null ? (int)maxLayersProp.GetValue(slot) : 1;

                int fgSize = LayerSize(getLayerWidth, slot, 0);
                setTexture.Invoke(slot, new object[] { ScaleTexture(foreground, fgSize), 0 });

                if (maxLayers > 1)
                {
                    int bgSize = LayerSize(getLayerWidth, slot, 1);
                    setTexture.Invoke(slot, new object[] { ScaleTexture(background, bgSize), 1 });
                }
            }

            setIcons.Invoke(null, new object[] { NamedBuildTarget.Android, adaptive, iconsObj });
        }
        catch (Exception e)
        {
            Debug.LogWarning("AppIconGen: iconos adaptivos no disponibles, se usan legacy. " + e.Message);
        }
    }

    /// <summary>Tamaño de capa (con tope inferior) leído por reflection, robusto.</summary>
    static int LayerSize(MethodInfo getLayerWidth, object slot, int layer)
    {
        try
        {
            if (getLayerWidth != null)
            {
                int w = (int)getLayerWidth.Invoke(slot, new object[] { layer });
                return Mathf.Max(64, w);
            }
        }
        catch { /* usa el valor por defecto */ }
        return 512;
    }

    /// <summary>
    /// Localiza el overload de PlayerSettings.{Get,Set}PlatformIcons cuyo primer
    /// parámetro es NamedBuildTarget y con el nº de parámetros indicado.
    /// </summary>
    static MethodInfo FindPlayerSettingsIconMethod(string name, int paramCount)
    {
        foreach (var m in typeof(PlayerSettings).GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            if (m.Name != name) continue;
            var ps = m.GetParameters();
            if (ps.Length != paramCount) continue;
            if (ps[0].ParameterType == typeof(NamedBuildTarget)) return m;
        }
        return null;
    }

    /// <summary>Busca un tipo por nombre completo en los ensamblados cargados.</summary>
    static Type FindType(string fullName)
    {
        Type t = Type.GetType(fullName);
        if (t != null) return t;
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            t = asm.GetType(fullName);
            if (t != null) return t;
        }
        return null;
    }

    // ---------------------------------------------------------------------
    // SPLASH
    // ---------------------------------------------------------------------

    static void ApplySplash()
    {
        try
        {
            PlayerSettings.SplashScreen.backgroundColor = SkyTop;
            PlayerSettings.SplashScreen.blurBackgroundImage = false;
            // Si la licencia (Personal) no permite quitar el logo de Unity, el
            // try/catch evita romper el build: queda solo el color de fondo.
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("AppIconGen: no se pudo ocultar el logo de Unity (licencia). Se conserva el color de fondo. " + e.Message);
        }
    }

    // ---------------------------------------------------------------------
    // VERSIONADO
    // ---------------------------------------------------------------------

    static void ApplyVersion()
    {
        if (string.IsNullOrEmpty(PlayerSettings.bundleVersion) || PlayerSettings.bundleVersion == "0.1")
            PlayerSettings.bundleVersion = "1.0.0";

        // bundleVersionCode incremental, siempre >= 1.
        int code = PlayerSettings.Android.bundleVersionCode;
        PlayerSettings.Android.bundleVersionCode = Mathf.Max(1, code);
    }

    // ---------------------------------------------------------------------
    // DIBUJO DEL ICONO
    // ---------------------------------------------------------------------

    /// <summary>
    /// Construye la textura del icono a la resolución dada. Si withBackground es
    /// true incluye el degradado opaco de fondo; si no, el fondo queda
    /// transparente (para usar como capa foreground de los iconos adaptivos).
    /// </summary>
    static Texture2D BuildIconTexture(int size, bool withBackground)
    {
        var px = new Color[size * size];

        if (withBackground)
            FillBackground(px, size);
        else
            for (int i = 0; i < px.Length; i++) px[i] = new Color(0f, 0f, 0f, 0f);

        DrawSubject(px, size);

        return ToTexture(px, size);
    }

    /// <summary>Textura plana con SOLO el degradado de fondo (capa background adaptiva).</summary>
    static Texture2D BuildBackgroundTexture(int size)
    {
        var px = new Color[size * size];
        FillBackground(px, size);
        return ToTexture(px, size);
    }

    /// <summary>Rellena el degradado vertical de la paleta con viñeta en esquinas.</summary>
    static void FillBackground(Color[] px, int size)
    {
        float c = (size - 1) * 0.5f;
        float maxDist = Mathf.Sqrt(2f) * c;
        for (int y = 0; y < size; y++)
        {
            float t = (float)y / (size - 1);            // 0 abajo, 1 arriba
            Color baseCol = Color.Lerp(SkyBottom, SkyTop, t);
            for (int x = 0; x < size; x++)
            {
                // Viñeta: oscurece hacia las esquinas.
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / maxDist;
                float vig = Mathf.Lerp(1f, 0.55f, Mathf.SmoothStep(0.55f, 1f, d));
                Color col = baseCol * vig;
                col.a = 1f;
                px[y * size + x] = col;
            }
        }
    }

    /// <summary>
    /// Dibuja el "sujeto" del icono sobre el array: silueta de zombie centrada y
    /// la mira de francotirador (anillo + cruz + marcas) por encima. Coordenadas
    /// normalizadas (0..1) para ser independientes de la resolución.
    /// </summary>
    static void DrawSubject(Color[] px, int size)
    {
        // --- Cabeza de zombie (disco) ---
        Vector2 head = new Vector2(0.5f, 0.52f);
        float headR = 0.225f;

        // Sombra/contorno: disco un poco mayor y oscuro.
        Disc(px, size, head, headR + 0.022f, Outline);
        // Cuerpo principal.
        Disc(px, size, head, headR, ZombieBody);
        // Sombreado inferior del cuerpo (media luna).
        Disc(px, size, head + new Vector2(0.02f, -0.06f), headR * 0.86f, ZombieShade, alpha: 0.45f);

        // Orejas (dos discos pequeños a los lados).
        float earR = 0.06f;
        Disc(px, size, head + new Vector2(-headR * 0.92f, 0.02f), earR, Outline);
        Disc(px, size, head + new Vector2(-headR * 0.92f, 0.02f), earR * 0.7f, ZombieBody);
        Disc(px, size, head + new Vector2(headR * 0.92f, 0.02f), earR, Outline);
        Disc(px, size, head + new Vector2(headR * 0.92f, 0.02f), earR * 0.7f, ZombieBody);

        // Ojos rojos brillantes con halo.
        float eyeR = 0.045f;
        Vector2 eyeL = head + new Vector2(-0.08f, 0.03f);
        Vector2 eyeR2 = head + new Vector2(0.08f, 0.03f);
        Disc(px, size, eyeL, eyeR * 1.6f, EyeRed, alpha: 0.35f); // halo
        Disc(px, size, eyeR2, eyeR * 1.6f, EyeRed, alpha: 0.35f);
        Disc(px, size, eyeL, eyeR, EyeRed);
        Disc(px, size, eyeR2, eyeR, EyeRed);
        Disc(px, size, eyeL + new Vector2(-0.012f, 0.012f), eyeR * 0.35f, Color.white, alpha: 0.85f); // brillo
        Disc(px, size, eyeR2 + new Vector2(-0.012f, 0.012f), eyeR * 0.35f, Color.white, alpha: 0.85f);

        // Boca/mueca: línea oscura dentada simple.
        Vector2 mouthL = head + new Vector2(-0.09f, -0.12f);
        Vector2 mouthR = head + new Vector2(0.09f, -0.12f);
        Line(px, size, mouthL, mouthR, 0.022f, Outline);
        // Dientes: pequeñas marcas verticales claras.
        for (int t = -2; t <= 2; t++)
        {
            float fx = head.x + t * 0.035f;
            Vector2 a = new Vector2(fx, head.y - 0.10f);
            Vector2 b = new Vector2(fx, head.y - 0.135f);
            Line(px, size, a, b, 0.008f, new Color(0.92f, 0.92f, 0.86f));
        }

        // --- Mira de francotirador (por encima de la silueta) ---
        Vector2 cen = new Vector2(0.5f, 0.5f);
        float ringR = 0.40f;
        Ring(px, size, cen, ringR, 0.028f, Neon, alpha: 0.95f);
        Ring(px, size, cen, ringR - 0.05f, 0.010f, Neon, alpha: 0.45f);

        // Cruz central, con hueco en el centro (no tapa los ojos).
        float gap = 0.06f, ext = ringR + 0.03f, th = 0.018f;
        Line(px, size, new Vector2(0.5f, cen.y + gap), new Vector2(0.5f, cen.y + ext), th, Neon);
        Line(px, size, new Vector2(0.5f, cen.y - gap), new Vector2(0.5f, cen.y - ext), th, Neon);
        Line(px, size, new Vector2(cen.x + gap, 0.5f), new Vector2(cen.x + ext, 0.5f), th, Neon);
        Line(px, size, new Vector2(cen.x - gap, 0.5f), new Vector2(cen.x - ext, 0.5f), th, Neon);

        // Marcas (ticks) cada 90° sobre el anillo.
        for (int k = 0; k < 4; k++)
        {
            float ang = k * Mathf.PI * 0.5f;
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            Vector2 a = cen + dir * (ringR - 0.045f);
            Vector2 b = cen + dir * (ringR + 0.01f);
            Line(px, size, a, b, 0.02f, Neon);
        }
    }

    // ---------------------------------------------------------------------
    // HELPERS DE RASTERIZADO (coordenadas normalizadas 0..1, antialias suave)
    // ---------------------------------------------------------------------

    /// <summary>Disco relleno con borde suavizado (antialias de 1 píxel).</summary>
    static void Disc(Color[] px, int size, Vector2 cN, float rN, Color col, float alpha = 1f)
    {
        float cx = cN.x * size, cy = cN.y * size, r = rN * size;
        float aa = 1.2f;
        int x0 = Mathf.Max(0, (int)(cx - r - 2));
        int x1 = Mathf.Min(size - 1, (int)(cx + r + 2));
        int y0 = Mathf.Max(0, (int)(cy - r - 2));
        int y1 = Mathf.Min(size - 1, (int)(cy + r + 2));
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                float cov = Mathf.Clamp01((r - d) / aa + 0.5f);
                if (cov <= 0f) continue;
                Blend(px, y * size + x, col, cov * alpha);
            }
    }

    /// <summary>Anillo (círculo hueco) de grosor thickN.</summary>
    static void Ring(Color[] px, int size, Vector2 cN, float rN, float thickN, Color col, float alpha = 1f)
    {
        float cx = cN.x * size, cy = cN.y * size, r = rN * size, half = thickN * size * 0.5f;
        float aa = 1.2f;
        int x0 = Mathf.Max(0, (int)(cx - r - half - 2));
        int x1 = Mathf.Min(size - 1, (int)(cx + r + half + 2));
        int y0 = Mathf.Max(0, (int)(cy - r - half - 2));
        int y1 = Mathf.Min(size - 1, (int)(cy + r + half + 2));
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                float edge = Mathf.Abs(d - r);
                float cov = Mathf.Clamp01((half - edge) / aa + 0.5f);
                if (cov <= 0f) continue;
                Blend(px, y * size + x, col, cov * alpha);
            }
    }

    /// <summary>Segmento (cápsula) de grosor thickN entre dos puntos normalizados.</summary>
    static void Line(Color[] px, int size, Vector2 aN, Vector2 bN, float thickN, Color col, float alpha = 1f)
    {
        Vector2 a = new Vector2(aN.x * size, aN.y * size);
        Vector2 b = new Vector2(bN.x * size, bN.y * size);
        float half = thickN * size * 0.5f;
        float aa = 1.2f;
        int x0 = Mathf.Max(0, (int)(Mathf.Min(a.x, b.x) - half - 2));
        int x1 = Mathf.Min(size - 1, (int)(Mathf.Max(a.x, b.x) + half + 2));
        int y0 = Mathf.Max(0, (int)(Mathf.Min(a.y, b.y) - half - 2));
        int y1 = Mathf.Min(size - 1, (int)(Mathf.Max(a.y, b.y) + half + 2));
        Vector2 ab = b - a;
        float abLen2 = Mathf.Max(1e-5f, ab.sqrMagnitude);
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / abLen2);
                Vector2 proj = a + ab * t;
                float d = Vector2.Distance(p, proj);
                float cov = Mathf.Clamp01((half - d) / aa + 0.5f);
                if (cov <= 0f) continue;
                Blend(px, y * size + x, col, cov * alpha);
            }
    }

    /// <summary>Alpha-blend de un color sobre el píxel del array (over).</summary>
    static void Blend(Color[] px, int idx, Color col, float a)
    {
        if (idx < 0 || idx >= px.Length) return;
        a = Mathf.Clamp01(a);
        Color dst = px[idx];
        float outA = a + dst.a * (1f - a);
        Color rgb = col * a + dst * (dst.a * (1f - a));
        rgb.a = outA;
        px[idx] = rgb;
    }

    // ---------------------------------------------------------------------
    // UTILIDADES DE TEXTURA
    // ---------------------------------------------------------------------

    static Texture2D ToTexture(Color[] px, int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear; // el icono debe verse limpio grande
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.SetPixels(px);
        tex.Apply(false, false);
        return tex;
    }

    /// <summary>Reescala bilinealmente una textura a un tamaño cuadrado nuevo.</summary>
    static Texture2D ScaleTexture(Texture2D src, int newSize)
    {
        if (src.width == newSize && src.height == newSize) return src;
        var dst = new Texture2D(newSize, newSize, TextureFormat.RGBA32, false);
        dst.filterMode = FilterMode.Bilinear;
        dst.wrapMode = TextureWrapMode.Clamp;
        var px = new Color[newSize * newSize];
        for (int y = 0; y < newSize; y++)
        {
            float v = (float)y / (newSize - 1);
            for (int x = 0; x < newSize; x++)
            {
                float u = (float)x / (newSize - 1);
                px[y * newSize + x] = src.GetPixelBilinear(u, v);
            }
        }
        dst.SetPixels(px);
        dst.Apply(false, false);
        return dst;
    }

    /// <summary>Convierte "#RRGGBB" o "#RRGGBBAA" a Color (fallback magenta si falla).</summary>
    static Color Hex(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out var c)) return c;
        return Color.magenta;
    }
}
#endif
