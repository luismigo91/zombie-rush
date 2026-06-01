using UnityEngine;

/// <summary>
/// Disparo automático: cada 1/cadencia segundos busca al enemigo más cercano
/// e instancia una bala dirigida hacia él. Va en el mismo GameObject que el
/// jugador.
///
/// Los valores (daño, cadencia) son públicos para poder tunearlos en el editor.
/// En la Fase 3 vendrán de WeaponData + UpgradeManager en lugar de estar aquí.
/// </summary>
public class AutoShooter : MonoBehaviour
{
    [Header("Arma (placeholder hasta WeaponData)")]
    public float damage = 10f;
    public float fireRate = 3f;       // disparos por segundo
    public float bulletSpeed = 12f;
    public float range = 100f;        // alcance enorme = toda la pantalla

    float cooldown;

    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
            return;

        cooldown -= Time.deltaTime;
        if (cooldown > 0f) return;

        Enemy target = FindNearest();
        if (target == null) return;   // sin objetivo no gastamos el disparo

        cooldown = 1f / Mathf.Max(0.01f, fireRate);
        Shoot(target);
    }

    /// <summary>Busca el enemigo vivo más cercano dentro del alcance.</summary>
    Enemy FindNearest()
    {
        Enemy best = null;
        float bestSqr = range * range;

        var list = Enemy.All;
        for (int i = 0; i < list.Count; i++)
        {
            Enemy e = list[i];
            if (e == null) continue;

            float sqr = ((Vector2)(e.transform.position - transform.position)).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = e;
            }
        }
        return best;
    }

    void Shoot(Enemy target)
    {
        Vector2 dir = ((Vector2)(target.transform.position - transform.position)).normalized;
        Bullet.Spawn(transform.position, dir, bulletSpeed, damage);
    }
}
