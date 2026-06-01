using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enemigo (zombie). Avanza hacia el jugador; al tocarlo le hace daño de
/// contacto y desaparece (kamikaze, simplificación de la fase gris). Muere al
/// recibir daño suficiente de las balas.
///
/// Mantiene una lista estática Enemy.All para que AutoShooter encuentre al más
/// cercano sin usar búsquedas caras en cada disparo.
///
/// Fase 2: los tipos (normal, corredor, tanque) y sus stats vendrán de un
/// ScriptableObject (EnemyData) en lugar de pasarse por parámetros aquí.
/// </summary>
public class Enemy : MonoBehaviour
{
    /// <summary>Registro de todos los enemigos vivos.</summary>
    public static readonly List<Enemy> All = new List<Enemy>();

    public float maxHealth = 30f;
    public float Health { get; private set; }
    public float moveSpeed = 2f;
    public float contactDamage = 15f;

    Transform target;

    /// <summary>Crea un enemigo por código con los stats indicados.</summary>
    public static Enemy Spawn(Vector3 pos, float health, float speed, float dmg, Color color, Vector2 size)
    {
        GameObject go = Prims.Make("Enemy", color, size, pos, sortingOrder: 1);

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.useFullKinematicContacts = true;

        var e = go.AddComponent<Enemy>();
        e.Init(health, speed, dmg);
        return e;
    }

    /// <summary>Inicializa stats. Se llama tras AddComponent (no en Awake) para
    /// que los valores ya estén asignados cuando se fija la vida.</summary>
    public void Init(float hp, float spd, float dmg)
    {
        maxHealth = hp;
        Health = hp;
        moveSpeed = spd;
        contactDamage = dmg;
    }

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.Player != null)
            target = GameManager.Instance.Player.transform;
    }

    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
            return;

        // El jugador puede no existir aún en el primer frame; reintenta.
        if (target == null && GameManager.Instance.Player != null)
            target = GameManager.Instance.Player.transform;

        if (target != null)
        {
            Vector2 dir = ((Vector2)(target.position - transform.position)).normalized;
            transform.position += (Vector3)(dir * moveSpeed * Time.deltaTime);
        }
    }

    /// <summary>Recibe daño de una bala. Al llegar a 0 muere y suma al marcador.</summary>
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
        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            player.TakeDamage(contactDamage);
            Destroy(gameObject); // el zombie "alcanza" al jugador y se consume
        }
    }
}
