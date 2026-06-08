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
        GameObject go = Prims.Make("Cage", new Color(0.85f, 0.8f, 0.35f, 0.9f), new Vector2(0.8f, 0.8f), pos, sortingOrder: 1);

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
            gm.Squad.Add(survivors);
            FloatingTextManager.Spawn(transform.position, "+" + survivors, new Color(0.6f, 1f, 0.6f));
            Sfx.Gate();                              // ding ascendente de rescate
            Vfx.CoinPickup(transform.position);      // destello de recompensa
            Haptics.Medium();
        }
        Destroy(gameObject);
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
