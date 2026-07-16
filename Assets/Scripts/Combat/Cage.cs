using UnityEngine;

/// <summary>
/// Jaula de supervivientes: baja con el scroll y, al romperla a tiros, suma sus
/// supervivientes al escuadrón. Si llega a la altura del escuadrón sin liberarse,
/// se pierde (no se obtienen). Es un eje de crecimiento por "rescate".
/// </summary>
public class Cage : MonoBehaviour, IShootable
{
    float health;
    int survivors;
    float fallSpeed;
    bool freed;

    public static Cage Spawn(Vector3 pos, int survivors, float health, float fallSpeed)
    {
        // Jaula con arte (1×1u normalizado); fallback a la caja amarilla plana.
        var sprite = ArtCache.Sprite("combat/cage");
        GameObject go = sprite != null
            ? Prims.MakeSprite("Cage", sprite, Color.white, new Vector2(0.9f, 0.9f), pos, sortingOrder: 1)
            : Prims.Make("Cage", new Color(0.85f, 0.8f, 0.35f, 0.9f), new Vector2(0.8f, 0.8f), pos, sortingOrder: 1);

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.useFullKinematicContacts = true;

        var c = go.AddComponent<Cage>();
        c.survivors = survivors;
        c.health = health;
        c.fallSpeed = fallSpeed;
        return c;
    }

    public void TakeHit(float damage)
    {
        if (freed) return;
        health -= damage;
        HitEffect.Burst(transform.position, new Color(1f, 0.95f, 0.5f), 3, 4f, 0.1f, 0.18f);
        if (health <= 0f) Free();
    }

    void Free()
    {
        freed = true;
        var gm = GameManager.Instance;
        if (gm != null && gm.Squad != null)
        {
            // Lo que no cabe en el escuadrón (cap 30) se convierte en monedas ×2,
            // como en los gates: rescatar nunca se desperdicia.
            int accepted = gm.Squad.Add(survivors);
            int excessCoins = (survivors - accepted) * 2;
            if (excessCoins > 0) gm.AddCoins(excessCoins);
            string label = accepted > 0
                ? (excessCoins > 0 ? $"+{accepted} · +{excessCoins} monedas" : "+" + accepted)
                : $"+{excessCoins} monedas";
            FloatingTextManager.Spawn(transform.position, label,
                excessCoins > 0 ? new Color(1f, 0.82f, 0.23f) : new Color(0.6f, 1f, 0.6f));
            Sfx.Gate();                              // ding ascendente de rescate
            Vfx.CoinPickup(transform.position);      // destello de recompensa
            Haptics.Medium();
        }

        // Si hay arte de jaula rota, la deja un instante abierta antes de desaparecer.
        var broken = ArtCache.Sprite("combat/cage_broken");
        var sr = GetComponent<SpriteRenderer>();
        if (broken != null && sr != null)
        {
            sr.sprite = broken;
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
            Destroy(gameObject, 0.5f); // sigue cayendo medio segundo, ya rota
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;

        float y = transform.position.y - fallSpeed * Time.deltaTime;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);

        if (y < -(Camera.main != null ? Camera.main.orthographicSize : 6f) - 1f)
            Destroy(gameObject); // se pierde sin liberar
    }
}
