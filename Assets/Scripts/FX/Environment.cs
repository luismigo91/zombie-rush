using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Entorno procedural de Zombie Rush: da "mundo" a las escenas. Todo se genera por
/// código (Texture2D, sin assets binarios) y se autogestiona en Update.
///
/// Construye un CIELO en degradado vertical (sorting -100) que cubre toda la cámara,
/// una VIÑETA oscura en los bordes (-30), un SUELO de asfalto con LÍNEAS DE CARRIL
/// que scrollean hacia abajo reciclando "losas" sin costuras (-90), PROPS laterales
/// (árboles muertos, farolas, escombros) en PARALLAX reciclado (-60..-40) y unas
/// bandas de NIEBLA sutil (-30).
///
/// La cámara es ortográfica fija (orthographicSize=5) y no se mueve en Y: por eso es
/// el propio entorno quien desplaza sus elementos hacia abajo para dar sensación de
/// avance. En la escena de juego el scroll sólo avanza con GameState.Playing (igual
/// que Gate.cs); en el menú avanza siempre, lento.
/// </summary>
public class Environment : MonoBehaviour
{
    // --- Paleta de la biblia (mood "noche apocalíptica neón") ---
    static readonly Color SKY_TOP   = Hex("14122A"); // cielo arriba
    static readonly Color SKY_BOT   = Hex("241A3A"); // cielo abajo
    static readonly Color ASPHALT   = Hex("2A2740"); // suelo/asfalto
    static readonly Color LANE      = Hex("4A4668"); // líneas de carril
    static readonly Color PROP_DARK = Hex("0E0C1C"); // silueta de prop (casi negro con tinte violeta)
    static readonly Color PROP_MID  = Hex("16122A"); // prop algo más claro (cercano)
    static readonly Color LAMP_HALO = Hex("E8A23A"); // halo ámbar tenue de farola
    static readonly Color FOG_TINT  = Hex("3A3060"); // niebla azul-violeta tenue

    // --- Sorting orders (biblia): cielo -100, suelo -90, props -60..-40, viñeta/niebla -30 ---
    const int SORT_SKY   = -100;
    const int SORT_GROUND = -90;
    const int SORT_PROP_FAR  = -60; // props lejanos (lentos, oscuros, pequeños)
    const int SORT_PROP_NEAR = -45; // props cercanos (rápidos, grandes)
    const int SORT_FOG  = -31;
    const int SORT_VIGNETTE = -30;

    // --- Estado de scroll ---
    Camera cam;
    float scrollSpeed;     // velocidad base de avance (u/s)
    bool scrollGated;      // true en juego (gate por GameState.Playing); false en menú

    // --- Cielo / viñeta (se reescalan al cambiar el aspect) ---
    Transform sky;
    Transform vignette;

    // --- Suelo: dos losas apiladas que se reciclan para un bucle infinito sin costuras ---
    readonly List<Transform> groundSlabs = new List<Transform>();
    float slabHeight;      // alto (mundo) de cada losa
    float slabsTotalSpan;  // alto total de todas las losas apiladas

    // --- Niebla: bandas que derivan lentamente ---
    readonly List<Transform> fogBands = new List<Transform>();
    readonly List<float> fogDrift = new List<float>(); // deriva en X de cada banda

    // --- Props laterales en parallax (pool reciclado) ---
    class Prop
    {
        public Transform t;
        public SpriteRenderer sr;
        public float parallax;     // multiplicador de scrollSpeed
    }
    readonly List<Prop> props = new List<Prop>();
    Sprite[] propSprites;          // [0]=árbol [1]=farola [2]=escombro

    // Caché del aspect para sólo reescalar el fondo cuando cambia.
    float lastAspect = -1f;

    // ----------------------------------------------------------------------
    //  PUNTOS DE ENTRADA (firmas de contrato — no cambiar)
    // ----------------------------------------------------------------------

    /// <summary>
    /// Escena de JUEGO: cielo+suelo+carriles que scrollean a 'scrollSpeed' (u/s)+parallax+viñeta.
    /// El desplazamiento sólo avanza mientras GameManager.State == GameState.Playing.
    /// </summary>
    public static void Build(float scrollSpeed)
    {
        Create(scrollSpeed, gated: true);
    }

