using UnityEngine;

/// <summary>
/// Barrera destructible: un muro con vida que baja con el scroll y bloquea el
/// paso. Hay que derribarla a tiros; si llega a la altura del escuadrón todavía
/// con vida, arrasa parte de la fila (penalización proporcional a lo que quedaba)
/// y se rompe. Da ritmo: obliga a concentrar fuego.
/// </summary>
public class Barrier : MonoBehaviour, IShootable
{
    float health;
    float maxHealth;
    float fallSpeed;
    int penaltyPerHealth = 1; // soldados perdidos por cada "punto de dureza" restante (escalado)
    bool resolved;

    public static Barrier Spawn(Vector3 pos, float health, float width, float fallSpeed)
    {
        GameObject go = Prims.Make("Barrier", new Color(0.55f, 0.55f, 0.6f, 0.95f), new Vector2(width, 0.5f), pos, sortingOrder: 1);

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.useFullKinematicContacts = true;

        var b = go.AddComponent<Barrier>();
        b.health = health;
        b.maxHealth = health;
        b.fallSpeed = fallSpeed;
        return b;
    }

    public void TakeHit(float damage)
    {
        if (resolved) return;
        health -= damage;
        HitEffect.Burst(transform.position, new Color(0.8f, 0.8f, 0.85f), 3, 4f, 0.1f, 0.18f);

        // Se va "vaciando" visualmente con la vida restante.
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color c = sr.color;
            c.a = 0.35f + 0.6f * Mathf.Clamp01(health / maxHealth);
            sr.color = c;
        }

        if (health <= 0f)
        {
            resolved = true;
            HitEffect.Burst(transform.position, new Color(0.8f, 0.8f, 0.85f), 8, 6f, 0.15f, 0.3f);
            Destroy(gameObject);
        }
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;

        float y = transform.position.y - fallSpeed * Time.deltaTime;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);

        Squad squad = gm.Squad;
        if (!resolved && squad != null && y <= squad.transform.position.y)
        {
            resolved = true;
            int penalty = Mathf.CeilToInt(health / Mathf.Max(1f, maxHealth) * 8f) * penaltyPerHealth;
            squad.RemoveFront(penalty); // la barrera intacta arrasa parte del frente
            CameraShake.Shake(0.18f, 0.2f);
            Destroy(gameObject);
        }
    }
}
