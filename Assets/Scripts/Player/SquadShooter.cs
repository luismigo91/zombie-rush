using UnityEngine;

/// <summary>
/// Disparo del escuadrón (Zombie Rush): RECTO hacia arriba, sin auto-apuntado.
/// Va en el mismo GameObject que Squad.
///
/// No dispara una bala por soldado: emite fuego por "streams" repartidos a lo
/// ancho del blob, y el daño de cada stream escala con la DENSIDAD (soldados por
/// stream). El arma global (Weapons, por tiers que sube con los gates de arma)
/// modula daño, cadencia y nº de streams extra.
///   - mientras el blob ensancha (√N) → más streams = más cobertura.
///   - pasado el tope de ancho → mismo nº de streams pero más daño = densidad.
/// </summary>
[RequireComponent(typeof(Squad))]
public class SquadShooter : MonoBehaviour
{
    [Header("Arma base (la modula el tier de Weapons)")]
    public float baseDamage = 7f;     // daño por soldado (tier 0)
    public float fireRate = 3.5f;     // ráfagas/seg (tier 0)
    public float bulletSpeed = 16f;
    public float streamSpacing = 0.42f;
    public int maxStreams = 11;

    Squad squad;
    float cooldown;

    void Awake() => squad = GetComponent<Squad>();

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;

        Weapons.Tier tier = Weapons.Get(gm.WeaponTier);

        cooldown -= Time.deltaTime;
        if (cooldown > 0f) return;

        Fire(tier);
        cooldown = 1f / Mathf.Max(0.01f, fireRate * tier.fireRateMult);
    }

    void Fire(Weapons.Tier tier)
    {
        int n = squad.Count;
        if (n <= 0) return;

        float width = squad.Width;
        int streams = Mathf.Clamp(Mathf.RoundToInt(width / streamSpacing) + tier.extraStreams, 1, maxStreams);
        streams = Mathf.Min(streams, n);

        float damagePerStream = baseDamage * tier.damageMult * n / streams;
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