    /// <summary>
    /// Escena de MENU: mismo mundo pero ambiental, scroll lento/casi estático, sin gating de GameState.
    /// </summary>
    public static void BuildMenu()
    {
        // Scroll lento para un fondo ambiental que respira sin distraer del menú.
        Create(0.45f, gated: false);
    }

    static void Create(float scrollSpeed, bool gated)
    {
        // Evita duplicados si por error se llama dos veces en la misma escena.
        var existing = GameObject.Find("Environment");
        if (existing != null) Object.Destroy(existing);

        var go = new GameObject("Environment");
        var env = go.AddComponent<Environment>();
        env.scrollSpeed = Mathf.Max(0f, scrollSpeed);
        env.scrollGated = gated;
    }

    // ----------------------------------------------------------------------
    //  CONSTRUCCIÓN
    // ----------------------------------------------------------------------

    void Start()
    {
        cam = Camera.main;
        if (cam == null)
        {
            // Sin cámara no hay nada que dibujar; nos auto-destruimos sin romper.
            Destroy(gameObject);
            return;
        }

        transform.position = Vector3.zero; // anclado al origen; el mundo no se mueve, se mueven sus piezas.

        BuildSky();
        BuildGround();
        BuildProps();
        BuildFog();
        BuildVignette();

        Resize(); // primer encaje al aspect actual
    }

