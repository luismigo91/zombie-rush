using System.Collections.Generic;
using UnityEngine;

/// <summary>Tipos de perk de campaña (mejoras permanentes elegidas al ganar niveles).</summary>
public enum PerkType { Damage, FireRate, Pierce, Magnet, StartShield, Reinforce, Lucky }

/// <summary>
/// Perks de campaña (Zombie Rush): al completar un nivel se elige 1 de 3 mejoras
/// PERMANENTES. Son la contrapartida del escalado de amenaza por nivel: sin ellos
/// el techo del jugador (tope de soldados + tier de arma) se alcanza hacia el
/// nivel 10-15 y la curva de dificultad lo deja atrás (se volvía imposible).
///
/// Persistidos con PlayerPrefs ("perk_&lt;Tipo&gt;"). Las 3 opciones de cada nivel
/// salen de un RNG sembrado con el nivel → deterministas (mismas cartas si
/// repites la victoria). Cada perk tiene un tope para mantener el balance acotado.
/// </summary>
public static class Perks
{
    static readonly PerkType[] AllTypes =
    {
        PerkType.Damage, PerkType.FireRate, PerkType.Pierce, PerkType.Magnet,
        PerkType.StartShield, PerkType.Reinforce, PerkType.Lucky,
    };

    static string Key(PerkType p) => "perk_" + p;

    public static int Level(PerkType p) => Mathf.Clamp(PlayerPrefs.GetInt(Key(p), 0), 0, Cap(p));

    /// <summary>Tope de niveles por perk (49 en total, ~mitad de la campaña; los de
    /// daño/cadencia son largos a propósito: son el recorrido del late game).</summary>
    public static int Cap(PerkType p) => p switch
    {
        PerkType.Damage => 18,
        PerkType.FireRate => 12,
        PerkType.Pierce => 3,
        PerkType.Magnet => 4,
        PerkType.StartShield => 4,
        PerkType.Reinforce => 4, // con el cap de escuadrón en 30, pocos y valiosos
        _ => 4, // Lucky
    };

    public static bool IsMaxed(PerkType p) => Level(p) >= Cap(p);

    // ---- Efectos (los consultan SquadShooter/Squad/PowerUpManager/PowerUp) ----

    /// <summary>Multiplicador de daño del escuadrón (+12 % por nivel: con el cap de
    /// 30 soldados las MEJORAS son el eje de poder — rediseño de playtest).</summary>
    public static float DamageMult => 1f + 0.12f * Level(PerkType.Damage);

    /// <summary>Multiplicador de cadencia (+8 % por nivel).</summary>
    public static float FireRateMult => 1f + 0.08f * Level(PerkType.FireRate);

    /// <summary>Enemigos extra que atraviesa cada bala (se suma al pierce del arma).</summary>
    public static int BonusPierce => Level(PerkType.Pierce);

    /// <summary>Multiplicador del radio de recogida de power-ups (+30 % por nivel).</summary>
    public static float MagnetMult => 1f + 0.30f * Level(PerkType.Magnet);

    /// <summary>Segundos de escudo con los que empieza cada nivel (2.5 s por nivel).</summary>
    public static float StartShieldSeconds => 2.5f * Level(PerkType.StartShield);

    /// <summary>Soldados iniciales extra (+2 por nivel; se suman al punto de partida).</summary>
    public static int BonusStartUnits => 2 * Level(PerkType.Reinforce);

    /// <summary>Multiplicador de la probabilidad de drop de power-up (+33 % por nivel).</summary>
    public static float LuckMult => 1f + 0.33f * Level(PerkType.Lucky);

    // ---- Textos para las cartas de la pantalla de victoria ----

    public static string Name(PerkType p) => p switch
    {
        PerkType.Damage => "POTENCIA",
        PerkType.FireRate => "CADENCIA",
        PerkType.Pierce => "PERFORANTE",
        PerkType.Magnet => "IMÁN",
        PerkType.StartShield => "BLINDAJE",
        PerkType.Reinforce => "REFUERZOS",
        _ => "SUERTE",
    };

    /// <summary>Qué gana el jugador si coge el SIGUIENTE nivel del perk.</summary>
    public static string Description(PerkType p) => p switch
    {
        PerkType.Damage => "+12% de daño por disparo",
        PerkType.FireRate => "+8% de cadencia de fuego",
        PerkType.Pierce => "Las balas atraviesan +1 enemigo",
        PerkType.Magnet => "+30% de radio para recoger power-ups",
        PerkType.StartShield => "+2.5s de escudo al empezar cada nivel",
        PerkType.Reinforce => "+2 soldados iniciales",
        _ => "+33% de probabilidad de power-up",
    };

    /// <summary>Sube un nivel el perk (lo llama la carta elegida en la victoria).</summary>
    public static void Grant(PerkType p)
    {
        if (IsMaxed(p)) return;
        PlayerPrefs.SetInt(Key(p), Level(p) + 1);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Las 3 opciones (o menos, si casi todo está al tope) para la victoria del
    /// nivel dado. Determinista: sembrado con el nivel, como el generador.
    /// </summary>
    public static PerkType[] RollChoices(int levelIndex)
    {
        var pool = new List<PerkType>();
        foreach (var p in AllTypes)
            if (!IsMaxed(p)) pool.Add(p);

        var rng = new System.Random(LevelGenerator.GlobalSeed ^ (levelIndex * 7919 + 17));
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        int take = Mathf.Min(3, pool.Count);
        return pool.GetRange(0, take).ToArray();
    }

    /// <summary>Borra todos los perks (lo usa el reinicio de progreso del menú).</summary>
    public static void ResetAll()
    {
        foreach (var p in AllTypes) PlayerPrefs.DeleteKey(Key(p));
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Piso de perks al empezar la run en un CHECKPOINT (nivel 11, 21, …): si la
    /// run está limpia (0 perks elegidos), concede los niveles saltados con la
    /// política greedy del simulador de balance (daño→cadencia→perforante→
    /// refuerzos→blindaje→suerte→imán) — un build estándar equivalente al de
    /// quien llega jugando desde el nivel 1. Con perks ya elegidos no toca nada
    /// (una run en curso conserva las elecciones del jugador).
    /// </summary>
    public static void EnsureBaseline(int startLevel)
    {
        if (startLevel <= 1) return;

        int picked = 0;
        foreach (var p in AllTypes) picked += Level(p);
        if (picked > 0) return;

        int grants = startLevel - 1;
        PerkType[] order =
        {
            PerkType.Damage, PerkType.FireRate, PerkType.Pierce,
            PerkType.Reinforce, PerkType.StartShield, PerkType.Lucky, PerkType.Magnet,
        };
        foreach (var p in order)
        {
            while (grants > 0 && !IsMaxed(p)) { Grant(p); grants--; }
            if (grants == 0) break;
        }
    }
}
