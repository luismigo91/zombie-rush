using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Variedades de zombie. Además de los "sabores" de stats (Runner/Tank), las
/// variedades con comportamiento propio crean COUNTERPLAY (respuestas distintas
/// a "dispara más"): al Exploder mátalo LEJOS, al Spitter esquívale o priorízalo,
/// al Screamer bájalo antes de que acelere a la horda.
/// </summary>
public enum EnemyKind { Normal, Runner, Tank, Exploder, Spitter, Screamer }

/// <summary>
/// Enemigo (zombie). Avanza hacia el escuadrón; al tocarle el frente le hace bajas
/// y desaparece (kamikaze), salvo el JEFE, que golpea y rebota sin morir. Muere al
/// recibir daño de las balas y, al morir por balas, suelta una moneda (no si te
/// alcanza: ahí ya te dañó).
///
/// Usa un POOL estático (se reactiva en vez de instanciar/destruir) porque las
/// hordas generan muchos zombies. El pool tolera la recarga de escena: los objetos
/// destruidos por Unity quedan como referencias nulas y se descartan al sacarlos
/// (igual que Bullet). El jefe NO se poolea (es único por nivel-jefe).
///
/// JEFES por acto (bossPattern): 0 Invocador (adds), 1 Embestida (telegrafiada,
/// castiga quedarse en su carril), 2 Escupidor (abanicos de proyectiles esquivables),
/// 3 Combinado (rota los tres y se enfurece a mitad de vida).
///
/// Mantiene Enemy.All (alta/baja por OnEnable/OnDisable) para barras de jefe,
/// la granada del escuadrón y las búsquedas de área (grito, explosión).
/// </summary>
public class Enemy : MonoBehaviour, IShootable
{
    static readonly Stack<Enemy> pool = new Stack<Enemy>();

    public static readonly List<Enemy> All = new List<Enemy>();

    public float maxHealth = 30f;
    public float Health { get; private set; }
    public float moveSpeed = 2f;
    public int coinValue = 1;
    public bool isBoss;
    public EnemyKind kind = EnemyKind.Normal;

    // Guard de re-entrada en la muerte: la explosión en cadena del Exploder hacía
    // Die(A)→TakeDamage(B)→Die(B)→TakeDamage(A)→Die(A)… (A seguía con Health≤0 y
    // "volvía a morir") → recursión infinita → stack overflow (crash en el Pixel).
    bool dying;

    Transform target;
    SpriteRenderer sr;
    Color baseColor;
    float flashT; // temporizador del destello blanco al recibir daño

    // Comportamiento por variedad.
    float boostT;  // aceleración por grito de screamer (decae sola)
    float spitT;   // cadencia del spitter
    float screamT; // cadencia del screamer

    // Estado de jefe.
    float bossAttackT;   // temporizador del siguiente ataque
    int bossPattern;     // 0 invocador, 1 embestida, 2 escupidor, 3 combinado
    int bossCombo;       // rotación de ataques del patrón combinado
    bool charging;       // en plena embestida
    float chargeT;       // tiempo restante de embestida
    Vector2 chargeDir;   // dirección fijada al arrancar la embestida (esquivable)
    float bossContactT;  // cooldown del golpe de contacto (para no drenar por frame)

    // Los sprites Kenney top-down-shooter vienen en vista lateral (mirando a la
    // derecha). Los zombies avanzan hacia abajo → −90° Z (CW) pone la cabeza abajo
    // (en el frente que los erosiona). El jefe usa el mismo facing.
    static readonly Quaternion Facing = Quaternion.Euler(0f, 0f, -90f);

