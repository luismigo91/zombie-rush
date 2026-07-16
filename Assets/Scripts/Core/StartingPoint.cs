using UnityEngine;

/// <summary>Mejora permanente del punto de partida que vende la meta-tienda.</summary>
public enum StartStat { Units, Weapon, Damage }

/// <summary>
/// Punto de partida permanente (Zombie Rush): con qué empieza CADA nivel el
/// escuadrón. Sustituye a las mejoras de % del juego anterior. Persistido con
/// PlayerPrefs; la run lo amplifica con gates/jaulas.
///
/// - Units:  nº de soldados iniciales.
/// - Weapon: tier de arma base (ver Weapons).
///
/// El constructor estático migra la economía pre-pivote (borra mejoras % y armas
/// antiguas; conserva las monedas del banco).
/// </summary>
public static class StartingPoint
{
    static StartingPoint()
    {
        if (PlayerPrefs.GetInt("migrated_v2", 0) == 0)
        {
            PlayerPrefs.DeleteKey("upg_Damage");
            PlayerPrefs.DeleteKey("upg_FireRate");
            PlayerPrefs.DeleteKey("upg_MaxHealth");
            PlayerPrefs.DeleteKey("upg_MoveSpeed");
            PlayerPrefs.DeleteKey("wpn_Escopeta");
            PlayerPrefs.DeleteKey("wpn_equipped");
            PlayerPrefs.SetInt("migrated_v2", 1);
            PlayerPrefs.Save();
        }
    }

    static string Key(StartStat s) => "sp_" + s;

    public static int MaxLevel(StartStat s) => s switch
    {
        StartStat.Units => 5,  // 4→14 iniciales: con el cap de escuadrón en 30, la
                               // run debe seguir creciendo con gates/jaulas
        StartStat.Weapon => Weapons.MaxTier,
        _ => 40, // Damage: línea LARGA (sink de monedas duradero; +5 % por nivel)
    };

    public static int Level(StartStat s) => Mathf.Clamp(PlayerPrefs.GetInt(Key(s), 0), 0, MaxLevel(s));

    public static int StartUnits => 4 + 2 * Level(StartStat.Units);
    public static int BaseWeaponTier => Level(StartStat.Weapon);

    /// <summary>Multiplicador de daño permanente comprado en la tienda (+5 % por nivel:
    /// con el cap de 30 soldados, las MEJORAS son el eje de poder — rediseño de playtest).</summary>
    public static float DamageMult => 1f + 0.05f * Level(StartStat.Damage);

    public static string Name(StartStat s) => s switch
    {
        StartStat.Units => "Soldados iniciales",
        StartStat.Weapon => "Arma base",
        _ => "Potencia de fuego",
    };

    public static string ValueText(StartStat s) => s switch
    {
        StartStat.Units => $"{StartUnits} soldados",
        StartStat.Weapon => Weapons.Name(BaseWeaponTier),
        _ => $"+{Level(StartStat.Damage) * 5}% daño",
    };

    public static bool IsMaxed(StartStat s) => Level(s) >= MaxLevel(s);

    public static int NextCost(StartStat s)
    {
        if (IsMaxed(s)) return -1;
        int lvl = Level(s);
        (int baseCost, float growth) = s switch
        {
            StartStat.Units => (25, 1.5f),
            StartStat.Weapon => (120, 2.0f),
            _ => (40, 1.3f), // Damage: exponencial suave y sin final cercano
        };
        return Mathf.RoundToInt(baseCost * Mathf.Pow(growth, lvl));
    }

    /// <summary>Compra el siguiente nivel de la mejora gastando del banco.</summary>
    public static bool TryBuy(StartStat s)
    {
        if (IsMaxed(s)) return false;
        if (!Economy.TrySpend(NextCost(s))) return false;
        PlayerPrefs.SetInt(Key(s), Level(s) + 1);
        PlayerPrefs.Save();
        return true;
    }

    /// <summary>Borra todo el progreso (banco, mejoras, perks, récords y campaña). Para pruebas.</summary>
    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey("coins");
        PlayerPrefs.DeleteKey(Key(StartStat.Units));
        PlayerPrefs.DeleteKey(Key(StartStat.Weapon));
        PlayerPrefs.DeleteKey(Key(StartStat.Damage));
        PlayerPrefs.DeleteKey("level");
        PlayerPrefs.Save();
        Perks.ResetAll();
        RunConfig.ResetRecords();
        Loadout.ResetAll();
        Campaign.ResetBest();
    }
}
