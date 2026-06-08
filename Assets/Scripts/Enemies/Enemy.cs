using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enemigo (zombie). Avanza hacia el jugador; al tocarlo le hace daño de
/// contacto y desaparece (kamikaze). Muere al recibir daño de las balas y, al
/// morir por balas, suelta una moneda (no si te alcanza: ahí ya te dañó).
///
/// Sus stats vienen de un EnemyData (ScriptableObject), de modo que cada tipo
/// (normal, corredor, tanque) se define por datos y no por código.
///
/// Mantiene Enemy.All para que AutoShooter encuentre al más cercano sin búsquedas caras.
/// </summary>
public class Enemy : MonoBehaviour, IShootable
{
    public static readonly List<Enemy> All = new List<Enemy>();

    public float maxHealth = 30f;
    public float Health { get; private set; }
    public float moveSpeed = 2f;
    public float contactDamage = 15f;
    public int coinValue = 1;
    public bool isBoss;

    Transform target;
    SpriteRenderer sr;
    Color baseColor;
    float flashT; // temporizador del destello blanco al recibir daño

    /// <summary>Crea un enemigo a partir de su EnemyData.</summary>
    public static Enemy Spawn(EnemyData data, Vector3 pos)
        => Spawn(pos, data.health, data.moveSpeed, data.contactDamage, data.coinValue, data.color, data.size);

    /// <summary>Crea un enemigo con stats explícitos (usado también como fallback).</summary>
    public static Enemy Spawn(Vector3 pos, float health, float speed, float dmg, int coins, Color color, Vector2 size)
    {
        GameObject go = Prims.MakeSprite("Enemy", PixelArt.Zombie, color, size, pos, sortingOrder: 1);

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.useFullKinematicContacts = true;

        var e = go.AddComponent<Enemy>();
        e.Init(health, speed, dmg, coins);

        // Animación de arrastre (grises tintables → el color por tipo sigue tiñendo
        // todos los frames). El offset de fase evita que la horda marche sincronizada.
        SpriteAnim.Play(go, PixelArt.ZombieShamble, 4f, true);
        // Pop de escala al aparecer.
        Vfx.Pop(go.transform);
        return e;
    }

    /// <summary>Crea un mini-jefe: silueta propia masiva, con mucha vida y recompensa.</summary>
    public static Enemy SpawnBoss(Vector3 pos, float hp)
    {
        var e = Spawn(pos, hp, 1.3f, 40f, 30, new Color(0.45f, 0.70f, 0.20f), new Vector2(1.7f, 1.7f));
        e.isBoss = true;

        var srr = e.GetComponent<SpriteRenderer>();
        if (srr != null)
        {
            srr.sortingOrder = 2;
            // Sprite dedicado de jefe (color final propio): se pinta tal cual.
            srr.sprite = PixelArt.Boss;
            srr.color = Color.white;
        }
        // El jefe no usa la animación de arrastre de zombie: apagamos el SpriteAnim
        // que Spawn dejó activo para que no machaque el sprite de jefe cada frame.
        var anim = e.GetComponent<SpriteAnim>();
        if (anim != null) anim.enabled = false;

        // Entrada de jefe: rugido grave + sacudida fuerte.
        Sfx.BossRoar();
        CameraShake.Shake(0.35f, 0.4f);
        return e;
    }

    public void Init(float hp, float spd, float dmg, int coins)
    {
        maxHealth = hp;
        Health = hp;
        moveSpeed = spd;
        contactDamage = dmg;
        coinValue = coins;
    }

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
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
        Squad squad = gm.Squad;
        if (squad != null && squad.Count > 0)
        {
            float reach = squad.Radius + 0.25f;
            Vector2 toSquad = (Vector2)(transform.position - squad.transform.position);
            if (toSquad.sqrMagnitude <= reach * reach)
            {
                squad.RemoveFront(1);
                Sfx.Hurt();
                Destroy(gameObject);
                return;
            }
        }

        if (target != null)
        {
            Vector2 dir = ((Vector2)(target.position - transform.position)).normalized;
            transform.position += (Vector3)(dir * moveSpeed * Time.deltaTime);
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
            GameManager.Instance.AddCoins(isBoss ? 25 : 1); // monedas de la run → banco al acabar
        }

        // Gore verdoso en el tinte del zombie + micro hit-stop + sacudida + sonido.
        Vfx.Gore(transform.position, baseColor);
        Vfx.HitStop(isBoss ? 0.08f : 0.04f);
        CameraShake.Shake(isBoss ? 0.3f : 0.12f, isBoss ? 0.3f : 0.18f);
        if (isBoss) Haptics.Heavy(); else Haptics.Light();
        Sfx.Death();

        // (Las monedas/economía se reintroducen en la fase de meta-tienda.)
        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            player.TakeDamage(contactDamage);
            Destroy(gameObject); // alcanza al jugador y se consume (sin soltar moneda)
        }
    }
}
