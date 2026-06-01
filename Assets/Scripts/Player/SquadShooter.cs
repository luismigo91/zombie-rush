using UnityEngine;

/// <summary>
/// Disparo del escuadrón (Zombie Rush): RECTO hacia arriba, sin auto-apuntado.
/// Va en el mismo GameObject que Squad.
///
/// No dispara una bala por soldado (sería redundante y caro): emite fuego por
/// "streams" repartidos a lo ancho del blob, y el daño de cada stream escala con
/// la DENSIDAD (cuántos soldados hay por stream). Así:
///   - mientras el blob ensancha (√N) → más streams = más cobertura.
///   - pasado el tope de ancho → mismo nº de streams pero más daño = densidad.
/// El DPS total ≈ baseDamage · N · fireRate, crezca como crezca el escuadrón.
/// </summary>
[RequireComponent(typeof(Squad))]
public class SquadShooter : MonoBehaviour
{
    [Header("Arma base (placeholder; luego tiers en la meta-tienda)")]
    public float baseDamage = 7f;     // daño por soldado
    public float fireRate = 3.5f;     // ráfagas por segundo
    public float bulletSpeed = 16f;
    public float streamSpacing = 0.42f; // separación entre streams (ancho/columna)
    public int maxStreams = 11;

    Squad squad;
    float cooldown;

    void Awake() => squad = GetComponent<Squad>();

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;

        cooldown -= Time.deltaTime;
        if (cooldown > 0f) return;

        Fire();
        cooldown = 1f / Mathf.Max(0.01f, fireRate);
    }

    void Fire()
    {
        int n = squad.Count;
        if (n <= 0) return;

        float width = squad.Width;
        int streams = Mathf.Clamp(Mathf.RoundToInt(width / streamSpacing), 1, maxStreams);
        streams = Mathf.Min(streams, n);

        float damagePerStream = baseDamage * n / streams; // densidad: el daño total se reparte
        float cx = transform.position.x;
        float y = squad.TopY;

        for (int k = 0; k < streams; k++)
        {
            float fx = streams == 1
                ? 0f
                : Mathf.Lerp(-width * 0.5f, width * 0.5f, k / (streams - 1f));
            Bullet.Spawn(new Vector3(cx + fx, y, 0f), Vector2.up, bulletSpeed, damagePerStream);
        }

        Sfx.Shoot();
    }
}
