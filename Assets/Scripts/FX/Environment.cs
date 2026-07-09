using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Entorno de Zombie Rush con assets cargados desde <see cref="ArtCache"/>. Da
/// "mundo" a las escenas con un CIELO en degradado procedural (sorting -100),
/// un SUELO de tiles de asfalto que scrollean reciclando losas sin costuras
/// (-90), PROPS laterales (barriles, conos, rocas, barreras) en PARALLAX
/// reciclado (-60..-40) y bandas de NIEBLA sutil (-31).
///
/// La VIÑETA se elimina de aquí: la aporta el volume URP (cambio render-pipeline).
///
/// La cámara es ortográfica fija y no se mueve en Y: el propio entorno desplaza
/// sus elementos hacia abajo para dar sensación de avance. En la escena de juego
/// el scroll sólo avanza con GameState.Playing; en el menú avanza siempre, lento.
///
/// Filosofía code-first: los sprites se piden a <see cref="ArtCache"/> por
/// nombre, sin cablear nada en el Inspector. Si un asset falta, ArtCache cae a
/// un fallback procedural.
/// </summary>
public class Environment : MonoBehaviour
{
    // --- Paleta (mood "noche apocalíptica neón") ---
    static readonly Color SKY_TOP   = Hex("14122A");
    static readonly Color SKY_BOT   = Hex("241A3A");
    static readonly Color FOG_TINT  = Hex("3A3060");
    static readonly Color PROP_FAR_TINT = new Color(0.5f, 0.5f, 0.6f, 0.7f); // atenuado por distancia

    // --- Sorting orders ---
    const int SORT_SKY       = -100;
    const int SORT_GROUND    = -90;
    const int SORT_PROP_FAR  = -60;
    const int SORT_PROP_NEAR = -45;
    const int SORT_FOG       = -31;

    // --- Temas de localización (uno cada 2 actos; el menú usa suburbs) ---
    static readonly string[] THEMES = { "suburbs", "downtown", "cemetery", "industrial", "lab" };

    /// <summary>Prop "luminoso" de cada tema (recibe un Light2D ámbar/propio).</summary>
    static readonly Dictionary<string, string> THEME_LIGHT = new Dictionary<string, string>
    {
        { "suburbs",    "prop_suburbs_02"    }, // farola encendida
        { "downtown",   "prop_downtown_05"   }, // neón roto
        { "cemetery",   "prop_cemetery_06"   }, // farol de gas
        { "industrial", "prop_industrial_07" }, // foco industrial
        { "lab",        "prop_lab_02"        }, // luz de emergencia
    };

    /// <summary>
    /// Tamaño de mundo BASE de cada prop (unidades, para su dimensión mayor), por
    /// tema e índice 01..08. La normalización de ArtCache iguala todos los sprites
    /// a ~1u, así que la escala relativa real (un cono ≪ un coche) se restaura
    /// aquí. Referencia: un soldado mide 0.32u.
    /// </summary>
    static readonly Dictionary<string, float[]> THEME_PROP_SIZE = new Dictionary<string, float[]>
    {
        //                         01    02    03    04    05    06    07    08
        { "suburbs",    new[] { 1.80f, 1.30f, 0.95f, 0.60f, 0.90f, 0.45f, 0.40f, 0.65f } }, // coche, farola, valla, arbusto, señal, tapa, cono, neumáticos
        { "downtown",   new[] { 2.20f, 0.50f, 0.90f, 1.20f, 0.85f, 0.75f, 0.50f, 0.80f } }, // bus, escombros, barricada, semáforo, neón, cartel, basura, tablas
        { "cemetery",   new[] { 0.55f, 0.70f, 1.60f, 0.95f, 1.40f, 0.90f, 0.75f, 0.60f } }, // lápida, cruz, árbol, verja, mausoleo, farol, banco, pozo
        { "industrial", new[] { 1.90f, 0.50f, 0.80f, 1.10f, 0.90f, 0.95f, 1.10f, 0.70f } }, // contenedor, bidón, palé, tubería, charco, tubo, foco, bidones
        { "lab",        new[] { 1.00f, 0.50f, 0.85f, 0.80f, 1.15f, 0.80f, 1.05f, 1.25f } }, // tanque, luz, cables, pantalla, cápsula, torreta, valla, puerta
    };

    // --- Estado de scroll ---
    Camera cam;
    float scrollSpeed;
    bool scrollGated;
    string theme = "suburbs";

    // --- Cielo ---
    Transform sky;
    Transform skyline;

