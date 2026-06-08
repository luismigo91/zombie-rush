using System.Collections;
using UnityEngine;

/// <summary>
/// Fachada estática de efectos "jugosos" (game-feel) construida 100% por código:
/// fogonazo de boca, gore al morir un zombie, chispas de impacto, pop de recoger
/// moneda, pop de escala al spawnear, micro hit-stop y confeti de victoria.
///
/// Todo se genera con primitivas (Prims.Make / Prims.MakeSprite, cuadrado blanco
/// tintado) y reutiliza lo existente: HitEffect.Burst, CameraShake.Shake y
/// FloatingTextManager.Spawn. Sin prefabs, PNG ni WAV. Respeta el SORTING de VFX
/// (rango 8..12): destellos/gore/impacto/coin en 8..10, confeti/números en 12.
///
/// No requiere cableado: el runner de corutinas (VfxRunner) se autocrea de forma
/// perezosa y se hace DontDestroyOnLoad la primera vez que se llama a cualquier
/// método. Basta invocar los estáticos.
/// </summary>
public static class Vfx
{
    // --- paleta usada por los efectos (mood "noche apocalíptica neón") ---
    static readonly Color BulletCore = new Color(1f, 0.823f, 0.227f); // #FFD23A
    static readonly Color BulletTail = new Color(1f, 0.478f, 0.102f); // #FF7A1A
    static readonly Color MuzzleHalo = new Color(1f, 0.969f, 0.800f); // #FFF7CC
    static readonly Color CoinGold = new Color(1f, 0.823f, 0.227f);   // #FFD23A
    static readonly Color CoinHi = new Color(1f, 0.941f, 0.627f);     // #FFF0A0

    // Colores de confeti (paleta de acentos UI).
    static readonly Color[] ConfettiColors =
    {
        new Color(0.239f, 0.839f, 0.961f), // cian   #3DD6F5
        new Color(1f, 0.302f, 0.553f),     // magenta #FF4D8D
        new Color(1f, 0.823f, 0.227f),     // oro    #FFD23A
        new Color(0.357f, 0.839f, 0.416f), // lima   #5BD66A
        new Color(0.957f, 0.945f, 0.910f), // hueso  #F4F1E8
    };

    // ------------------------------------------------------------------
    //  FOGONAZO DE BOCA
    // ------------------------------------------------------------------

    /// <summary>Destello breve de fogonazo: un quad amarillo que crece y se desvanece, con micro-shake.</summary>
    public static void Muzzle(Vector3 pos)
    {
        // Quad amarillo (sin depender de PixelArt.Muzzle, que es de otra área):
        // un cuadrado tintado vale como destello y nunca rompe la compilación.
        var go = Prims.Make("VfxMuzzle", BulletCore, new Vector2(0.35f, 0.35f), pos, sortingOrder: 10);
        var halo = Prims.Make("VfxMuzzleHalo", new Color(MuzzleHalo.r, MuzzleHalo.g, MuzzleHalo.b, 0.7f),
            new Vector2(0.5f, 0.5f), pos, sortingOrder: 9);
        halo.transform.SetParent(go.transform, true);

        VfxRunner.Inst.StartCoroutine(MuzzleRoutine(go));
        CameraShake.Shake(0.04f, 0.05f); // micro kick
    }

    static IEnumerator MuzzleRoutine(GameObject go)
    {
        var sr = go.GetComponent<SpriteRenderer>();
        var haloSr = go.transform.childCount > 0 ? go.transform.GetChild(0).GetComponent<SpriteRenderer>() : null;
        Vector3 baseScale = go.transform.localScale;
        const float life = 0.075f;
        float age = 0f;

        while (age < life)
        {
            age += Time.unscaledDeltaTime; // independiente de hit-stop
            float t = Mathf.Clamp01(age / life);
            // Crece de 0.6x a 1.4x y se apaga.
            float s = Mathf.Lerp(0.6f, 1.4f, t);
            go.transform.localScale = baseScale * s;
            float a = 1f - t;
            SetAlpha(sr, a);
            SetAlpha(haloSr, a * 0.7f);
            yield return null;
        }
        if (go != null) Object.Destroy(go);
    }

    // ------------------------------------------------------------------
    //  GORE (muerte de zombie)
    // ------------------------------------------------------------------

