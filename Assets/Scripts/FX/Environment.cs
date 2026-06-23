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

    // --- Estado de scroll ---
    Camera cam;
    float scrollSpeed;
    bool scrollGated;

    // --- Cielo ---
    Transform sky;

    // --- Suelo: losas que se reciclan ---
    readonly List<Transform> groundSlabs = new List<Transform>();
    float slabHeight;
    float slabsTotalSpan;

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
        BuildGround();
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
        // Tile de asfalto de Resources/Art/environment/. Si falta, ArtCache
        // devuelve null y usamos un sprite blanco tintado como fallback.
        var tile = ArtCache.Sprite("environment/road_asphalt01");
        if (tile == null)
        {
            Debug.LogWarning("[Environment] road_asphalt01 no encontrado; usando fallback blanco.");
            tile = Sprite.Create(Texture2D.whiteTexture,
                new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(0.5f, 0.5f), Texture2D.whiteTexture.width);
        }

        // El tile repite verticalmente (wrapMode Repeat) para un scroll sin costuras.
        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.mainTexture = tile.texture;
        mat.SetTextureScale("_MainTex", new Vector2(1f, 2f)); // repite 2 veces por losa

        for (int i = 0; i < 2; i++)
        {
            var go = MakeQuad("GroundSlab" + i, tile, Color.white, SORT_GROUND);
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sharedMaterial = mat;
            groundSlabs.Add(go.transform);
        }
    }

    /// <summary>Props laterales cargados desde ArtCache, en pool de parallax.</summary>
    void BuildProps()
    {
        // Cargar los props de Kenney desde Resources/Art/environment/.
        var names = new[]
        {
            "environment/prop_barrel_blue",
            "environment/prop_barrel_red",
            "environment/prop_cone",
            "environment/prop_rock1",
            "environment/prop_rock2",
            "environment/prop_rock3",
            "environment/prop_barrier_red",
            "environment/prop_barrier_white",
            "environment/prop_light_white",
            "environment/prop_light_yellow",
        };

        var list = new List<Sprite>();
        int idx = 0;
        foreach (var n in names)
        {
            var s = ArtCache.Sprite(n);
            if (s != null)
            {
                list.Add(s);
                if (n.Contains("light")) lightPropIndices.Add(idx);
                idx++;
            }
        }

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

        // Suelo: cada losa cubre la pantalla; dos apiladas → bucle.
        slabHeight = coverH;
        slabsTotalSpan = slabHeight * groundSlabs.Count;
        for (int i = 0; i < groundSlabs.Count; i++)
        {
            var slab = groundSlabs[i];
            slab.localScale = new Vector3(coverW, slabHeight, 1f);
            slab.position = new Vector3(0f, i * slabHeight, slab.position.z);
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

        bool left = Random.value < 0.5f;
        float lateral = Random.Range(0.62f, 0.98f) * halfW;
        float x = left ? -lateral : lateral;

        bool near = Random.value < 0.5f;
        if (near)
        {
            pr.parallax = Random.Range(1.0f, 1.2f);
            float s = Random.Range(1.3f, 1.9f);
            pr.t.localScale = new Vector3(s, s, 1f);
            pr.sr.sortingOrder = SORT_PROP_NEAR;
            pr.sr.color = Color.white;
        }
        else
        {
            pr.parallax = Random.Range(0.5f, 0.75f);
            float s = Random.Range(0.8f, 1.2f);
            pr.t.localScale = new Vector3(s, s, 1f);
            pr.sr.sortingOrder = SORT_PROP_FAR;
            pr.sr.color = PROP_FAR_TINT;
        }

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