    // --- Suelo: losas que se reciclan (+ columnas de arcén a los lados) ---
    readonly List<Transform> groundSlabs = new List<Transform>();
    readonly List<Transform> edgeColumns = new List<Transform>(); // [L0, L1, R0, R1]
    Material edgeMat;
    float slabHeight;
    float slabsTotalSpan;
    const float EDGE_W = 0.85f; // ancho del arcén en unidades

    // --- Niebla ---
    readonly List<Transform> fogBands = new List<Transform>();
    readonly List<float> fogDrift = new List<float>();

    // --- Props laterales en parallax ---
    class Prop
    {
        public Transform t;
        public SpriteRenderer sr;
        public float parallax;
    }
    readonly List<Prop> props = new List<Prop>();
    Sprite[] propSprites;
    float[] propBaseSizes; // tamaño de mundo base por sprite (alineado con propSprites)
    HashSet<int> lightPropIndices = new HashSet<int>(); // índices de props que emiten luz (farolas)

    float lastAspect = -1f;

    // ----------------------------------------------------------------------
    //  PUNTOS DE ENTRADA
    // ----------------------------------------------------------------------

    public static void Build(float scrollSpeed) => Create(scrollSpeed, gated: true);

    public static void BuildMenu() => Create(0.45f, gated: false);

    static void Create(float scrollSpeed, bool gated)
    {
        var existing = GameObject.Find("Environment");
        if (existing != null) Object.Destroy(existing);

        var go = new GameObject("Environment");
        var env = go.AddComponent<Environment>();
        env.scrollSpeed = Mathf.Max(0f, scrollSpeed);
        env.scrollGated = gated;
        env.theme = ResolveTheme(gated);
    }

    /// <summary>Tema de localización para un nivel dado (uno cada 2 actos). Público:
    /// lo usan también LevelRunner (obstáculos temáticos) y quien lo necesite.</summary>
    public static string ThemeFor(int level)
    {
        int act = (level - 1) / 10;                       // 0..9
        return THEMES[Mathf.Clamp(act / 2, 0, THEMES.Length - 1)];
    }

    /// <summary>Tema según el estado: menú → suburbs; juego → por nivel actual.</summary>
    static string ResolveTheme(bool gated)
    {
        if (!gated) return THEMES[0];
        return ThemeFor(GameManager.Instance != null ? GameManager.Instance.Level : 1);
    }

    // ----------------------------------------------------------------------
    //  CONSTRUCCIÓN
    // ----------------------------------------------------------------------

    void Start()
    {
        cam = Camera.main;
        if (cam == null) { Destroy(gameObject); return; }

        transform.position = Vector3.zero;

        BuildSky();
        BuildSkyline();
        BuildGround();
        BuildEdges();
        BuildProps();
        BuildFog();

        Resize();
    }