    /// <summary>
    /// Estallido rico de partículas en el color del zombie: salpicaduras con
    /// gravedad y giro, un núcleo brillante rápido (HitEffect) y una mancha
    /// persistente en el suelo del impacto que se desvanece lenta.
    /// </summary>
    public static void Gore(Vector3 pos, Color tint)
    {
        // Núcleo brillante e instantáneo reutilizando el burst existente.
        HitEffect.Burst(pos, tint, 6, 4.5f, 0.16f, 0.22f);

        // Salpicaduras propias con gravedad (caen) y giro.
        int count = Random.Range(14, 19);
        for (int i = 0; i < count; i++)
        {
            float size = Random.Range(0.07f, 0.18f);
            var go = Prims.Make("VfxGore", tint, new Vector2(size, size), pos, sortingOrder: 9);
            float ang = Random.Range(0f, Mathf.PI * 2f);
            float spd = Random.Range(2.5f, 6.5f);
            Vector2 vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd;
            vel.y += Random.Range(1.5f, 4f); // sesgo hacia arriba antes de caer
            go.AddComponent<VfxParticle>().Init(vel, Random.Range(0.3f, 0.6f),
                gravity: -9f, friction: 1.5f, spin: Random.Range(-720f, 720f), shrink: true);
        }

        // Mancha plana oscurecida del tinte que se queda en el suelo.
        Color stainCol = new Color(tint.r * 0.45f, tint.g * 0.45f, tint.b * 0.45f, 0.55f);
        var stain = Prims.Make("VfxStain", stainCol, new Vector2(0.7f, 0.45f), pos, sortingOrder: 8);
        VfxRunner.Inst.StartCoroutine(FadeAndDestroy(stain, 0.5f, hold: 0.1f));
    }

    // ------------------------------------------------------------------
    //  IMPACTO DE BALA
    // ------------------------------------------------------------------

    /// <summary>Chispa pequeña y rápida al impactar una bala (sin gravedad).</summary>
    public static void BulletImpact(Vector3 pos)
    {
        int count = Random.Range(3, 6);
        for (int i = 0; i < count; i++)
        {
            Color c = Random.value < 0.5f ? BulletCore : BulletTail;
            float size = Random.Range(0.05f, 0.11f);
            var go = Prims.Make("VfxSpark", c, new Vector2(size, size), pos, sortingOrder: 9);
            float ang = Random.Range(0f, Mathf.PI * 2f);
            float spd = Random.Range(3.5f, 7f);
            Vector2 vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd;
            go.AddComponent<VfxParticle>().Init(vel, Random.Range(0.12f, 0.2f),
                gravity: 0f, friction: 6f, spin: 0f, shrink: true);
        }
    }

    // ------------------------------------------------------------------
    //  RECOGER MONEDA
    // ------------------------------------------------------------------

    /// <summary>Destello dorado + chispas + micro-texto "+1" al recoger una moneda.</summary>
    public static void CoinPickup(Vector3 pos)
    {
        // Destello que crece y se apaga.
        var flash = Prims.Make("VfxCoinFlash", CoinGold, new Vector2(0.3f, 0.3f), pos, sortingOrder: 10);
        VfxRunner.Inst.StartCoroutine(CoinFlashRoutine(flash));

        // Chispas doradas hacia arriba.
        int count = Random.Range(3, 5);
        for (int i = 0; i < count; i++)
        {
            float size = Random.Range(0.05f, 0.1f);
            var go = Prims.Make("VfxCoinSpark", CoinHi, new Vector2(size, size), pos, sortingOrder: 10);
            float ang = Random.Range(Mathf.PI * 0.25f, Mathf.PI * 0.75f); // arco hacia arriba
            float spd = Random.Range(2f, 4f);
            Vector2 vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd;
            go.AddComponent<VfxParticle>().Init(vel, Random.Range(0.25f, 0.4f),
                gravity: -5f, friction: 2f, spin: 0f, shrink: true);
        }

        // Micro-texto de feedback.
        FloatingTextManager.Spawn(pos, "+1", CoinGold, 24f, 0.55f);
    }

    static IEnumerator CoinFlashRoutine(GameObject go)
    {
        var sr = go.GetComponent<SpriteRenderer>();
        Vector3 baseScale = go.transform.localScale;
        const float life = 0.18f;
        float age = 0f;
        while (age < life)
        {
            age += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(age / life);
            go.transform.localScale = baseScale * Mathf.Lerp(0.5f, 1.6f, t);
            // Vira de oro a brillo y se apaga.
            Color c = Color.Lerp(CoinGold, CoinHi, t);
            c.a = 1f - t;
            if (sr != null) sr.color = c;
            yield return null;
        }
        if (go != null) Object.Destroy(go);
    }

