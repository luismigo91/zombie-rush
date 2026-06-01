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
public class Enemy : MonoBehaviour
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
        return e;
    }

    /// <summary>Crea un mini-jefe: un zombie enorme, con mucha vida y recompensa.</summary>
    public static Enemy SpawnBoss(Vector3 pos, float hp)
    {
        var e = Spawn(pos, hp, 1.3f, 40f, 30, new Color(0.45f, 0.70f, 0.20f), new Vector2(1.7f, 1.7f));
        e.isBoss = true;
        var srr = e.GetComponent<SpriteRenderer>();
        if (srr != null) srr.sortingOrder = 2;
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
        if (gm != null && gm.Player != null)
            target = gm.Player.transform;
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

        if (target != null)
        {
            Vector2 dir = ((Vector2)(target.position - transform.position)).normalized;
            transform.position += (Vector3)(dir * moveSpeed * Time.deltaTime);
        }
    }

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
            GameManager.Instance.AddKill();

        // Estallido en el color del enemigo + sacudida de cámara + sonido.
        HitEffect.Burst(transform.position, baseColor, 10, 6f, 0.16f, 0.35f);
        CameraShake.Shake(0.12f, 0.18f);
        Sfx.Death();

        Pickup.SpawnCoin(transform.position, coinValue);
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
