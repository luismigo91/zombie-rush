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
    }

    public static readonly Tier[] Tiers =
    {
        new Tier { name = "Pistola",  damageMult = 1.0f, fireRateMult = 1.0f, extraStreams = 0 },
        new Tier { name = "Subfusil", damageMult = 0.7f, fireRateMult = 2.0f, extraStreams = 0 },
        new Tier { name = "Escopeta", damageMult = 1.6f, fireRateMult = 0.9f, extraStreams = 2 },
    };

    public static int MaxTier => Tiers.Length - 1;
    public static Tier Get(int tier) => Tiers[Mathf.Clamp(tier, 0, MaxTier)];
    public static string Name(int tier) => Get(tier).name;
}