    // ------------------------------------------------------------------
    //  POP DE ESCALA (al spawnear)
    // ------------------------------------------------------------------

    /// <summary>
    /// Pop de escala con overshoot y asentamiento sobre el Transform al aparecer.
    /// Seguro si t es null. Se lanza desde el runner para funcionar aunque el
    /// llamante no sea MonoBehaviour.
    /// </summary>
    public static void Pop(Transform t)
    {
        if (t == null) return;
        VfxRunner.Inst.StartCoroutine(PopRoutine(t));
    }

    static IEnumerator PopRoutine(Transform t)
    {
        if (t == null) yield break;
        Vector3 baseScale = t.localScale;
        // Si el transform arranca en escala 0 (recién creado), asumimos 1.
        if (baseScale == Vector3.zero) baseScale = Vector3.one;

        const float dur = 0.18f;
        float age = 0f;
        while (age < dur)
        {
            if (t == null) yield break;
            age += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(age / dur);
            // 0.85x -> 1.18x (overshoot) -> 1.0x con ease-out suave.
            float s;
            if (p < 0.5f)
            {
                float k = Mathf.SmoothStep(0f, 1f, p / 0.5f);
                s = Mathf.Lerp(0.85f, 1.18f, k);
            }
            else
            {
                float k = Mathf.SmoothStep(0f, 1f, (p - 0.5f) / 0.5f);
                s = Mathf.Lerp(1.18f, 1f, k);
            }
            t.localScale = baseScale * s;
            yield return null;
        }
        if (t != null) t.localScale = baseScale;
    }

    // ------------------------------------------------------------------
    //  HIT-STOP (micro-pausa reentrante)
    // ------------------------------------------------------------------

    /// <summary>
    /// Micro-pausa congelando Time.timeScale brevemente y restaurándolo a 1.
    /// Reentrante: llamadas solapadas extienden la pausa sin pisarse ni restaurar
    /// antes de tiempo. Usa tiempo real (unscaled), así funciona con timeScale=0.
    /// </summary>
    public static void HitStop(float seconds)
    {
        if (seconds <= 0f) return;
        VfxRunner.Inst.RequestHitStop(seconds);
    }

    // ------------------------------------------------------------------
    //  CONFETI (victoria)
    // ------------------------------------------------------------------

    /// <summary>Lluvia de partículas de colores cayendo desde encima del centro, al ganar.</summary>
    public static void Confetti(Vector3 center)
    {
        int count = Random.Range(28, 41);
        // Semiancho REAL de la cámara (size * aspect); en 9:16 ≈ 2.8, no un valor fijo,
        // para que el confeti no caiga fuera de pantalla por los lados.
        var camMain = Camera.main;
        float halfW = camMain != null ? camMain.orthographicSize * camMain.aspect : 2.8f;
        float topY = center.y + 6f;
        for (int i = 0; i < count; i++)
        {
            Color c = ConfettiColors[Random.Range(0, ConfettiColors.Length)];
            float size = Random.Range(0.1f, 0.2f);
            Vector3 spawn = new Vector3(
                center.x + Random.Range(-halfW, halfW),
                topY + Random.Range(0f, 2.5f),
                center.z);
            var go = Prims.Make("VfxConfetti", c, new Vector2(size, size * Random.Range(0.5f, 1f)), spawn, sortingOrder: 12);
            Vector2 vel = new Vector2(Random.Range(-1.5f, 1.5f), Random.Range(-1f, 0.5f));
            go.AddComponent<VfxParticle>().Init(vel, Random.Range(0.8f, 1.4f),
                gravity: -6f, friction: 0.3f, spin: Random.Range(-540f, 540f), shrink: false);
        }
        CameraShake.Shake(0.08f, 0.2f); // pequeño kick de celebración
    }

    // ------------------------------------------------------------------
    //  utilidades internas
    // ------------------------------------------------------------------

    static void SetAlpha(SpriteRenderer sr, float a)
    {
        if (sr == null) return;
        Color c = sr.color;
        c.a = a;
        sr.color = c;
    }

