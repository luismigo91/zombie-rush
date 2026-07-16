using UnityEngine;

/// <summary>
/// Proyectil ENEMIGO (spitter y jefe escupidor): viaja en línea recta hacia donde
/// estaba el escuadrón al disparar → se esquiva moviéndose. NO es IShootable a
/// propósito: no se puede destruir a tiros; la respuesta es el movimiento (castiga
/// quedarse quieto, que con el fuego recto era la estrategia dominante). Con
/// escudo activo se deshace sin daño. Sin pool: hay pocos simultáneos.
/// </summary>
public class EnemyShot : MonoBehaviour
{
    static readonly Color Acid = new Color(0.55f, 0.95f, 0.30f);

    Vector2 dir;
    float speed;

    public static EnemyShot Spawn(Vector3 pos, Vector2 dir, float speed)
    {
        var go = Prims.MakeSprite("EnemyShot", ArtCache.Sprite("combat/bullet"), Acid,
            new Vector2(0.24f, 0.38f), pos, sortingOrder: 3);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        go.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        var s = go.AddComponent<EnemyShot>();
        s.dir = dir.normalized;
        s.speed = speed;
        return s;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;

        transform.position += (Vector3)(dir * speed * Time.deltaTime);

        // Impacto con el frente del escuadrón (misma comprobación por distancia
        // que el contacto de los zombies).
        Squad squad = gm.Squad;
        if (squad != null && squad.Count > 0)
        {
            float reach = squad.Radius + 0.2f;
            if (((Vector2)(transform.position - squad.transform.position)).sqrMagnitude <= reach * reach)
            {
                bool shielded = PowerUpManager.Instance != null && PowerUpManager.Instance.ShieldActive;
                if (!shielded)
                {
                    squad.RemoveFront(1);
                    Hud.FlashDamage();
                    Sfx.Hurt();
                }
                HitEffect.Burst(transform.position, Acid, 5, 5f, 0.12f, 0.2f);
                Destroy(gameObject);
                return;
            }
        }

        // Fuera de pantalla → fuera.
        Camera cam = Camera.main;
        float halfH = cam != null ? cam.orthographicSize : 6f;
        float halfW = cam != null ? halfH * cam.aspect : 4f;
        Vector3 p = transform.position;
        if (p.y < -halfH - 1f || p.y > halfH + 2f || Mathf.Abs(p.x) > halfW + 1f)
            Destroy(gameObject);
    }
}
