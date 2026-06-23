using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Proyectil del escuadrón: viaja recto y daña al primer "disparable" (zombie,
/// jaula o barrera) que toca. Tiene vida máxima para que las balas que no impactan
/// no se acumulen.
///
/// Usa un POOL estático (se reactiva en vez de instanciar/destruir) porque el
/// escuadrón dispara muchas balas. El pool tolera la recarga de escena: las balas
/// destruidas por Unity quedan como referencias nulas y se descartan al sacarlas.
/// </summary>
public class Bullet : MonoBehaviour
{
    static readonly Stack<Bullet> pool = new Stack<Bullet>();

    Vector2 dir;
    float speed;
    float damage;
    float life;
    int pierce; // cuántos enemigos extra puede atravesar (0 = se detiene al primer impacto)

    /// <summary>Saca una bala del pool (o crea una) y la lanza desde pos hacia dir.</summary>
    public static Bullet Spawn(Vector3 pos, Vector2 dir, float speed, float damage, int pierce = 0)
    {
        Bullet b = null;
        while (pool.Count > 0)
        {
            b = pool.Pop();
            if (b != null) break; // descarta nulos (destruidos por recarga de escena)
            b = null;
        }

        if (b == null)
        {
            GameObject go = Prims.MakeSprite("Bullet", PixelArt.Bullet, Color.white,
                new Vector2(0.28f, 0.45f), pos, sortingOrder: 5);

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.useFullKinematicContacts = true;

            b = go.AddComponent<Bullet>();
        }

        b.dir = dir;
        b.speed = speed;
        b.damage = damage;
        b.life = 1.4f;
        b.pierce = pierce;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        b.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, 0f, angle));
        if (!b.gameObject.activeSelf) b.gameObject.SetActive(true);

        return b;
    }

    void Despawn()
    {
        gameObject.SetActive(false);
        pool.Push(this);
    }

    void Update()
    {
        transform.position += (Vector3)(dir * speed * Time.deltaTime);

        life -= Time.deltaTime;
        if (life <= 0f) Despawn();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var hit = other.GetComponent<IShootable>();
        if (hit != null)
        {
            hit.TakeHit(damage);
            Vfx.BulletImpact(transform.position); // chispa de impacto
            Sfx.Hit();
            // Si le queda pierce, atraviesa y sigue; si no, se consume.
            if (pierce > 0) pierce--;
            else Despawn();
        }
    }
}