    /// <summary>Mantiene un objeto unos instantes y luego lo desvanece y destruye.</summary>
    static IEnumerator FadeAndDestroy(GameObject go, float fade, float hold)
    {
        var sr = go != null ? go.GetComponent<SpriteRenderer>() : null;
        float baseA = sr != null ? sr.color.a : 1f;
        float age = 0f;
        while (age < hold)
        {
            age += Time.unscaledDeltaTime;
            yield return null;
        }
        age = 0f;
        while (age < fade)
        {
            if (go == null) yield break;
            age += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(age / fade);
            SetAlpha(sr, baseA * (1f - t));
            yield return null;
        }
        if (go != null) Object.Destroy(go);
    }
}

/// <summary>
/// Runner interno de corutinas para Vfx: singleton perezoso que se autocrea y
/// sobrevive a los cambios de escena (DontDestroyOnLoad). También gestiona el
/// hit-stop de forma reentrante con una única corutina vigilante.
/// </summary>
public class VfxRunner : MonoBehaviour
{
    static VfxRunner _inst;

    /// <summary>Instancia perezosa: la crea por código en el primer acceso.</summary>
    public static VfxRunner Inst
    {
        get
        {
            if (_inst == null)
            {
                var go = new GameObject("VfxRunner");
                _inst = go.AddComponent<VfxRunner>();
                Object.DontDestroyOnLoad(go);
            }
            return _inst;
        }
    }

    // --- estado del hit-stop reentrante ---
    float restoreAt = -1f;      // instante (realtime) en que se restaura timeScale
    bool hitStopActive;         // hay una pausa en curso
    const float FrozenScale = 0.02f; // escala mínima durante la pausa (casi congelado)

    void Awake()
    {
        if (_inst == null) _inst = this;
    }

    /// <summary>Pide un hit-stop de 'seconds'; extiende la pausa si ya hay otra activa.</summary>
    public void RequestHitStop(float seconds)
    {
        float target = Time.realtimeSinceStartup + seconds;
        restoreAt = Mathf.Max(restoreAt, target);

        if (!hitStopActive)
        {
            hitStopActive = true;
            Time.timeScale = FrozenScale;
            StartCoroutine(HitStopWatch());
        }
    }

    IEnumerator HitStopWatch()
    {
        // Espera en tiempo real hasta superar el último 'restoreAt' solicitado.
        while (Time.realtimeSinceStartup < restoreAt)
            yield return new WaitForSecondsRealtime(0.005f);

        Time.timeScale = 1f; // se asume timeScale normal = 1
        hitStopActive = false;
        restoreAt = -1f;
    }
}

/// <summary>
/// Partícula VFX propia, más rica que FxParticle: cuadrado tintado con velocidad
/// inicial, gravedad opcional, fricción, giro y fade por vida. Al expirar se
/// autodestruye. La usan Gore (gravedad + giro), BulletImpact (chispa sin
/// gravedad) y Confetti (gravedad + giro + colores).
/// </summary>
public class VfxParticle : MonoBehaviour
{
    Vector2 vel;
    float life = 0.4f;
    float age;
    float gravity;
    float friction;
    float spin;     // grados/seg
    bool shrink;    // si encoge con la edad
    SpriteRenderer sr;
    Vector3 baseScale;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
    }

    /// <summary>Configura la partícula. gravity en u/s² (negativo = cae); spin en grados/seg.</summary>
    public void Init(Vector2 velocity, float lifetime, float gravity, float friction, float spin, bool shrink)
    {
        vel = velocity;
        life = Mathf.Max(0.01f, lifetime);
        this.gravity = gravity;
        this.friction = friction;
        this.spin = spin;
        this.shrink = shrink;
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
    }

    void Update()
    {
        // Usa tiempo real para no congelarse durante un hit-stop simultáneo.
        float dt = Time.unscaledDeltaTime;
        age += dt;

        vel.y += gravity * dt;
        if (friction > 0f) vel *= Mathf.Max(0f, 1f - friction * dt);
        transform.position += (Vector3)(vel * dt);

        if (spin != 0f) transform.Rotate(0f, 0f, spin * dt);

        float t = Mathf.Clamp01(age / life);
        if (shrink) transform.localScale = baseScale * (1f - t);
        if (sr != null)
        {
            Color c = sr.color;
            c.a = 1f - t;
            sr.color = c;
        }

        if (age >= life) Destroy(gameObject);
    }
}
