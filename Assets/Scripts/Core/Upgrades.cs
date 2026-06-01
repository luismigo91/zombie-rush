using System.Collections.Generic;
using UnityEngine;

/// <summary>Definición por defecto de una mejora (fuente única de verdad).</summary>
public struct UpgradeDef
{
    public StatId stat;
    public string name;
    public float baseValue;
    public float perLevel;
    public int baseCost;
    public float costGrowth;
    public int maxLevel;
}

/// <summary>
/// Gestiona los niveles de mejora (persistidos con PlayerPrefs) y calcula los
/// valores y costes. Usa los assets UpgradeData de Resources/Upgrades si existen;
/// si no, recurre a la tabla Defaults (para que el juego funcione igualmente).
///
/// Defaults también es lo que usa el generador de datos del editor, así no se
/// duplica la curva de progresión en dos sitios.
/// </summary>
public static class Upgrades
{
    public static readonly UpgradeDef[] Defaults =
    {
        new UpgradeDef { stat = StatId.Damage,    name = "Daño",      baseValue = 10f,  perLevel = 4f,    baseCost = 12, costGrowth = 1.5f, maxLevel = 12 },
        new UpgradeDef { stat = StatId.FireRate,  name = "Cadencia",  baseValue = 3f,   perLevel = 0.4f,  baseCost = 15, costGrowth = 1.5f, maxLevel = 10 },
        new UpgradeDef { stat = StatId.MaxHealth, name = "Vida",      baseValue = 100f, perLevel = 25f,   baseCost = 12, costGrowth = 1.4f, maxLevel = 12 },
        new UpgradeDef { stat = StatId.MoveSpeed, name = "Velocidad", baseValue = 1.0f, perLevel = 0.12f, baseCost = 10, costGrowth = 1.4f, maxLevel = 8  },
    };

    static Dictionary<StatId, UpgradeData> _assets;

    static void EnsureLoaded()
    {
        if (_assets != null) return;
        _assets = new Dictionary<StatId, UpgradeData>();
        foreach (var u in Resources.LoadAll<UpgradeData>("Upgrades"))
            _assets[u.stat] = u;
    }

    /// <summary>Resuelve la definición efectiva de un stat (asset si existe, si no Default).</summary>
    static UpgradeDef Resolve(StatId stat)
    {
        EnsureLoaded();
        if (_assets.TryGetValue(stat, out var a))
        {
            return new UpgradeDef
            {
                stat = a.stat, name = a.displayName,
                baseValue = a.baseValue, perLevel = a.perLevel,
                baseCost = a.baseCost, costGrowth = a.costGrowth, maxLevel = a.maxLevel
            };
        }
        foreach (var d in Defaults)
            if (d.stat == stat) return d;
        return Defaults[0];
    }

    static string LevelKey(StatId stat) => "upg_" + stat;

    public static IEnumerable<StatId> AllStats
    {
        get
        {
            yield return StatId.Damage;
            yield return StatId.FireRate;
            yield return StatId.MaxHealth;
            yield return StatId.MoveSpeed;
        }
    }

    public static string Name(StatId stat) => Resolve(stat).name;
    public static int Level(StatId stat) => PlayerPrefs.GetInt(LevelKey(stat), 0);
    public static int MaxLevel(StatId stat) => Resolve(stat).maxLevel;
    public static bool IsMaxed(StatId stat) => Level(stat) >= MaxLevel(stat);

    /// <summary>Valor actual del stat según su nivel.</summary>
    public static float Value(StatId stat)
    {
        var d = Resolve(stat);
        return d.baseValue + d.perLevel * Level(stat);
    }

    /// <summary>Coste del siguiente nivel, o -1 si ya está al máximo.</summary>
    public static int NextCost(StatId stat)
    {
        var d = Resolve(stat);
        int lvl = Level(stat);
        if (lvl >= d.maxLevel) return -1;
        return Mathf.RoundToInt(d.baseCost * Mathf.Pow(d.costGrowth, lvl));
    }

    /// <summary>Intenta comprar el siguiente nivel; gasta del banco si se puede.</summary>
    public static bool TryBuy(StatId stat)
    {
        if (IsMaxed(stat)) return false;
        int cost = NextCost(stat);
        if (!Economy.TrySpend(cost)) return false;
        PlayerPrefs.SetInt(LevelKey(stat), Level(stat) + 1);
        PlayerPrefs.Save();
        return true;
    }

    /// <summary>Borra todo el progreso (monedas y niveles). Útil para pruebas.</summary>
    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey("coins");
        foreach (var s in AllStats)
            PlayerPrefs.DeleteKey("upg_" + s);
        PlayerPrefs.Save();
    }
}
