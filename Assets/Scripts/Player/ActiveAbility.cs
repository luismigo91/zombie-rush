using UnityEngine;

/// <summary>
/// Habilidad activa del escuadrón (botón del HUD, cooldown propio por tipo). La
/// equipada se elige en el menú (Loadout.Ability):
/// - GRANADA: estalla en el centro de masa de la horda visible (daño en área).
/// - CONGELACIÓN: ralentiza MUCHO a toda la horda unos segundos + daño leve global.
/// - CENTINELA: planta una torreta que dispara sola unos segundos (posicional:
///   vale más que la granada, pero su valor depende de dónde la sueltes).
///
/// El daño escala con el nivel/oleada (GameManager.Level) para seguir siendo
/// relevante toda la campaña. Va en el mismo GameObject que Squad (GameBootstrap).
/// </summary>
public class ActiveAbility : MonoBehaviour
{
    public static ActiveAbility Instance { get; private set; }

    public float radius = 3.4f; // radio de la granada

    float cdT; // cooldown restante (0 = lista)

    /// <summary>Cooldown total de la habilidad equipada.</summary>
    public float Cooldown => Loadout.Ability switch
    {
        AbilityType.Freeze => 24f,
        AbilityType.Sentry => 26f,
        _ => 20f,
    };

    public bool Ready => cdT <= 0f;

    /// <summary>Fracción de cooldown RESTANTE (1 = recién usada, 0 = lista) para el HUD.</summary>
    public float CooldownFrac => Mathf.Clamp01(cdT / Mathf.Max(0.01f, Cooldown));

    void Awake() => Instance = this;

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;
        if (cdT > 0f) cdT -= Time.deltaTime;
    }

    /// <summary>Dispara la habilidad equipada si está lista. True si se usó.</summary>
    public bool TryFire()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing || !Ready) return false;

        bool used = Loadout.Ability switch
        {
            AbilityType.Freeze => FireFreeze(gm),
            AbilityType.Sentry => FireSentry(gm),
            _ => FireGrenade(gm),
        };
        if (used) cdT = Cooldown;
        return used;
    }

    // ---------------------------------------------------------------- granada

    bool FireGrenade(GameManager gm)
    {
        // Centro de masa de los enemigos EN PANTALLA: la granada busca el meollo.
        // Sin objetivo no se consume (evita desperdiciarla con la calle vacía).
        if (!VisibleCentroid(out Vector3 center)) return false;

        float damage = 140f + 22f * gm.Level; // escala con nivel/oleada
        var all = Enemy.All;
        for (int i = all.Count - 1; i >= 0; i--)
        {
            var e = all[i];
            if (e == null) continue;
            float d = ((Vector2)(e.transform.position - center)).magnitude;
            if (d <= radius)
                e.TakeDamage(damage * (1f - d / radius * 0.5f)); // caída al 50 % en el borde
        }

        // Estallido: burst naranja + sacudida + micro hit-stop (sin confeti: eso es de victoria).
        HitEffect.Burst(center, new Color(1f, 0.6f, 0.25f), 14, 7f, 0.16f, 0.35f);
        CameraShake.Shake(0.22f, 0.3f);
        Vfx.HitStop(0.04f);
        Sfx.Explosion();
        Haptics.Medium();
        return true;
    }

    // ----------------------------------------------------------- congelación

    bool FireFreeze(GameManager gm)
    {
        if (!VisibleCentroid(out Vector3 center)) return false;

        // Escarcha global: la horda casi se detiene + daño leve a todos los visibles.
        if (PowerUpManager.Instance != null) PowerUpManager.Instance.GrantFreeze(4f);

        float top = Camera.main != null ? Camera.main.orthographicSize : 5f;
        float chip = 40f + 8f * gm.Level;
        var all = Enemy.All;
        for (int i = all.Count - 1; i >= 0; i--)
        {
            var e = all[i];
            if (e == null || e.transform.position.y > top) continue;
            e.TakeDamage(chip);
        }

        HitEffect.Burst(center, new Color(0.55f, 0.85f, 1f), 16, 6f, 0.15f, 0.4f);
        CameraShake.Shake(0.12f, 0.2f);
        Sfx.Hit();
        Haptics.Medium();
        return true;
    }

    // -------------------------------------------------------------- centinela

    bool FireSentry(GameManager gm)
    {
        // La torreta siempre se puede plantar (su valor es posicional/anticipación).
        Vector3 pos = transform.position + Vector3.up * 0.9f;
        float damage = (140f + 22f * gm.Level) / 16f; // por disparo (5/s durante 8 s ≈ 2.5 granadas)
        Sentry.Spawn(pos, damage);
        Sfx.Gate();
        Haptics.Medium();
        return true;
    }

    /// <summary>Centro de masa de los enemigos visibles; false si no hay ninguno.</summary>
    static bool VisibleCentroid(out Vector3 center)
    {
        float top = Camera.main != null ? Camera.main.orthographicSize : 5f;
        Vector3 sum = Vector3.zero;
        int n = 0;
        var all = Enemy.All;
        for (int i = 0; i < all.Count; i++)
        {
            var e = all[i];
            if (e == null || e.transform.position.y > top) continue;
            sum += e.transform.position;
            n++;
        }
        center = n > 0 ? sum / n : Vector3.zero;
        return n > 0;
    }
}

/// <summary>
/// Torreta CENTINELA: se planta donde estaba el escuadrón, baja con el scroll
/// (comparte plano con el asfalto) y dispara recto hacia arriba unos segundos.
/// Visual: un soldado cian destacado. Sin pool: hay una por uso de habilidad.
/// </summary>
public class Sentry : MonoBehaviour
{
    const float Duration = 8f;
    const float FireRate = 5f;

    float life;
    float fireT;
    float damage;
    float scroll;
    int muzzleTick;

    public static Sentry Spawn(Vector3 pos, float damagePerShot)
    {
        var go = Prims.MakeSprite("Sentry", ArtCache.Soldier, new Color(0.24f, 0.84f, 0.96f),
            new Vector2(0.5f, 0.5f), pos, sortingOrder: 3);
        go.transform.rotation = Quaternion.Euler(0f, 0f, 90f); // mira hacia arriba (como las unidades)

        var s = go.AddComponent<Sentry>();
        s.life = Duration;
        s.damage = damagePerShot;
        s.scroll = LevelRunner.Instance != null ? LevelRunner.Instance.ScrollSpeed : 2f;
        SpriteAnim.Play(go, ArtCache.SoldierMarch, 6f, true);
        Vfx.Pop(go.transform);
        return s;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;

        // Baja con el suelo (está plantada en él) y muere al agotarse o salir.
        transform.position += Vector3.down * (scroll * Time.deltaTime);
        life -= Time.deltaTime;
        float bottom = -(Camera.main != null ? Camera.main.orthographicSize : 5f) - 1f;
        if (life <= 0f || transform.position.y < bottom)
        {
            HitEffect.Burst(transform.position, new Color(0.24f, 0.84f, 0.96f), 6, 4f, 0.12f, 0.25f);
            Destroy(gameObject);
            return;
        }

        fireT -= Time.deltaTime;
        if (fireT > 0f) return;
        fireT = 1f / FireRate;

        var boca = transform.position + Vector3.up * 0.3f;
        Bullet.Spawn(boca, Vector2.up, 16f, damage, pierce: 1);
        if (muzzleTick++ % 3 == 0) Vfx.Muzzle(boca); // fogonazo muestreado (5/s sería ruido)
    }
}
