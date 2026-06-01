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

    Transform target;

    /// <summary>Crea un enemigo a partir de su EnemyData.</summary>
    public static Enemy Spawn(EnemyData data, Vector3 pos)
        => Spawn(pos, data.health, data.moveSpeed, data.contactDamage, data.coinValue, data.color, data.size);

    /// <summary>Crea un enemigo con stats explícitos (usado también como fallback).</summary>
    public static Enemy Spawn(Vector3 pos, float health, float speed, float dmg, int coins, Color color, Vector2 size)
    {
        GameObject go = Prims.Make("Enemy", color, size, pos, sortingOrder: 1);

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

    void Start() => TryAcquireTarget();

    void TryAcquireTarget()
    {
        var gm = GameManager.Instance;
        if (gm != null && gm.Player != null)
            target = gm.Player.transform;
    }

    void Update()
    {
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
        Health -= damage;
        if (Health <= 0f)
            Die();
    }

    void Die()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.AddKill();

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
