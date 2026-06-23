using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Caché central de sprites cargados desde <c>Resources/Art/</c>. Mantiene la
/// filosofía code-first: los bootstraps piden sprites por nombre (p. ej.
/// <c>ArtCache.Sprite("environment/road_asphalt01")</c>) y esta clase los carga
/// desde disco con caché, sin cablear nada en el Inspector.
///
/// Si un sprite solicitado no existe en Resources, cae a un fallback procedural
/// de <see cref="PixelArt"/> (mientras exista) para que el juego nunca crashee
/// por un asset ausente — solo loguea un warning.
///
/// Convención de nombres: <c>"categoria/nombre"</c> → <c>Resources/Art/categoria/nombre</c>.
/// Para arrays de animación, <see cref="Sprites"/> carga todos los sprites de
/// una carpeta con <c>Resources.LoadAll</c>.
/// </summary>
public static class ArtCache
{
    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
    static readonly Dictionary<string, Sprite[]> arrCache = new Dictionary<string, Sprite[]>();

    /// <summary>
    /// Devuelve un sprite individual cacheado. Ruta relativa sin extensión,
    /// p. ej. <c>"environment/road_asphalt01"</c>. Si no existe en Resources,
    /// cae a un fallback de <see cref="PixelArt"/> por nombre lógico.
    /// </summary>
    public static Sprite Sprite(string path)
    {
        if (cache.TryGetValue(path, out var s)) return s;

        var loaded = Resources.Load<Sprite>($"Art/{path}");
        if (loaded != null)
        {
            cache[path] = loaded;
            return loaded;
        }

        // Fallback procedural.
        var fb = PixelArtFallback(path);
        if (fb != null)
        {
            cache[path] = fb;
            return fb;
        }

        Debug.LogWarning($"[ArtCache] Sprite no encontrado: Art/{path} (ni fallback procedural).");
        cache[path] = null;
        return null;
    }

    /// <summary>
    /// Devuelve un array de sprites (para animación) cacheado. Carga todos los
    /// sprites de la carpeta <c>Resources/Art/{folder}</c> con
    /// <c>Resources.LoadAll</c>. Si no hay nada, cae al fallback de PixelArt.
    /// </summary>
    public static Sprite[] Sprites(string folder)
    {
        if (arrCache.TryGetValue(folder, out var arr)) return arr;

        var loaded = Resources.LoadAll<Sprite>($"Art/{folder}");
        if (loaded != null && loaded.Length > 0)
        {
            arrCache[folder] = loaded;
            return loaded;
        }

        // Fallback procedural para animaciones conocidas.
        var fb = PixelArtArrayFallback(folder);
        if (fb != null)
        {
            arrCache[folder] = fb;
            return fb;
        }

        Debug.LogWarning($"[ArtCache] No se encontraron sprites en Art/{folder}/ (ni fallback).");
        arrCache[folder] = System.Array.Empty<Sprite>();
        return arrCache[folder];
    }

    /// <summary>Atajo para el fallback común: un sprite individual o null.</summary>
    static Sprite PixelArtFallback(string path)
    {
        switch (path)
        {
            case "characters/soldier":      return PixelArt.Player;
            case "characters/zombie":       return PixelArt.Zombie;
            case "characters/boss":         return PixelArt.Boss;
            case "combat/bullet":           return PixelArt.Bullet;
            case "items/coin":              return PixelArt.Coin;
            case "items/chest":             return PixelArt.Chest;
            case "fx/muzzle":               return PixelArt.Muzzle;
            default:                        return null;
        }
    }

    /// <summary>Atajo para el fallback de arrays de animación.</summary>
    static Sprite[] PixelArtArrayFallback(string folder)
    {
        switch (folder)
        {
            case "characters/soldier_march":  return PixelArt.SoldierMarch;
            case "characters/soldier_shoot":  return PixelArt.SoldierShoot;
            case "characters/zombie_shamble": return PixelArt.ZombieShamble;
            case "items/coin_spin":           return PixelArt.CoinSpin;
            default:                          return null;
        }
    }
}
