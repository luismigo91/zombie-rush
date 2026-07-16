using UnityEngine;

/// <summary>
/// Tipos de power-up in-run (Zombie Rush). Sueltan los zombies al morir (baja
/// probabilidad; el jefe siempre suelta uno) y, al recogerlos con el escuadrón,
/// aplican un efecto: bomba (daño en área), ralentización de la horda, escudo
/// temporal (inmunidad al contacto) o cadencia disparada.
/// </summary>
public enum PowerUpType { Bomb, Slow, Shield, Rapid }

/// <summary>
/// Gestiona los efectos temporales de los power-ups: escudo, cadencia y
/// ralentización. Lo crea GameBootstrap. Las mecánicas lo consultan:
/// - SquadShooter: si RapidActive, multiplica la cadencia.
/// - Enemy: si SlowActive, reduce la velocidad de la horda.
/// - Enemy contacto: si ShieldActive, el zombie muere sin bajar soldados.
/// La bomba es instantánea (daño en área a todos los enemigos en pantalla).
/// </summary>
public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance { get; private set; }

    [Header("Duraciones (segundos)")]
    public float shieldDuration = 6f;
    public float rapidDuration = 6f;
    public float slowDuration = 5f;

    [Header("Bomba")]
    public float bombDamage = 60f;
    public float bombRadius = 12f;

    [Header("Cadena de eventos")]
    public float dropChance = 0.045f; // probabilidad por zombie: escaso a propósito (supervivencia)
    public int pityKills = 40;        // drop GARANTIZADO tras tantas muertes sin premio (anti-sequía RNG)

    int killsSinceDrop;

    // Tiempo restante de cada efecto temporal (>0 → activo).
    float shieldT, rapidT, slowT, freezeT;

    public bool ShieldActive => shieldT > 0f;
    public bool RapidActive => rapidT > 0f;
    public bool SlowActive => slowT > 0f;
    public bool FreezeActive => freezeT > 0f;
    /// <summary>Fracción de tiempo restante de cada efecto (0..1) para el HUD.</summary>
    public float ShieldFrac => shieldDuration > 0f ? Mathf.Clamp01(shieldT / shieldDuration) : 0f;
    public float RapidFrac => rapidDuration > 0f ? Mathf.Clamp01(rapidT / rapidDuration) : 0f;
    public float SlowFrac => slowDuration > 0f ? Mathf.Clamp01(slowT / slowDuration) : 0f;
    public float FreezeFrac => Mathf.Clamp01(freezeT / 4f);
    /// <summary>Factor de velocidad de la horda: congelación (habilidad) &gt; slow (power-up).</summary>
    public float HordeSpeedFactor => FreezeActive ? 0.22f : (SlowActive ? 0.45f : 1f);
    /// <summary>Multiplicador de cadencia del escuadrón (2× si rapid activo).</summary>
    public float FireRateFactor => RapidActive ? 2.2f : 1f;

    void Awake() => Instance = this;

    /// <summary>
    /// Tirada de drop por muerte de zombie: probabilidad (modulada por el perk de
    /// suerte) + "pity" (garantía tras una racha seca). Centralizado aquí para que
    /// el contador de racha viva junto a la probabilidad.
    /// </summary>
    public bool RollDrop(bool force)
    {
        // Desafío diario SIN POWER-UPS: nada cae, ni siquiera del jefe (reto puro).
        if (RunConfig.DailyModActive(DailyMod.NoPowerUps)) return false;

        killsSinceDrop++;
        float chance = dropChance * Perks.LuckMult;
        if (force || killsSinceDrop >= pityKills || Random.value <= chance)
        {
            killsSinceDrop = 0;
            return true;
        }
        return false;
    }

    /// <summary>Concede escudo directo (perk de blindaje inicial, revive). No acorta uno activo.</summary>
    public void GrantShield(float seconds) => shieldT = Mathf.Max(shieldT, seconds);

    /// <summary>Congelación de la habilidad CONGELACIÓN (más fuerte que el slow).</summary>
    public void GrantFreeze(float seconds) => freezeT = Mathf.Max(freezeT, seconds);

    void Update()
    {
        float dt = Time.deltaTime;
        if (shieldT > 0f) shieldT -= dt;
        if (rapidT > 0f) rapidT -= dt;
        if (slowT > 0f) slowT -= dt;
        if (freezeT > 0f) freezeT -= dt;
    }

    /// <summary>Aplica el efecto del power-up recogido.</summary>
    public void Apply(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.Bomb:   DetonateBomb(); break;
            case PowerUpType.Slow:    slowT = slowDuration; break;
            case PowerUpType.Shield:  shieldT = shieldDuration; break;
            case PowerUpType.Rapid:   rapidT = rapidDuration; break;
        }
        Sfx.LevelUp(); // chime de recogida (reutiliza el de subida de nivel)
        Haptics.Medium();
    }

    /// <summary>Bomba: daña a todos los enemigos dentro del radio del escuadrón.</summary>
    void DetonateBomb()
    {
        Vector3 center = GameManager.Instance != null && GameManager.Instance.Squad != null
            ? GameManager.Instance.Squad.transform.position : Vector3.zero;

        // Iteramos sobre una copia de índices: la bomba puede matar a muchos a la vez.
        var all = Enemy.All;
        for (int i = all.Count - 1; i >= 0; i--)
        {
            var e = all[i];
            if (e == null) continue;
            float d = ((Vector2)(e.transform.position - center)).magnitude;
            if (d <= bombRadius)
            {
                // Daño directo (mata a casi todo lo estándar; erosiona al jefe).
                e.TakeDamage(bombDamage * (1f - d / bombRadius * 0.5f));
            }
        }

        // Flash + sacudida vistosos.
        Vfx.Confetti(center); // reutiliza el burst de partículas como onda expansiva
        CameraShake.Shake(0.3f, 0.35f);
        Vfx.HitStop(0.06f);
    }
}