    /// <summary>Cielo: degradado vertical #14122A → #241A3A en una textura difusa (Bilinear).</summary>
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
            // y=0 abajo → SKY_BOT ; y=H-1 arriba → SKY_TOP
            float f = y / (float)(H - 1);
            tex.SetPixel(0, y, Color.Lerp(SKY_BOT, SKY_TOP, f));
        }
        tex.Apply();

        var spr = Sprite.Create(tex, new Rect(0, 0, 1, H), new Vector2(0.5f, 0.5f), H);
        sky = MakeQuad("Sky", spr, Color.white, SORT_SKY).transform;
    }

    /// <summary>
    /// Suelo de asfalto con líneas de carril discontinuas, en losas que tilean
    /// verticalmente sin costura (el patrón de dashes encaja en los bordes).
    /// </summary>
    void BuildGround()
    {
        // Textura de losa: 64 (ancho lógico) x 128 (alto) en píxeles de patrón.
        // El ancho real lo da la escala; el alto define un tramo de carril.
        const int W = 64;
        const int H = 128;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat
        };

        // Base de asfalto.
        var pixels = new Color[W * H];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = ASPHALT;

        // Carriles verticales: columnas a 1/4, 2/4 y 3/4 del ancho, con dashes.
        int[] laneCols = { W / 4, W / 2, (3 * W) / 4 };
        const int dashLen = 16;  // alto del trazo
        const int gapLen = 16;   // hueco entre trazos (dashLen+gapLen divide H → tileado perfecto)
        const int lineW = 2;     // grosor de la línea en píxeles
        Color laneDim = LANE * 0.85f; laneDim.a = 1f; // ligeramente atenuada en bordes del trazo

        foreach (int col in laneCols)
        {
            for (int y = 0; y < H; y++)
            {
                bool onDash = (y % (dashLen + gapLen)) < dashLen;
                if (!onDash) continue;
                for (int dx = 0; dx < lineW; dx++)
                {
                    int x = col + dx;
                    if (x < 0 || x >= W) continue;
                    // Centro de la línea más brillante, bordes algo apagados.
                    Color c = dx == 0 ? LANE : laneDim;
                    pixels[y * W + x] = c;
                }
            }
        }

        // Vibración sutil del asfalto (grano) para que no sea plano.
        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i] == ASPHALT && ((i * 2654435761u) % 17u) == 0u)
                pixels[i] = ASPHALT * 1.08f;
        }

        tex.SetPixels(pixels);
        tex.Apply();

        var spr = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), H);

        // Dos losas apiladas (el alto/posición real se fija en Resize).
        for (int i = 0; i < 2; i++)
        {
            var go = MakeQuad("GroundSlab" + i, spr, Color.white, SORT_GROUND);
            groundSlabs.Add(go.transform);
        }
    }

    /// <summary>Props laterales (árbol muerto, farola, escombro) en pool de parallax.</summary>
    void BuildProps()
    {
        propSprites = new[]
        {
            MakeDeadTreeSprite(),
            MakeLampSprite(),
            MakeRubbleSprite()
        };

        const int count = 10; // cantidad fija de props reciclados
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Prop" + i);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = propSprites[0];

            var p = new Prop { t = go.transform, sr = sr };
            props.Add(p);
            // Reparto inicial escalonado en altura para que no entren todos a la vez.
            RecycleProp(p, initial: true, slot: i, total: count);
        }
    }

    /// <summary>Niebla: 1-2 bandas semitransparentes que derivan en X muy despacio.</summary>
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
            // Alpha máximo en el centro vertical de la banda, suave a los bordes.
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
            fogDrift.Add(i == 0 ? 0.25f : -0.18f); // derivas opuestas para dar profundidad
        }
    }

    /// <summary>Viñeta: oscurecimiento radial + marco en los bordes, encima del fondo.</summary>
    void BuildVignette()
    {
        const int S = 64;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        var px = new Color[S * S];
        Vector2 c = new Vector2((S - 1) * 0.5f, (S - 1) * 0.5f);
        float maxR = c.magnitude;
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float r = Vector2.Distance(new Vector2(x, y), c) / maxR; // 0 centro → 1 esquina
                // Radial: el centro queda limpio (alpha 0) y crece hacia fuera.
                float radial = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1f, r));
                // Refuerzo de marco arriba/abajo (encuadre).
                float ny = y / (float)(S - 1);
                float frame = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.30f, 0f, Mathf.Min(ny, 1f - ny)));
                float a = Mathf.Clamp01(Mathf.Max(radial, frame * 0.8f)) * 0.55f;
                px[y * S + x] = new Color(0f, 0f, 0f, a);
            }
        }
        tex.SetPixels(px); tex.Apply();
        var spr = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), S);
        vignette = MakeQuad("Vignette", spr, Color.white, SORT_VIGNETTE).transform;
    }

    // ----------------------------------------------------------------------
    //  SPRITES DE PROPS (siluetas generadas)
    // ----------------------------------------------------------------------

    /// <summary>Árbol muerto: tronco + ramas irregulares (silueta casi negra).</summary>
    Sprite MakeDeadTreeSprite()
    {
        const int S = 32;
        var px = NewClear(S);
        int cx = S / 2;
        // Tronco.
        for (int y = 2; y < 26; y++)
            for (int x = cx - 2; x <= cx + 1; x++)
                Set(px, S, x, y, PROP_DARK);
        // Ramas (líneas diagonales simples).
        DrawLine(px, S, cx, 16, cx - 8, 24, PROP_DARK);
        DrawLine(px, S, cx, 18, cx - 11, 22, PROP_DARK);
        DrawLine(px, S, cx, 14, cx + 9, 23, PROP_DARK);
        DrawLine(px, S, cx, 20, cx + 7, 28, PROP_DARK);
        DrawLine(px, S, cx, 22, cx + 2, 30, PROP_DARK);
        return ToSprite(px, S, FilterMode.Point);
    }

    /// <summary>Farola: poste + brazo + halo ámbar tenue.</summary>
    Sprite MakeLampSprite()
    {
        const int S = 32;
        var px = NewClear(S);
        int cx = S / 2;
        // Poste.
        for (int y = 2; y < 28; y++)
            for (int x = cx - 1; x <= cx; x++)
                Set(px, S, x, y, PROP_DARK);
        // Brazo horizontal arriba.
        for (int x = cx; x <= cx + 7; x++) Set(px, S, x, 27, PROP_DARK);
        // Cabeza de la lámpara.
        for (int y = 24; y <= 26; y++)
            for (int x = cx + 6; x <= cx + 8; x++)
                Set(px, S, x, y, PROP_DARK);
        // Halo ámbar difuso bajo la lámpara.
        Vector2 lampC = new Vector2(cx + 7, 24);
        for (int y = 16; y <= 26; y++)
            for (int x = cx; x < S; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), lampC);
                if (d > 7f) continue;
                var c = LAMP_HALO;
                c.a = Mathf.Clamp01(1f - d / 7f) * 0.45f;
                Blend(px, S, x, y, c);
            }
        return ToSprite(px, S, FilterMode.Point);
    }

    /// <summary>Escombro: bloque/roca dentada.</summary>
    Sprite MakeRubbleSprite()
    {
        const int S = 32;
        var px = NewClear(S);
        // Montículo irregular bajo.
        int[] heights = { 4, 7, 9, 11, 12, 11, 13, 10, 8, 6, 9, 7, 5, 8, 4, 3 };
        int baseX = 6;
        for (int i = 0; i < heights.Length; i++)
        {
            int x = baseX + i;
            if (x >= S) break;
            for (int y = 2; y < 2 + heights[i]; y++)
                Set(px, S, x, y, PROP_DARK);
        }
        // Un par de bloques rectangulares para variar la silueta.
        for (int y = 2; y < 9; y++)
            for (int x = 9; x < 14; x++)
                Set(px, S, x, y, PROP_DARK);
        return ToSprite(px, S, FilterMode.Point);
    }

    // ----------------------------------------------------------------------
    //  UPDATE: scroll + parallax + reciclado
    // ----------------------------------------------------------------------

    void Update()
    {
        if (cam == null) return;

        // Reencaja el fondo si cambió el aspect (rotación/resize de ventana).
        if (!Mathf.Approximately(cam.aspect, lastAspect)) Resize();

        // Gate de scroll: en juego sólo avanza con GameState.Playing; en menú siempre.
        if (scrollGated)
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.State != GameState.Playing) return;
        }

        float dt = Time.deltaTime;
        float halfH = cam.orthographicSize;

        // --- Suelo: las losas bajan; al salir por abajo se recolocan arriba (bucle) ---
        float move = scrollSpeed * dt;
        foreach (var slab in groundSlabs)
        {
            Vector3 p = slab.position;
            p.y -= move;
            // Si la losa salió completa por abajo, súbela por encima de la pila.
            if (p.y + slabHeight * 0.5f < -halfH)
                p.y += slabsTotalSpan;
            slab.position = p;
        }

        // --- Props: parallax + reciclado lateral ---
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

        // --- Niebla: deriva lenta en X, rebote suave dentro del ancho visible ---
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
    //  ENCAJE AL ASPECT (cielo, viñeta, losas de suelo, niebla)
    // ----------------------------------------------------------------------

    void Resize()
    {
        lastAspect = cam.aspect;
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        float fullH = halfH * 2f;
        float fullW = halfW * 2f;

        // Margen extra para que nunca se vea un borde al temblar la cámara.
        float coverW = fullW + 1.5f;
        float coverH = fullH + 1.5f;

        if (sky != null) sky.localScale = new Vector3(coverW, coverH, 1f);
        if (vignette != null) vignette.localScale = new Vector3(coverW, coverH, 1f);

        // Suelo: cada losa cubre el ancho y un tramo de alto; dos apiladas → bucle.
        slabHeight = coverH; // cada losa cubre la pantalla; dos dan colchón de reciclado
        slabsTotalSpan = slabHeight * groundSlabs.Count;
        for (int i = 0; i < groundSlabs.Count; i++)
        {
            var slab = groundSlabs[i];
            slab.localScale = new Vector3(coverW, slabHeight, 1f);
            // Apilado: losa 0 cubriendo la pantalla, losa 1 justo encima.
            slab.position = new Vector3(0f, i * slabHeight, slab.position.z);
        }

        // Niebla: bandas anchas y bajas a distintas alturas.
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

    /// <summary>
    /// Recoloca un prop arriba con tipo/lado/escala/parallax nuevos. En 'initial'
    /// reparte la altura por su 'slot' para escalonar la entrada.
    /// </summary>
    void RecycleProp(Prop pr, bool initial, int slot, int total)
    {
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        // Tipo de prop aleatorio.
        int type = Random.Range(0, propSprites.Length);
        pr.sr.sprite = propSprites[type];

        // Lado: izquierda o derecha, en la franja lateral (fuera de la zona jugable).
        bool left = Random.value < 0.5f;
        // Banda lateral: desde ~0.62*halfW hasta el borde, con jitter.
        float lateral = Random.Range(0.62f, 0.98f) * halfW;
        float x = left ? -lateral : lateral;

        // ¿Lejano o cercano? Decide parallax, escala, sorting y oscuridad.
        bool near = Random.value < 0.5f;
        if (near)
        {
            pr.parallax = Random.Range(1.0f, 1.2f);   // más rápido que el suelo
            float s = Random.Range(1.3f, 1.9f);
            pr.t.localScale = new Vector3(s, s, 1f);
            pr.sr.sortingOrder = SORT_PROP_NEAR;
            pr.sr.color = Color.white; // silueta a plena oscuridad (PROP_DARK ya es casi negro)
        }
        else
        {
            pr.parallax = Random.Range(0.5f, 0.75f);  // más lento (lejano)
            float s = Random.Range(0.8f, 1.2f);
            pr.t.localScale = new Vector3(s, s, 1f);
            pr.sr.sortingOrder = SORT_PROP_FAR;
            pr.sr.color = new Color(0.7f, 0.7f, 0.8f, 0.9f); // atenuado por distancia
        }

        // Espejado horizontal aleatorio para variar la silueta.
        if (Random.value < 0.5f)
        {
            var sc = pr.t.localScale; sc.x = -sc.x; pr.t.localScale = sc;
        }

        float y;
        if (initial)
        {
            // Reparte por toda la altura (más un poco arriba) escalonando por slot.
            float span = halfH * 2f + 3f;
            y = -halfH + (slot + 0.5f) / total * span;
        }
        else
        {
            // Reaparece por encima del borde superior, con un poco de jitter.
            y = halfH + Random.Range(0.5f, 2.5f);
        }

        pr.t.position = new Vector3(x, y, 0f);
    }

    // ----------------------------------------------------------------------
    //  HELPERS
    // ----------------------------------------------------------------------

    /// <summary>Crea un quad-sprite hijo del Environment con un sorting dado.</summary>
    GameObject MakeQuad(string name, Sprite spr, Color color, int sortingOrder)
    {
        var go = Prims.MakeSprite(name, spr, color, Vector2.one, Vector3.zero, sortingOrder);
        go.transform.SetParent(transform, false);
        return go;
    }

    /// <summary>Convierte un hex "RRGGBB" en Color (alpha 1).</summary>
    static Color Hex(string rgb)
    {
        byte r = (byte)System.Convert.ToInt32(rgb.Substring(0, 2), 16);
        byte g = (byte)System.Convert.ToInt32(rgb.Substring(2, 2), 16);
        byte b = (byte)System.Convert.ToInt32(rgb.Substring(4, 2), 16);
        return new Color32(r, g, b, 255);
    }

    // --- Utilidades de pintado en arrays de Color (texturas de props) ---

    static Color[] NewClear(int s)
    {
        var px = new Color[s * s];
        for (int i = 0; i < px.Length; i++) px[i] = new Color(0f, 0f, 0f, 0f);
        return px;
    }

    static void Set(Color[] px, int s, int x, int y, Color c)
    {
        if (x < 0 || y < 0 || x >= s || y >= s) return;
        px[y * s + x] = c;
    }

    static void Blend(Color[] px, int s, int x, int y, Color c)
    {
        if (x < 0 || y < 0 || x >= s || y >= s) return;
        Color dst = px[y * s + x];
        float a = c.a + dst.a * (1f - c.a);
        Color rgb = Color.Lerp(dst, c, c.a);
        rgb.a = a;
        px[y * s + x] = rgb;
    }

    static void DrawLine(Color[] px, int s, int x0, int y0, int x1, int y1, Color c)
    {
        // Bresenham simple.
        int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;
        while (true)
        {
            Set(px, s, x0, y0, c);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }
    }

    static Sprite ToSprite(Color[] px, int s, FilterMode filter)
    {
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false)
        {
            filterMode = filter,
            wrapMode = TextureWrapMode.Clamp
        };
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
    }
}
