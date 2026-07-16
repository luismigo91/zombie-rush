using UnityEngine;

/// <summary>Habilidad activa equipada (botón del HUD).</summary>
public enum AbilityType { Grenade, Freeze, Sentry }

/// <summary>
/// Equipamiento del jugador elegido en el MENÚ y persistido con PlayerPrefs:
/// - Habilidad activa (granada / congelación / centinela): gratis, se cicla.
/// - Skin del escuadrón (tintes; sink cosmético de monedas).
/// - Héroe FRANCOTIRADOR (compra única): unidad dorada con auto-apuntado que
///   prioriza al enemigo más gordo (lo que peor lleva el fuego recto).
/// </summary>
public static class Loadout
{
    // ------------------------------------------------------------ habilidad

    const string AbilityKey = "loadout_ability";

    public static AbilityType Ability
    {
        get => (AbilityType)Mathf.Clamp(PlayerPrefs.GetInt(AbilityKey, 0), 0, 2);
        set { PlayerPrefs.SetInt(AbilityKey, (int)value); PlayerPrefs.Save(); }
    }

    public static string AbilityName(AbilityType a) => a switch
    {
        AbilityType.Grenade => "GRANADA",
        AbilityType.Freeze => "CONGELACIÓN",
        _ => "CENTINELA",
    };

    public static void CycleAbility() => Ability = (AbilityType)(((int)Ability + 1) % 3);

    // ---------------------------------------------------------------- skins

    public struct Skin
    {
        public string name;
        public Color tint;
        public int cost;
    }

    /// <summary>Tintes del escuadrón. Nada verdoso: se confundiría con los zombies.</summary>
    public static readonly Skin[] Skins =
    {
        new Skin { name = "CLÁSICO",  tint = Color.white,                        cost = 0 },
        new Skin { name = "DESIERTO", tint = new Color(0.91f, 0.82f, 0.63f),     cost = 250 },
        new Skin { name = "ÁRTICO",   tint = new Color(0.66f, 0.78f, 0.91f),     cost = 250 },
        new Skin { name = "SOMBRA",   tint = new Color(0.57f, 0.58f, 0.66f),     cost = 500 },
        new Skin { name = "ORO",      tint = new Color(1f, 0.82f, 0.23f),        cost = 1000 },
    };

    const string SkinKey = "loadout_skin";

    public static int SkinIndex
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt(SkinKey, 0), 0, Skins.Length - 1);
        set { PlayerPrefs.SetInt(SkinKey, Mathf.Clamp(value, 0, Skins.Length - 1)); PlayerPrefs.Save(); }
    }

    /// <summary>Tinte de la skin equipada (lo aplica Squad a cada unidad).</summary>
    public static Color SkinTint => Skins[SkinIndex].tint;

    public static bool SkinOwned(int i) => i == 0 || PlayerPrefs.GetInt("skin_owned_" + i, 0) == 1;

    /// <summary>Índice de la SIGUIENTE skin en el ciclo (para mostrarla en el botón).</summary>
    public static int NextSkinIndex => (SkinIndex + 1) % Skins.Length;

    /// <summary>
    /// Avanza a la siguiente skin: si está comprada la equipa; si no, la compra
    /// (el precio está visible en el botón: gasto informado) y la equipa. Si el
    /// banco no alcanza, no hace nada. Devuelve true si el equipamiento cambió.
    /// </summary>
    public static bool CycleSkin()
    {
        int i = NextSkinIndex;
        if (!SkinOwned(i))
        {
            if (!Economy.TrySpend(Skins[i].cost)) return false;
            PlayerPrefs.SetInt("skin_owned_" + i, 1);
        }
        SkinIndex = i;
        return true;
    }

    // ---------------------------------------------------------------- héroe

    public const int SniperCost = 800;
    const string SniperKey = "hero_sniper";

    public static bool SniperOwned => PlayerPrefs.GetInt(SniperKey, 0) == 1;

    public static bool TryBuySniper()
    {
        if (SniperOwned) return false;
        if (!Economy.TrySpend(SniperCost)) return false;
        PlayerPrefs.SetInt(SniperKey, 1);
        PlayerPrefs.Save();
        return true;
    }

    // ---------------------------------------------------------------- reset

    /// <summary>Borra el equipamiento (lo llama el reinicio de progreso del menú).</summary>
    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(AbilityKey);
        PlayerPrefs.DeleteKey(SkinKey);
        PlayerPrefs.DeleteKey(SniperKey);
        for (int i = 1; i < Skins.Length; i++) PlayerPrefs.DeleteKey("skin_owned_" + i);
        PlayerPrefs.Save();
    }
}
