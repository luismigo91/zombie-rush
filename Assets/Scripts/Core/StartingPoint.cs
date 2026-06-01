using UnityEngine;

/// <summary>Mejora permanente del punto de partida que vende la meta-tienda.</summary>
public enum StartStat { Units, Weapon }

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

    public static int MaxLevel(StartStat s) => s == StartStat.Units ? 8 : Weapons.MaxTier;
    public static int Level(StartStat s) => Mathf.Clamp(PlayerPrefs.GetInt(Key(s), 0), 0, MaxLevel(s));

    public static int StartUnits => 6 + 3 * Level(StartStat.Units);
    public static int BaseWeaponTier => Level(StartStat.Weapon);

    public static string Name(StartStat s) => s == StartStat.Units ? "Soldados iniciales" : "Arma base";
    public static string ValueText(StartStat s)
        => s == StartStat.Units ? $"{StartUnits} soldados" : Weapons.Name(BaseWeaponTier);

    public static bool IsMaxed(StartStat s) => Level(s) >= MaxLevel(s);

    public static int NextCost(StartStat s)
    {
        if (IsMaxed(s)) return -1;
        int lvl = Level(s);
        int baseCost = s == StartStat.Units ? 25 : 120;
        float growth = s == StartStat.Units ? 1.5f : 2.0f;
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

    /// <summary>Borra todo el progreso (banco, mejoras y nivel de campaña). Para pruebas.</summary>
    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey("coins");
        PlayerPrefs.DeleteKey(Key(StartStat.Units));
        PlayerPrefs.DeleteKey(Key(StartStat.Weapon));
        PlayerPrefs.DeleteKey("level");
        PlayerPrefs.Save();
    }
}
