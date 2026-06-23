using UnityEngine;

/// <summary>
/// Disparo del escuadrón (Zombie Rush): RECTO hacia arriba, sin auto-apuntado.
/// Va en el mismo GameObject que Squad.
///
/// El fuego se concentra en el centro del blob y se reparte en "streams"
/// (columnas). Cada columna dispara con su PROPIO temporizador desfasado y con
/// jitter, de modo que las columnas se descoordinan entre sí → fuego continuo
/// (no volleys en fila). Un enemigo grande recibe impactos de varias columnas en
/// instantes distintos. El daño por stream escala con la densidad (soldados/stream)
/// y el arma (Weapons) modula daño, cadencia y nº de streams.
/// </summary>
[RequireComponent(typeof(Squad))]
public class SquadShooter : MonoBehaviour
{
    [Header("Arma base (la modula el tier de Weapons)")]
    public float baseDamage = 7f;     // daño por soldado (tier 0)
    public float fireRate = 3.5f;     // ráfagas/seg por columna (tier 0)
    public float bulletSpeed = 16f;
    public float streamSpacing = 0.42f;
    public int maxStreams = 11;
    public float fireConcentration = 0.6f; // fracción del ancho del blob donde se concentra el fuego

    Squad squad;
    float[] timers;   // temporizador independiente por columna
    float sfxCd;

    void Awake()
    {
        squad = GetComponent<Squad>();
        timers = new float[Mathf.Max(1, maxStreams)];
        // Fases iniciales aleatorias → las columnas arrancan descoordinadas.
        for (int k = 0; k < timers.Length; k++)
            timers[k] = Random.Range(0f, 1f / Mathf.Max(0.01f, fireRate));
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;

        int n = squad.Count;
        if (n <= 0) return;

        Weapons.Tier tier = Weapons.Get(gm.WeaponTier);

        // Cadencia amplificada por el power-up Rapid (si está activo).
        float rapidFactor = PowerUpManager.Instance != null ? PowerUpManager.Instance.FireRateFactor : 1f;

        float fireWidth = squad.Width * fireConcentration;
        int streams = Mathf.Clamp(Mathf.RoundToInt(fireWidth / streamSpacing) + tier.extraStreams, 1, maxStreams);
        streams = Mathf.Min(streams, n);

        float interval = 1f / Mathf.Max(0.01f, fireRate * tier.fireRateMult * rapidFactor);
        float damagePerStream = baseDamage * tier.damageMult * n / streams;
        float cx = transform.position.x;
        float y = squad.TopY;

        bool firedAny = false;

        // Se avanzan TODOS los temporizadores (mantiene las fases), pero solo
        // disparan las columnas activas (k < streams).
        for (int k = 0; k < timers.Length; k++)
        {
            timers[k] -= Time.deltaTime;
            if (timers[k] > 0f) continue;

            if (k < streams)
            {
                float fx = streams == 1
                    ? 0f
                    : Mathf.Lerp(-fireWidth * 0.5f, fireWidth * 0.5f, k / (streams - 1f));
                var boca = new Vector3(cx + fx, y, 0f);
                Bullet.Spawn(boca, Vector2.up, bulletSpeed, damagePerStream, tier.pierce);
                Vfx.Muzzle(boca); // fogonazo de boca en cada columna
                firedAny = true;
            }

            timers[k] = interval * Random.Range(0.8f, 1.2f); // jitter → descoordinación continua
        }

        // Sonido + animación de disparo + haptic, limitados (si no, sería un zumbido continuo).
        sfxCd -= Time.deltaTime;
        if (firedAny && sfxCd <= 0f)
        {
            Sfx.Shoot();
            squad.PlayShootAnim(3); // retroceso/fogonazo en unas pocas unidades
            Haptics.Light();        // micro-vibración al disparar (respeta ajustes)
            sfxCd = 0.07f;
        }
    }
}
