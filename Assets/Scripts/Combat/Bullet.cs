using UnityEngine;

/// <summary>
/// Proyectil: se mueve en línea recta en la dirección con la que nace y aplica
/// daño al primer enemigo que toca, destruyéndose. Tiene vida máxima para que
/// las balas que no impactan no se acumulen.
///
/// Se crea por código con Bullet.Spawn (enfoque code-first de la fase gris).
/// Cuando haya muchas balas en pantalla, aquí entrará el object pooling (Fase 4).
/// </summary>
public class Bullet : MonoBehaviour
{
    Vector2 dir;
    float speed;
    float damage;
    float life = 1.4f; // segundos antes de autodestruirse (acota balas sin pooling)

    /// <summary>Crea una bala en pos, viajando hacia dir, con la velocidad y daño dados.</summary>
    public static Bullet Spawn(Vector3 pos, Vector2 dir, float speed, float damage)
    {
        GameObject go = Prims.MakeSprite("Bullet", PixelArt.Bullet, Color.white, new Vector2(0.28f, 0.45f), pos, sortingOrder: 5);

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        // Necesario para que un cuerpo cinemático detecte triggers contra otros
        // cinemáticos/estáticos (enemigos). Sin esto no salta OnTriggerEnter2D.
        rb.useFullKinematicContacts = true;

        var b = go.AddComponent<Bullet>();
        b.dir = dir;
        b.speed = speed;
        b.damage = damage;

        // Orienta el rectángulo de la bala en la dirección de avance (estético).
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        go.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        return b;
    }

    void Update()
    {
        transform.position += (Vector3)(dir * speed * Time.deltaTime);

        life -= Time.deltaTime;
        if (life <= 0f)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Daña a cualquier "disparable": zombie, jaula o barrera.
        var hit = other.GetComponent<IShootable>();
        if (hit != null)
        {
            hit.TakeHit(damage);
            Sfx.Hit();
            Destroy(gameObject);
        }
    }
}
