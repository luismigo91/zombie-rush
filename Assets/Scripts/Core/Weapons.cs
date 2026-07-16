using UnityEngine;

/// <summary>
/// Arma GLOBAL del escuadrón por tiers (Zombie Rush). Todas las unidades disparan
/// el mismo arma; el tier modula daño, cadencia y nº de streams. El tier base lo
/// fija la meta-tienda (StartingPoint) y sube durante el nivel con los gates de
/// arma (GameManager.WeaponTier).
/// </summary>
public static class Weapons
{
    public struct Tier
    {
        public string name;
        public float damageMult;
        public float fireRateMult;
        public int extraStreams;
        public int pierce; // enemigos extra que atraviesan las balas (mecánica perforante)
    }

    // Progresión de DPS (daño×cadencia): 1.0 → 1.4 → 1.44 (+pierce) → 1.82 → 2.38 → 2.86
    // (+pierce alto). Con 6 tiers los gates ARMA+ importan durante toda la campaña
    // (con 3 se agotaban hacia el nivel 10 y el gate quedaba muerto).
    public static readonly Tier[] Tiers =
    {
        new Tier { name = "Pistola",     damageMult = 1.0f,  fireRateMult = 1.0f, extraStreams = 0, pierce = 0 },
        new Tier { name = "Subfusil",    damageMult = 0.7f,  fireRateMult = 2.0f, extraStreams = 0, pierce = 0 },
        new Tier { name = "Escopeta",    damageMult = 1.6f,  fireRateMult = 0.9f, extraStreams = 2, pierce = 2 },
        new Tier { name = "Rifle",       damageMult = 1.3f,  fireRateMult = 1.4f, extraStreams = 1, pierce = 1 },
        new Tier { name = "Minigun",     damageMult = 0.85f, fireRateMult = 2.8f, extraStreams = 2, pierce = 1 },
        new Tier { name = "Láser",       damageMult = 2.2f,  fireRateMult = 1.3f, extraStreams = 3, pierce = 4 },
    };

    public static int MaxTier => Tiers.Length - 1;
    public static Tier Get(int tier) => Tiers[Mathf.Clamp(tier, 0, MaxTier)];
    public static string Name(int tier) => Get(tier).name;
}