/// <summary>
/// Pickup de power-up: sprite que cae con el scroll, rota/bobea y, al tocar el
/// escuadrón, aplica su efecto. Si se sale de pantalla, se autodestruye.
/// </summary>
public class PowerUp : MonoBehaviour
{
    static readonly Color[] TypeColors =
    {
        new Color(1f, 0.35f, 0.24f),   // Bomb  #FF5A3C
        new Color(0.24f, 0.84f, 0.96f),// Slow  #3DD6F5
        new Color(0.36f, 0.56f, 1f),   // Shield #5B8DFF
        new Color(1f, 0.82f, 0.23f),   // Rapid #FFD23A
    };

    PowerUpType type;
    float scrollSpeed;
    float bob;
    SpriteRenderer sr;

    public static PowerUp Spawn(Vector3 pos, PowerUpType type, float scrollSpeed)
    {
        Color c = TypeColors[(int)type];
        var go = Prims.MakeSprite("PowerUp", ArtCache.Sprite("items/coin"), c, new Vector2(0.5f, 0.5f), pos, sortingOrder: 4);
        var p = go.AddComponent<PowerUp>();
        p.type = type;
        p.scrollSpeed = scrollSpeed;
        p.sr = go.GetComponent<SpriteRenderer>();
        // Giro de moneda (frames de items/coin_spin) manteniendo el tinte por tipo.
        var spin = ArtCache.Sprites("items/coin_spin");
        if (spin != null && spin.Length > 1) SpriteAnim.Play(go, spin, 10f, true);
        Vfx.Pop(go.transform);
        return p;
    }

    /// <summary>Suelta un power-up aleatorio en pos según la tirada del manager (con pity).</summary>
    public static void MaybeDrop(Vector3 pos, float scrollSpeed, bool force = false)
    {
        var mgr = PowerUpManager.Instance;
        if (mgr == null) return;
        if (!mgr.RollDrop(force)) return;
        PowerUpType t = (PowerUpType)Random.Range(0, 4);
        Spawn(pos, t, scrollSpeed);
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        // Cae con el scroll + leve bob vertical.
        bob += Time.deltaTime * 6f;
        float dy = Mathf.Sin(bob) * 0.15f;
        transform.position += Vector3.down * (scrollSpeed * 0.6f + 1.5f) * Time.deltaTime
                              + Vector3.up * dy * Time.deltaTime;
        transform.Rotate(0f, 0f, 90f * Time.deltaTime);

        // Recogida: solape con el escuadrón (el perk IMÁN amplía el alcance).
        if (gm.State == GameState.Playing && gm.Squad != null && gm.Squad.Count > 0)
        {
            float reach = (gm.Squad.Radius + 0.4f) * Perks.MagnetMult;
            if (((Vector2)(transform.position - gm.Squad.transform.position)).sqrMagnitude <= reach * reach)
            {
                if (PowerUpManager.Instance != null) PowerUpManager.Instance.Apply(type);
                Vfx.CoinPickup(transform.position);
                Destroy(gameObject);
                return;
            }
        }

        // Fuera de pantalla por abajo → se autodestruye.
        if (transform.position.y < Camera.main.transform.position.y - Camera.main.orthographicSize - 2f)
            Destroy(gameObject);
    }
}