    /// <summary>Saca un zombie del pool (o crea uno) y lo lanza con sus stats.</summary>
    public static Enemy Spawn(Vector3 pos, float health, float speed, int coins,
        Color color, Vector2 size, EnemyKind kind = EnemyKind.Normal)
    {
        Enemy e = null;
        while (pool.Count > 0)
        {
            e = pool.Pop();
            if (e != null) break; // descarta nulos (destruidos por recarga de escena)
            e = null;
        }

        if (e == null) e = Create(pos, color, size);
        else e.ResetVisual(pos, color, size);

        e.isBoss = false;
        e.kind = kind;
        e.boostT = 0f;
        e.spitT = 0.9f;   // primer escupitajo pronto (el fuego del escuadrón lo mata rápido)
        e.screamT = 2.5f; // primer grito con respiro
        e.charging = false;
        e.bossContactT = 0f;
        e.bossCombo = 0;
        e.Init(health, speed, coins);

        // Animación de arrastre (grises tintables → el color por tipo sigue tiñendo
        // todos los frames). El offset de fase evita que la horda marche sincronizada.
        var anim = e.GetComponent<SpriteAnim>();
        if (anim != null) { anim.enabled = true; SpriteAnim.Play(e.gameObject, ArtCache.ZombieShamble, 4f, true); }

        if (!e.gameObject.activeSelf) e.gameObject.SetActive(true);
        Vfx.Pop(e.transform); // pop de escala al aparecer
        return e;
    }

    static Enemy Create(Vector3 pos, Color color, Vector2 size)
    {
        GameObject go = Prims.MakeSprite("Enemy", ArtCache.Zombie, color, size, pos, sortingOrder: 1);
        go.transform.rotation = Facing;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.useFullKinematicContacts = true;

        var e = go.AddComponent<Enemy>();
        e.sr = go.GetComponent<SpriteRenderer>();
        e.baseColor = color;
        return e;
    }

    void ResetVisual(Vector3 pos, Color color, Vector2 size)
    {
        transform.SetPositionAndRotation(pos, Facing);
        transform.localScale = new Vector3(size.x, size.y, 1f);
        if (sr != null)
        {
            sr.sprite = ArtCache.Zombie;
            sr.color = color;
            sr.sortingOrder = 1;
        }
        baseColor = color;
        flashT = 0f;
    }

    /// <summary>Crea el jefe del nivel: silueta masiva, mucha vida y patrón por acto.</summary>
    public static Enemy SpawnBoss(Vector3 pos, float hp)
    {
        var e = Spawn(pos, hp, 1.3f, 25, new Color(0.45f, 0.70f, 0.20f), new Vector2(1.7f, 1.7f));
        e.isBoss = true;

        // Patrón según el acto (nivel/oleada 10 → invocador ... 40+ → combinado).
        int lvl = GameManager.Instance != null ? GameManager.Instance.Level : 10;
        e.bossPattern = Mathf.Clamp((lvl - 1) / 10, 0, 3);

        if (e.sr != null)
        {
            e.sr.sortingOrder = 2;
            // Sprite dedicado de jefe (color final propio): se pinta tal cual.
            e.sr.sprite = ArtCache.Boss;
            e.sr.color = Color.white;
            e.baseColor = Color.white;
        }
        // El jefe no usa la animación de arrastre: apagamos el SpriteAnim para que
        // no machaque el sprite de jefe cada frame.
        var anim = e.GetComponent<SpriteAnim>();
        if (anim != null) anim.enabled = false;

        // Entrada de jefe: rugido grave + sacudida fuerte.
        Sfx.BossRoar();
        CameraShake.Shake(0.35f, 0.4f);
        e.bossAttackT = 3f; // primer ataque tras un pequeño respiro
        return e;
    }