    /// <summary>Cielo: degradado vertical procedural #14122A → #241A3A.</summary>
    void BuildSky()
    {
        const int H = 64;
        var tex = new Texture2D(1, H, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        for (int y = 0; y < H; y++)
        {
            float f = y / (float)(H - 1);
            tex.SetPixel(0, y, Color.Lerp(SKY_BOT, SKY_TOP, f));
        }
        tex.Apply();

        var spr = Sprite.Create(tex, new Rect(0, 0, 1, H), new Vector2(0.5f, 0.5f), H);
        sky = MakeQuad("Sky", spr, Color.white, SORT_SKY).transform;
    }

    /// <summary>
    /// Suelo: losas de tile de asfalto cargadas desde ArtCache que scrollean y
    /// se reciclan. Dos losas apiladas para un bucle infinito sin costuras.
    /// </summary>
    void BuildGround()
    {
        for (int i = 0; i < 2; i++)
        {
            // Tile del tema (variante distinta por losa para dar variedad), con
            // fallback al asfalto genérico antiguo y, en último término, blanco.
            var tile = ArtCache.Sprite($"environment/road_{theme}_0{i + 1}")
                    ?? ArtCache.Sprite("environment/road_asphalt01");
            if (tile == null)
            {
                Debug.LogWarning("[Environment] Sin tile de suelo; usando fallback blanco.");
                tile = Sprite.Create(Texture2D.whiteTexture,
                    new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                    new Vector2(0.5f, 0.5f), Texture2D.whiteTexture.width);
            }

            // El tile repite verticalmente (wrapMode Repeat) para scroll sin costuras.
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.mainTexture = tile.texture;
            mat.SetTextureScale("_MainTex", new Vector2(1f, 2f)); // repite 2 veces por losa

            var go = MakeQuad("GroundSlab" + i, tile, Color.white, SORT_GROUND);
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sharedMaterial = mat;
            groundSlabs.Add(go.transform);
        }
    }

    /// <summary>
    /// Arcenes: dos columnas de tile <c>edge_{tema}</c> a cada lado de la calzada
    /// que scrollean con el suelo (mismo reciclado que las losas). Si el tema no
    /// tiene arcén, se omiten sin ruido.
    /// </summary>
    void BuildEdges()
    {
        var tile = ArtCache.Sprite($"environment/edge_{theme}");
        if (tile == null) return;

        edgeMat = new Material(Shader.Find("Sprites/Default"));
        edgeMat.mainTexture = tile.texture;

        for (int i = 0; i < 4; i++) // [izq0, izq1, der0, der1]
        {
            var go = MakeQuad("Edge" + i, tile, Color.white, SORT_GROUND + 1);
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sharedMaterial = edgeMat;
            edgeColumns.Add(go.transform);
        }
    }

    /// <summary>
    /// Skyline: franja de silueta lejana pegada al borde superior (horizonte
    /// falso, como en la portada). Estática: es "distancia infinita".
    /// </summary>
    void BuildSkyline()
    {
        var s = ArtCache.Sprite($"environment/skyline_{theme}");
        if (s == null) return;
        skyline = MakeQuad("Skyline", s, new Color(1f, 1f, 1f, 0.85f), SORT_SKY + 5).transform;
    }

    /// <summary>Props laterales cargados desde ArtCache, en pool de parallax.</summary>
    void BuildProps()
    {
        // Props del TEMA actual (prop_{tema}_01..08); si el tema no aporta
        // suficientes, cae al set genérico antiguo (barriles/conos/rocas).
        var names = new List<string>();
        for (int i = 1; i <= 8; i++) names.Add($"environment/prop_{theme}_0{i}");

        string lightName = THEME_LIGHT.TryGetValue(theme, out var ln) ? ln : null;
        float[] themeSizes = THEME_PROP_SIZE.TryGetValue(theme, out var ts) ? ts : null;

        var list = new List<Sprite>();
        var sizes = new List<float>();
        int idx = 0;
        for (int i = 0; i < names.Count; i++)
        {
            var s = ArtCache.Sprite(names[i]);
            if (s != null)
            {
                list.Add(s);
                sizes.Add(themeSizes != null && i < themeSizes.Length ? themeSizes[i] : 0.8f);
                if (lightName != null && names[i].EndsWith(lightName)) lightPropIndices.Add(idx);
                idx++;
            }
        }

        if (list.Count < 3)
        {
            // Fallback: set genérico pre-temas.
            list.Clear(); sizes.Clear(); lightPropIndices.Clear(); idx = 0;
            var legacy = new[]
            {
                "environment/prop_barrel_blue", "environment/prop_barrel_red",
                "environment/prop_cone", "environment/prop_rock1",
                "environment/prop_rock2", "environment/prop_rock3",
                "environment/prop_barrier_red", "environment/prop_barrier_white",
                "environment/prop_light_white", "environment/prop_light_yellow",
            };
            foreach (var n in legacy)
            {
                var s = ArtCache.Sprite(n);
                if (s != null)
                {
                    list.Add(s);
                    sizes.Add(0.8f);
                    if (n.Contains("light")) lightPropIndices.Add(idx);
                    idx++;
                }
            }
        }

        propBaseSizes = sizes.ToArray();

        if (list.Count == 0)
        {
            Debug.LogWarning("[Environment] No se cargaron props desde ArtCache; props desactivados.");
            propSprites = System.Array.Empty<Sprite>();
            return;
        }

        propSprites = list.ToArray();

        const int count = 10;
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Prop" + i);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = propSprites[0];

            var p = new Prop { t = go.transform, sr = sr };
            props.Add(p);
            RecycleProp(p, initial: true, slot: i, total: count);
        }
    }

