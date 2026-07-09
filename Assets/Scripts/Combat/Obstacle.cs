using UnityEngine;

/// <summary>
/// Obstáculo físico EN LA CALZADA (coche calcinado, lápida, contenedor... según
/// el tema): baja con el scroll y, si el escuadrón lo toca, mata soldados del
/// frente (daño proporcional a su tamaño) y se hace pedazos. No se puede
/// disparar: es un peligro de esquiva pura, complementario a la Barrier
/// (que ocupa todo el ancho y sí se destruye a tiros).
/// </summary>
public class Obstacle : MonoBehaviour
{
    float fallSpeed;
    int damage;
    float radius;
    bool resolved;

    /// <summary>Crea un obstáculo con arte por clave de ArtCache (fallback: bloque gris).</summary>
    public static Obstacle Spawn(Vector3 pos, string spriteKey, float size, int damage, float fallSpeed)
    {
        var sprite = ArtCache.Sprite(spriteKey);
        GameObject go = sprite != null
            ? Prims.MakeSprite("Obstacle", sprite, Color.white, new Vector2(size, size), pos, sortingOrder: 1)
            : Prims.Make("Obstacle", new Color(0.55f, 0.5f, 0.5f, 0.95f), new Vector2(size, size * 0.6f), pos, sortingOrder: 1);

        var o = go.AddComponent<Obstacle>();
        o.fallSpeed = fallSpeed;
        o.damage = Mathf.Max(1, damage);
        o.radius = size * 0.45f; // radio de contacto algo menor que el sprite (perdona el roce)
        return o;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;

        float y = transform.position.y - fallSpeed * Time.deltaTime;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);

        // Contacto con el escuadrón por distancia (mismo patrón que Enemy).
        Squad squad = gm.Squad;
        if (!resolved && squad != null && squad.Count > 0)
        {
            float reach = squad.Radius + radius;
            Vector2 to = (Vector2)(transform.position - squad.transform.position);
            if (to.sqrMagnitude <= reach * reach)
            {
                resolved = true;
                squad.RemoveFront(damage);
                FloatingTextManager.Spawn(transform.position, "−" + damage, new Color(1f, 0.5f, 0.5f));
                HitEffect.Burst(transform.position, new Color(0.75f, 0.7f, 0.65f), 10, 6f, 0.16f, 0.35f);
                CameraShake.Shake(0.15f, 0.18f);
                Vfx.HitStop(0.04f);
                Sfx.Hurt();
                Haptics.Heavy();
                Destroy(gameObject);
                return;
            }
        }

        if (y < -(Camera.main != null ? Camera.main.orthographicSize : 6f) - 1.5f)
            Destroy(gameObject); // esquivado
    }
}
