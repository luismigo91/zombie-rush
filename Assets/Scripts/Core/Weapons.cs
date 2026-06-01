using UnityEngine;

public enum WeaponId { Pistola, Escopeta }

/// <summary>
/// Armas desbloqueables. La Pistola se tiene siempre; la Escopeta se compra con
/// monedas del banco y se equipa. Todo persiste con PlayerPrefs.
/// </summary>
public static class Weapons
{
    public const int EscopetaCost = 150;

    public static bool Owns(WeaponId w)
    {
        if (w == WeaponId.Pistola) return true;
        return PlayerPrefs.GetInt("wpn_" + w, 0) == 1;
    }

    public static WeaponId Equipped
    {
        get
        {
            var e = (WeaponId)PlayerPrefs.GetInt("wpn_equipped", 0);
            return Owns(e) ? e : WeaponId.Pistola;
        }
        set
        {
            if (!Owns(value)) return;
            PlayerPrefs.SetInt("wpn_equipped", (int)value);
            PlayerPrefs.Save();
        }
    }

    public static int Cost(WeaponId w) => w == WeaponId.Escopeta ? EscopetaCost : 0;
    public static string Name(WeaponId w) => w == WeaponId.Escopeta ? "Escopeta" : "Pistola";

    /// <summary>Compra y equipa un arma si hay monedas suficientes.</summary>
    public static bool TryBuy(WeaponId w)
    {
        if (Owns(w)) return false;
        if (!Economy.TrySpend(Cost(w))) return false;
        PlayerPrefs.SetInt("wpn_" + w, 1);
        PlayerPrefs.Save();
        Equipped = w; // se equipa al comprarla
        return true;
    }
}
