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
            loaded = Normalize(loaded);
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
            for (int i = 0; i < loaded.Length; i++) loaded[i] = Normalize(loaded[i]);
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

    /// <summary>
    /// Normaliza la ESCALA del sprite: lo re-envuelve con pixelsPerUnit igual a su
    /// dimensión mayor, de modo que todo asset mida ~1×1 unidad de mundo sea cual
    /// sea su resolución en píxeles. Así las escalas del gameplay (localScale
    /// absolutas: soldado 0.32, jefe 1.7, etc.) significan lo mismo para cualquier
    /// arte. Los fallbacks de PixelArt ya nacen ~1u y no pasan por aquí.
    /// </summary>
    static Sprite Normalize(Sprite src)
    {
        if (src == null) return null;
        float maxDim = Mathf.Max(src.rect.width, src.rect.height);
        if (Mathf.Approximately(maxDim, src.pixelsPerUnit)) return src; // ya normalizado

        var norm = UnityEngine.Sprite.Create(src.texture, src.rect,
            new Vector2(0.5f, 0.5f), maxDim, 0, SpriteMeshType.FullRect);
        norm.name = src.name + "_norm";
        return norm;
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

    /// <summary>
    /// Construye un array de sprites a partir de nombres individuales (útil para
    /// sets de poses sueltas como los de Kenney, que no vienen en un sheet).
    /// Carga cada sprite con <see cref="Sprite"/> (con caché y fallback) y
    /// devuelve el array. Si todos fallan, devuelve array vacío.
    /// </summary>
    public static Sprite[] Array(params string[] paths)
    {
        var list = new List<Sprite>(paths.Length);
        foreach (var p in paths)
        {
            var s = Sprite(p);
            if (s != null) list.Add(s);
        }
        return list.Count > 0 ? list.ToArray() : System.Array.Empty<Sprite>();
    }

    // --- Arrays cacheados de animaciones de personajes (Kenney poses) ---
    static Sprite[] _soldierMarch, _soldierShoot, _zombieShamble;

    /// <summary>Pseudo-ciclo de marcha del soldado: [stand, hold] breathing.</summary>
    public static Sprite[] SoldierMarch
        => _soldierMarch ??= Array("characters/soldier_stand", "characters/soldier_hold", "characters/soldier_gun");

    /// <summary>Pseudo-ciclo de disparo: [gun, hold] quick fire pose.</summary>
    public static Sprite[] SoldierShoot
        => _soldierShoot ??= Array("characters/soldier_gun", "characters/soldier_hold");

    /// <summary>Pseudo-ciclo de arrastre del zombie: [stand, hold] lurching.</summary>
    public static Sprite[] ZombieShamble
        => _zombieShamble ??= Array("characters/zombie_stand", "characters/zombie_hold", "characters/zombie_gun");

    /// <summary>Sprite individual del soldado (primer frame de marcha).</summary>
    public static Sprite Soldier => SoldierMarch.Length > 0 ? SoldierMarch[0] : PixelArt.Player;

    /// <summary>Sprite individual del zombie (primer frame de shamble).</summary>
    public static Sprite Zombie => ZombieShamble.Length > 0 ? ZombieShamble[0] : PixelArt.Zombie;

    /// <summary>Sprite del jefe (usa zombie_machine como silueta masiva).</summary>
    public static Sprite Boss => Sprite("characters/zombie_machine") ?? PixelArt.Boss;
}