    /// <summary>Niebla: 1-2 bandas semitransparentes que derivan en X.</summary>
    void BuildFog()
    {
        const int W = 64, H = 16;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        var px = new Color[W * H];
        for (int y = 0; y < H; y++)
        {
            float fy = 1f - Mathf.Abs((y / (float)(H - 1)) - 0.5f) * 2f;
            for (int x = 0; x < W; x++)
            {
                float fx = 1f - Mathf.Abs((x / (float)(W - 1)) - 0.5f) * 2f;
                var c = FOG_TINT;
                c.a = 0.10f * fy * Mathf.Lerp(0.6f, 1f, fx);
                px[y * W + x] = c;
            }
        }
        tex.SetPixels(px); tex.Apply();
        var spr = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), Mathf.Max(W, H));

        for (int i = 0; i < 2; i++)
        {
            var go = MakeQuad("Fog" + i, spr, Color.white, SORT_FOG);
            fogBands.Add(go.transform);
            fogDrift.Add(i == 0 ? 0.25f : -0.18f);
        }
    }

    // ----------------------------------------------------------------------
    //  UPDATE: scroll + parallax + reciclado
    // ----------------------------------------------------------------------

    void Update()
    {
        if (cam == null) return;
        if (!Mathf.Approximately(cam.aspect, lastAspect)) Resize();

        if (scrollGated)
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing) return;
        }

        float dt = Time.deltaTime;
        float halfH = cam.orthographicSize;

        // Suelo: las losas bajan; al salir por abajo se recolocan arriba.
        float move = scrollSpeed * dt;
        foreach (var slab in groundSlabs)
        {
            Vector3 p = slab.position;
            p.y -= move;
            if (p.y + slabHeight * 0.5f < -halfH)
                p.y += slabsTotalSpan;
            slab.position = p;
        }

        // Arcenes: mismo scroll y reciclado que las losas.
        foreach (var col in edgeColumns)
        {
            Vector3 p = col.position;
            p.y -= move;
            if (p.y + slabHeight * 0.5f < -halfH)
                p.y += slabHeight * 2f; // dos columnas apiladas por lado
            col.position = p;
        }

        // Props: parallax + reciclado lateral.
        float margin = 1.5f;
        for (int i = 0; i < props.Count; i++)
        {
            var pr = props[i];
            Vector3 p = pr.t.position;
            p.y -= scrollSpeed * pr.parallax * dt;
            pr.t.position = p;
            if (p.y < -halfH - margin)
                RecycleProp(pr, initial: false, slot: i, total: props.Count);
        }

        // Niebla: deriva lenta en X.
        float halfW = halfH * cam.aspect;
        for (int i = 0; i < fogBands.Count; i++)
        {
            Vector3 p = fogBands[i].position;
            p.x += fogDrift[i] * dt;
            if (p.x > halfW * 0.5f || p.x < -halfW * 0.5f) fogDrift[i] = -fogDrift[i];
            fogBands[i].position = p;
        }
    }

    // ----------------------------------------------------------------------
    //  ENCAJE AL ASPECT
    // ----------------------------------------------------------------------

    void Resize()
    {
        lastAspect = cam.aspect;
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        float fullH = halfH * 2f;
        float fullW = halfW * 2f;

        float coverW = fullW + 1.5f;
        float coverH = fullH + 1.5f;

        if (sky != null) sky.localScale = new Vector3(coverW, coverH, 1f);

        // Skyline: franja pegada al borde superior, ancho completo (aspect 4:1).
        if (skyline != null)
        {
            float sw = coverW;
            skyline.localScale = new Vector3(sw, sw, 1f); // sprite 1×0.25u → alto = sw*0.25
            skyline.position = new Vector3(0f, halfH - sw * 0.25f * 0.5f + 0.15f, 0f);
        }

        // Suelo: cada losa cubre la pantalla; dos apiladas → bucle.
        slabHeight = coverH;
        slabsTotalSpan = slabHeight * groundSlabs.Count;
        for (int i = 0; i < groundSlabs.Count; i++)
        {
            var slab = groundSlabs[i];
            slab.localScale = new Vector3(coverW, slabHeight, 1f);
            slab.position = new Vector3(0f, i * slabHeight, slab.position.z);
        }

        // Arcenes: columna de ancho fijo pegada a cada borde; tile cuadrado
        // repetido en vertical vía material (mundo: EDGE_W de alto por repetición).
        if (edgeColumns.Count == 4)
        {
            if (edgeMat != null)
                edgeMat.SetTextureScale("_MainTex", new Vector2(1f, slabHeight / EDGE_W));
            float ex = halfW - EDGE_W * 0.5f;
            for (int i = 0; i < 4; i++)
            {
                bool leftSide = i < 2;
                int stack = i % 2;
                var col = edgeColumns[i];
                // El lado derecho se espeja para que el bordillo mire a la calzada.
                col.localScale = new Vector3(leftSide ? EDGE_W : -EDGE_W, slabHeight, 1f);
                col.position = new Vector3(leftSide ? -ex : ex, stack * slabHeight, col.position.z);
            }
        }

        // Niebla.
        for (int i = 0; i < fogBands.Count; i++)
        {
            fogBands[i].localScale = new Vector3(fullW * 1.2f, fullH * 0.30f, 1f);
            float fy = i == 0 ? halfH * 0.35f : -halfH * 0.30f;
            fogBands[i].position = new Vector3(fogBands[i].position.x, fy, fogBands[i].position.z);
        }
    }

    // ----------------------------------------------------------------------
    //  RECICLADO DE PROPS
    // ----------------------------------------------------------------------

    void RecycleProp(Prop pr, bool initial, int slot, int total)
    {
        if (propSprites == null || propSprites.Length == 0) return;

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        int type = Random.Range(0, propSprites.Length);
        pr.sr.sprite = propSprites[type];

        // Si es un prop de luz (farola), añade/actualiza un Light2D hijo ámbar.
        bool isLight = lightPropIndices.Contains(type);
        UpdateLight(pr, isLight);

        // Pegados al arcén (pueden asomar medio fuera de pantalla), sin invadir
        // la calzada: el arte normalizado mide ~1u y a escala grande llegaba al centro.
        bool left = Random.value < 0.5f;
        float lateral = Random.Range(0.84f, 1.04f) * halfW;
        float x = left ? -lateral : lateral;

        // Todos los props van EXACTAMENTE a la velocidad del suelo: es vista
        // cenital y comparten plano con el asfalto; con parallax parecían
        // "deslizarse" sobre la carretera.
        pr.parallax = 1f;

        // Tamaño REAL del objeto (tabla por tema: cono ≪ coche) ± variación leve.
        float baseSize = propBaseSizes != null && type < propBaseSizes.Length ? propBaseSizes[type] : 0.8f;
        float s = baseSize * Random.Range(0.85f, 1.15f);
        pr.t.localScale = new Vector3(s, s, 1f);

        // Dos "capas" visuales solo de tinte/orden (no de tamaño ni velocidad).
        bool near = Random.value < 0.5f;
        pr.sr.sortingOrder = near ? SORT_PROP_NEAR : SORT_PROP_FAR;
        pr.sr.color = near ? Color.white : PROP_FAR_TINT;

        if (Random.value < 0.5f)
        {
            var sc = pr.t.localScale; sc.x = -sc.x; pr.t.localScale = sc;
        }

        float y;
        if (initial)
        {
            float span = halfH * 2f + 3f;
            y = -halfH + (slot + 0.5f) / total * span;
        }
        else
        {
            y = halfH + Random.Range(0.5f, 2.5f);
        }

        pr.t.position = new Vector3(x, y, 0f);
    }

    // ----------------------------------------------------------------------
    //  HELPERS
    // ----------------------------------------------------------------------

    GameObject MakeQuad(string name, Sprite spr, Color color, int sortingOrder)
    {
        var go = Prims.MakeSprite(name, spr, color, Vector2.one, Vector3.zero, sortingOrder);
        go.transform.SetParent(transform, false);
        return go;
    }

    static Color Hex(string rgb)
    {
        byte r = (byte)System.Convert.ToInt32(rgb.Substring(0, 2), 16);
        byte g = (byte)System.Convert.ToInt32(rgb.Substring(2, 2), 16);
        byte b = (byte)System.Convert.ToInt32(rgb.Substring(4, 2), 16);
        return new Color32(r, g, b, 255);
    }

    /// <summary>
    /// Añade o reutiliza un Light2D hijo (color ámbar #E8A23A) en el prop si es
    /// una farola; lo desactiva si no. Creado por código (code-first).
    /// </summary>
    void UpdateLight(Prop pr, bool isLight)
    {
        Transform lightT = null;
        for (int i = 0; i < pr.t.childCount; i++)
            if (pr.t.GetChild(i).name == "Light2D") { lightT = pr.t.GetChild(i); break; }

        if (!isLight)
        {
            if (lightT != null) lightT.gameObject.SetActive(false);
            return;
        }

        GameObject lightGo;
        if (lightT != null) { lightGo = lightT.gameObject; lightGo.SetActive(true); }
        else
        {
            lightGo = new GameObject("Light2D");
            lightGo.transform.SetParent(pr.t, false);
        }

        var l2d = lightGo.GetComponent<Light2D>();
        if (l2d == null) l2d = lightGo.AddComponent<Light2D>();
        l2d.lightType = Light2D.LightType.Point;
        l2d.color = Hex("E8A23A"); // ámbar tenue de farola
        l2d.intensity = 0.8f;
        l2d.pointLightInnerRadius = 0.5f;
        l2d.pointLightOuterRadius = 2.5f;
        lightGo.transform.localPosition = Vector3.zero;
    }
}