    public void Init(float hp, float spd, int coins)
    {
        maxHealth = hp;
        Health = hp;
        moveSpeed = spd;
        coinValue = coins;
        dying = false; // reuso del pool: vuelve a estar vivo
    }

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    void Start()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr != null && baseColor == default) baseColor = sr.color;
        TryAcquireTarget();
    }

    void TryAcquireTarget()
    {
        var gm = GameManager.Instance;
        if (gm != null && gm.Squad != null)
            target = gm.Squad.transform;
    }

    /// <summary>Acelera temporalmente a este zombie (grito del screamer).</summary>
    public void Boost(float seconds) => boostT = Mathf.Max(boostT, seconds);

    /// <summary>Despawn SIN botín (limpieza del revive). El jefe no se esfuma.
    /// Gore MUESTREADO: el revive esfuma a toda la horda en un frame y cien
    /// bursts simultáneos daban un tirón.</summary>
    public void Vanish()
    {
        if (isBoss) return;
        if (Random.value < 0.25f) Vfx.Gore(transform.position, baseColor);
        Despawn();
    }

    void Update()
    {
        // El destello se restaura siempre, aunque la partida esté en game over.
        if (flashT > 0f)
        {
            flashT -= Time.deltaTime;
            if (flashT <= 0f && sr != null) sr.color = baseColor;
        }

        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;

        if (target == null) TryAcquireTarget();

        boostT -= Time.deltaTime;

        Squad squad = gm.Squad;
        bool shielded = PowerUpManager.Instance != null && PowerUpManager.Instance.ShieldActive;
        float dist = float.MaxValue;
        if (squad != null && squad.Count > 0)
            dist = Vector2.Distance(transform.position, squad.transform.position);

        // ---- Contacto con el frente del escuadrón ----
        if (isBoss)
        {
            // El jefe NO muere al tocar: golpea (−4), rebota y sigue. Antes hacía el
            // kamikaze 1:1 → se le podía "tanquear" con 1 soldado y ganar gratis.
            bossContactT -= Time.deltaTime;
            float reach = squad != null ? squad.Radius + 0.8f : 0f;
            if (squad != null && squad.Count > 0 && bossContactT <= 0f && dist <= reach)
            {
                if (!shielded)
                {
                    squad.RemoveFront(4);
                    Hud.FlashDamage();
                }
                Sfx.Hurt();
                CameraShake.Shake(0.15f, 0.25f);
                bossContactT = 1.2f;
                charging = false;
                transform.position += Vector3.up * 2.8f; // rebote: se recoloca arriba
            }
        }
        else if (squad != null && squad.Count > 0 && dist <= squad.Radius + 0.25f)
        {
            // Contacto kamikaze: el Exploder revienta en área (−3); el resto 1:1.
            // Con ESCUDO activo, el zombie muere sin bajar soldados.
            if (!shielded)
            {
                squad.RemoveFront(kind == EnemyKind.Exploder ? 3 : 1);
                Hud.FlashDamage();
            }
            if (kind == EnemyKind.Exploder)
            {
                HitEffect.Burst(transform.position, new Color(1f, 0.55f, 0.2f), 10, 6f, 0.15f, 0.28f);
                CameraShake.Shake(0.08f, 0.12f);
                Sfx.Explosion();
            }
            Sfx.Hurt();
            Despawn();
            return;
        }

        // ---- Movimiento ----
        if (isBoss && charging)
        {
            transform.position += (Vector3)(chargeDir * moveSpeed * 3.5f * Time.deltaTime);
        }
        else if (target != null)
        {
            // El spitter a tiro SIGUE entrando pero muy lento (si se plantaba del
            // todo, los escupidores se acumulaban para siempre a media pantalla y
            // rompían el ritmo de oleadas). El jefe escupidor sí mantiene distancia.
            float advanceMul = 1f;
            if (kind == EnemyKind.Spitter && dist < 4.8f) advanceMul = 0.3f;
            else if (isBoss && bossPattern == 2 && dist < 4.2f) advanceMul = 0f;
            if (advanceMul > 0f)
            {
                Vector2 dir = ((Vector2)(target.position - transform.position)).normalized;
                // Ralentización por power-up Slow + aceleración por grito de screamer.
                float speedMul = PowerUpManager.Instance != null ? PowerUpManager.Instance.HordeSpeedFactor : 1f;
                if (boostT > 0f) speedMul *= 1.6f;
                transform.position += (Vector3)(dir * moveSpeed * speedMul * advanceMul * Time.deltaTime);
            }
        }

        // ---- Comportamiento por variedad / patrón de jefe ----
        if (isBoss) BossBrain();
        else if (kind == EnemyKind.Spitter) SpitBrain(dist);
        else if (kind == EnemyKind.Screamer) ScreamBrain();
    }

    // ------------------------------------------------------------- variedades

    /// <summary>Spitter: escupe hacia el escuadrón casi desde que entra en pantalla
    /// (esquivable). Con el rango corto original (6.5u) el fuego del escuadrón lo
    /// mataba casi siempre ANTES de llegar a disparar (playtest) — su nicho de
    /// "artillería" nunca se activaba.</summary>
    void SpitBrain(float dist)
    {
        if (dist > 8.5f) return; // fuera de pantalla aún: baja sin escupir

        spitT -= Time.deltaTime;
        if (spitT > 0f) return;
        spitT = 2.2f;

        var gm = GameManager.Instance;
        if (gm == null || gm.Squad == null || gm.Squad.Count == 0) return;
        Vector2 d = ((Vector2)(gm.Squad.transform.position - transform.position)).normalized;
        EnemyShot.Spawn(transform.position + (Vector3)(d * 0.4f), d, 5f);
        HitEffect.Burst(transform.position, new Color(0.55f, 0.95f, 0.30f), 4, 3f, 0.1f, 0.15f);
    }

    /// <summary>Screamer: grito periódico que acelera a los zombies cercanos.</summary>
    void ScreamBrain()
    {
        screamT -= Time.deltaTime;
        if (screamT > 0f) return;
        screamT = 5f;

        int boosted = 0;
        for (int i = 0; i < All.Count; i++)
        {
            var e = All[i];
            if (e == null || e == this || e.isBoss) continue;
            if (((Vector2)(e.transform.position - transform.position)).sqrMagnitude <= 3.2f * 3.2f)
            {
                e.Boost(2f);
                boosted++;
            }
        }
        if (boosted > 0)
        {
            // Onda magenta: se lee "ese chillón está potenciando a la horda".
            HitEffect.Burst(transform.position, new Color(0.95f, 0.30f, 0.85f), 12, 6f, 0.14f, 0.35f);
            Sfx.Scream();
        }
    }

    // ------------------------------------------------------------------ jefes

    void BossBrain()
    {
        if (charging)
        {
            chargeT -= Time.deltaTime;
            // Corta la embestida al agotarse o al llegar abajo (el escuadrón esquivó).
            float bottom = -(Camera.main != null ? Camera.main.orthographicSize : 5f) + 1f;
            if (chargeT <= 0f || transform.position.y <= bottom) charging = false;
            return;
        }

        bossAttackT -= Time.deltaTime;
        if (bossAttackT > 0f) return;

        // Combinado: rota los tres ataques y se ENFURECE a mitad de vida.
        bool enraged = bossPattern == 3 && Health < maxHealth * 0.5f;
        int attack = bossPattern == 3 ? bossCombo++ % 3 : bossPattern;
        float cd;
        switch (attack)
        {
            case 1: StartCharge(); cd = 5.5f; break;
            case 2: SpitFan(enraged ? 7 : 5); cd = 3.5f; break;
            default: SummonAdds(enraged ? 3 : 2); cd = 4f; break;
        }
        bossAttackT = cd * (enraged ? 0.65f : 1f);
    }

    /// <summary>Invoca mini-zombies rápidos bajo el jefe (presión de contacto).</summary>
    void SummonAdds(int count)
    {
        for (int i = 0; i < count; i++)
        {
            float px = transform.position.x + Random.Range(-1.2f, 1.2f);
            Spawn(new Vector3(px, transform.position.y - 0.6f, 0f),
                maxHealth * 0.12f, moveSpeed * 1.4f, 1,
                new Color(0.50f, 0.69f, 0.31f), new Vector2(0.45f, 0.45f));
        }
        Sfx.BossRoar();
        CameraShake.Shake(0.12f, 0.18f);
    }

    /// <summary>Embestida: fija dirección al escuadrón y carga (rugido = telegrafía).</summary>
    void StartCharge()
    {
        var gm = GameManager.Instance;
        chargeDir = gm != null && gm.Squad != null
            ? ((Vector2)(gm.Squad.transform.position - transform.position)).normalized
            : Vector2.down;
        charging = true;
        chargeT = 1.1f;
        Sfx.BossRoar();
        CameraShake.Shake(0.15f, 0.2f);
        HitEffect.Burst(transform.position, new Color(1f, 0.3f, 0.25f), 10, 6f, 0.15f, 0.3f);
    }

    /// <summary>Abanico de escupitajos hacia el escuadrón (huecos entre trayectorias).</summary>
    void SpitFan(int count)
    {
        var gm = GameManager.Instance;
        Vector2 baseDir = gm != null && gm.Squad != null
            ? ((Vector2)(gm.Squad.transform.position - transform.position)).normalized
            : Vector2.down;
        for (int i = 0; i < count; i++)
        {
            float off = count == 1 ? 0f : Mathf.Lerp(-28f, 28f, i / (count - 1f));
            Vector2 d = Quaternion.Euler(0f, 0f, off) * baseDir;
            EnemyShot.Spawn(transform.position + (Vector3)(d * 0.9f), d, 4.2f);
        }
        Sfx.BossRoar();
    }

    // ------------------------------------------------------------------- daño

    /// <summary>Implementación de IShootable: las balas llaman aquí.</summary>
    public void TakeHit(float damage) => TakeDamage(damage);

    public void TakeDamage(float damage)
    {
        if (dying) return; // ya está muriendo: ignora daño extra (corta recursiones)

        // Invulnerable hasta ENTRAR en pantalla: el combate solo ocurre a la
        // vista (refuerza el corte de balas en el borde superior de Bullet).
        float screenTop = (Camera.main != null ? Camera.main.orthographicSize : 5f) + 0.2f;
        if (transform.position.y > screenTop) return;

        if (sr == null)
        {
            sr = GetComponent<SpriteRenderer>();
            if (sr != null) baseColor = sr.color;
        }

        Health -= damage;

        // Feedback de impacto: destello blanco siempre; número y chispa MUESTREADOS
        // (~1 de cada 3): con ~30 impactos/seg la nube de números tapaba el juego
        // (playtest: "no se ve qué pasa"). El jefe siempre muestra su daño.
        flashT = 0.07f;
        if (sr != null) sr.color = Color.white;
        if (isBoss || Random.value < 0.33f)
        {
            FloatingTextManager.Spawn(transform.position, Mathf.RoundToInt(damage).ToString(), new Color(1f, 0.95f, 0.5f));
            HitEffect.Burst(transform.position, new Color(1f, 0.9f, 0.4f), 3, 4f, 0.11f, 0.2f);
        }

        if (Health <= 0f)
            Die();
    }

    void Die()
    {
        if (dying) return; // doble seguro (además del guard de TakeDamage)
        dying = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddKill();
            GameManager.Instance.AddCoins(coinValue); // monedas de la run → banco al acabar
        }

        // Exploder: matarlo LEJOS recompensa — su explosión daña a los zombies
        // cercanos (cadenas). Snapshot de All: la cadena puede matar y mutar la lista.
        if (kind == EnemyKind.Exploder)
        {
            var snapshot = All.ToArray();
            foreach (var e in snapshot)
            {
                if (e == null || e == this) continue;
                float d = ((Vector2)(e.transform.position - transform.position)).magnitude;
                if (d <= 2.3f) e.TakeDamage(maxHealth * 1.5f);
            }
            HitEffect.Burst(transform.position, new Color(1f, 0.55f, 0.2f), 12, 7f, 0.16f, 0.3f);
            CameraShake.Shake(0.08f, 0.12f);
            Sfx.Explosion();
        }

        // Gore verdoso en el tinte del zombie + micro hit-stop + sacudida + sonido.
        Vfx.Gore(transform.position, baseColor);
        // Hit-stop solo en el JEFE: congelar el tiempo en cada muerte normal (varias
        // por segundo) hacía que el juego "se rallara" a micro-parones (playtest).
        if (isBoss) Vfx.HitStop(0.08f);
        // Solo sacude la muerte del JEFE: con varias muertes por segundo el shake
        // continuo mareaba (feedback de playtest).
        if (isBoss) CameraShake.Shake(0.3f, 0.3f);
        // Solo vibra el JEFE: con hordas de supervivencia mueren varios zombies por
        // segundo y vibrar por muerte era un zumbido agresivo (feedback de playtest).
        if (isBoss) Haptics.Heavy();
        Sfx.Death();

        // Suelta un power-up con baja probabilidad (el jefe siempre suelta uno).
        float scroll = LevelRunner.Instance != null ? LevelRunner.Instance.ScrollSpeed : 2f;
        PowerUp.MaybeDrop(transform.position, scroll, force: isBoss);

        // El jefe no se poolea (sprite/animación propios); el resto vuelve al pool.
        if (isBoss) Destroy(gameObject);
        else Despawn();
    }

    void Despawn()
    {
        gameObject.SetActive(false);
        pool.Push(this);
    }
}
