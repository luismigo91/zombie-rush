using UnityEngine;

/// <summary>
/// Monedas persistentes (banco) entre partidas, guardadas con PlayerPrefs.
/// Suficiente para un juego local en el MVP (Plan técnico). Más adelante podría
/// migrar a JSON en disco si la economía crece.
/// </summary>
public static class Economy
{
    const string Key = "coins";

    public static int Coins => PlayerPrefs.GetInt(Key, 0);

    /// <summary>Ingresa monedas al banco (p. ej. al terminar una run).</summary>
    public static void Add(int amount)
    {
        if (amount <= 0) return;
        PlayerPrefs.SetInt(Key, Coins + amount);
        PlayerPrefs.Save();
    }

    /// <summary>Intenta gastar monedas; devuelve false si no hay suficientes.</summary>
    public static bool TrySpend(int amount)
    {
        if (amount <= 0 || Coins < amount) return false;
        PlayerPrefs.SetInt(Key, Coins - amount);
        PlayerPrefs.Save();
        return true;
    }
}
