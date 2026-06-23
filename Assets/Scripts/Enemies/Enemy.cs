using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enemigo (zombie). Avanza hacia el escuadrón; al tocarle el frente le hace una
/// baja 1:1 y desaparece (kamikaze). Muere al recibir daño de las balas y, al morir
/// por balas, suelta una moneda (no si te alcanza: ahí ya te dañó).
///
/// Usa un POOL estático (se reactiva en vez de instanciar/destruir) porque las
/// hordas generan muchos zombies. El pool tolera la recarga de escena: los objetos
/// destruidos por Unity quedan como referencias nulas y se descartan al sacarlos
/// (igual que Bullet). El jefe NO se poolea (es único por nivel-jefe).
///
/// Mantiene Enemy.All (alta/baja por OnEnable/OnDisable) para barras de jefe y
/// búsquedas puntuales.
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

    Transform target;
    SpriteRenderer sr;
    Color baseColor;
    float flashT; // temporizador del destello blanco al recibir daño
    float bossAttackT; // temporizador del patrón de jefe (invoca adds)

    /// <summary>Saca un zombie del pool (o crea uno) y lo lanza con sus stats.</summary>
    public static Enemy Spawn(Vector3 pos, float health, float speed, int coins, Color color, Vector2 size)
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
        e.Init(health, speed, coins);

        // Animación de arrastre (grises tintables → el color por tipo sigue tiñendo
        // todos los frames). El offset de fase evita que la horda marche sincronizada.
        var anim = e.GetComponent<SpriteAnim>();
        if (anim != null) { anim.enabled = true; SpriteAnim.Play(e.gameObject, PixelArt.ZombieShamble, 4f, true); }

        if (!e.gameObject.activeSelf) e.gameObject.SetActive(true);
        Vfx.Pop(e.transform); // pop de escala al aparecer
        return e;
    }

    static Enemy Create(Vector3 pos, Color color, Vector2 size)
    {
        GameObject go = Prims.MakeSprite("Enemy", PixelArt.Zombie, color, size, pos, sortingOrder: 1);

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
        transform.SetPositionAndRotation(pos, Quaternion.identity);
        transform.localScale = new Vector3(size.x, size.y, 1f);
        if (sr != null)
        {
            sr.sprite = PixelArt.Zombie;
            sr.color = color;
            sr.sortingOrder = 1;
        }
        baseColor = color;
        flashT = 0f;
    }

    /// <summary>Crea un mini-jefe: silueta propia masiva, con mucha vida y recompensa.</summary>
    public static Enemy SpawnBoss(Vector3 pos, float hp)
    {
        var e = Spawn(pos, hp, 1.3f, 25, new Color(0.45f, 0.70f, 0.20f), new Vector2(1.7f, 1.7f));
        e.isBoss = true;

        if (e.sr != null)
        {
            e.sr.sortingOrder = 2;
            // Sprite dedicado de jefe (color final propio): se pinta tal cual.
            e.sr.sprite = PixelArt.Boss;
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
        e.bossAttackT = 3f; // primer invocation de adds tras un pequeño respiro
        return e;
    }

    public void Init(float hp, float spd, int coins)
    {
        maxHealth = hp;
        Health = hp;
        moveSpeed = spd;
        coinValue = coins;
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

        // Contacto 1:1: si alcanza el frente del escuadrón, mata 1 soldado y muere.
        // Con ESCUDO activo, el zombie muere sin bajar soldados.
        Squad squad = gm.Squad;
        bool shielded = PowerUpManager.Instance != null && PowerUpManager.Instance.ShieldActive;
        if (squad != null && squad.Count > 0)
        {
            float reach = squad.Radius + 0.25f;
            Vector2 toSquad = (Vector2)(transform.position - squad.transform.position);
            if (toSquad.sqrMagnitude <= reach * reach)
            {
                if (!shielded) squad.RemoveFront(1);
                Sfx.Hurt();
                Despawn();
                return;
            }
        }

        if (target != null)
        {
            Vector2 dir = ((Vector2)(target.position - transform.position)).normalized;
            // Ralentización de la horda por power-up Slow.
            float speedMul = PowerUpManager.Instance != null ? PowerUpManager.Instance.HordeSpeedFactor : 1f;
            transform.position += (Vector3)(dir * moveSpeed * speedMul * Time.deltaTime);
        }

        // Patrón de jefe: invoca un par de mini-zombies cada ~4s para presionar.
        if (isBoss)
        {
            bossAttackT -= Time.deltaTime;
            if (bossAttackT <= 0f)
            {
                bossAttackT = 4f;
                for (int i = 0; i < 2; i++)
                {
                    float px = transform.position.x + Random.Range(-1.2f, 1.2f);
                    Enemy.Spawn(new Vector3(px, transform.position.y - 0.6f, 0f),
                        maxHealth * 0.12f, moveSpeed * 1.4f, 1,
                        new Color(0.50f, 0.69f, 0.31f), new Vector2(0.45f, 0.45f));
                }
                Sfx.BossRoar();
                CameraShake.Shake(0.12f, 0.18f);
            }
        }
    }

    /// <summary>Implementación de IShootable: las balas llaman aquí.</summary>
    public void TakeHit(float damage) => TakeDamage(damage);

    public void TakeDamage(float damage)
    {
        if (sr == null)
        {
            sr = GetComponent<SpriteRenderer>();
            if (sr != null) baseColor = sr.color;
        }

        Health -= damage;

        // Feedback de impacto: destello blanco, número de daño y chispa.
        flashT = 0.07f;
        if (sr != null) sr.color = Color.white;
        FloatingTextManager.Spawn(transform.position, Mathf.RoundToInt(damage).ToString(), new Color(1f, 0.95f, 0.5f));
        HitEffect.Burst(transform.position, new Color(1f, 0.9f, 0.4f), 4, 4f, 0.12f, 0.22f);

        if (Health <= 0f)
            Die();
    }

    void Die()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddKill();
            GameManager.Instance.AddCoins(coinValue); // monedas de la run → banco al acabar
        }

        // Gore verdoso en el tinte del zombie + micro hit-stop + sacudida + sonido.
        Vfx.Gore(transform.position, baseColor);
        Vfx.HitStop(isBoss ? 0.08f : 0.04f);
        CameraShake.Shake(isBoss ? 0.3f : 0.12f, isBoss ? 0.3f : 0.18f);
        if (isBoss) Haptics.Heavy(); else Haptics.Light();
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
